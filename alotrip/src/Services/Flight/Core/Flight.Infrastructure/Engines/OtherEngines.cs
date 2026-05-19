using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Flight.Domain.Enums;
using Flight.Infrastructure.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flight.Infrastructure.Engines;

// ─────────────────────────────────────────────────────────────────────────────
// DatacomEngine — LCC SOAP WSDL (VietJet, Bamboo, Vietravel, VN domestic)
// Migrated from DatacomEngine.cs (legacy SOAP proxy DatacomWS.FlightWS)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Datacom LCC Engine.
/// Transport: SOAP via HttpClient (replaces legacy DatacomWS ASMX proxy).
/// SessionData format stored on FareDataDto:
///   JSON: {"apiSession":"xxx","fareDataId":"5"}
/// Each FlightSegmentDto.SelectedValue = FlightValue (needed for booking).
/// </summary>
public sealed class DatacomEngine : IFlightEngine
{
    private readonly string _headerUser;
    private readonly string _headerPass;
    private readonly string _agentAccount;
    private readonly string _agentPassword;
    private readonly string _productKey;
    private readonly string _contactPhone;

    private readonly ILogger<DatacomEngine> _logger;
    private readonly IHttpClientFactory      _http;
    private readonly IConfiguration          _config;

    public FlightSource Source    => FlightSource.Datacom;
    public bool         IsEnabled => true;

    public DatacomEngine(ILogger<DatacomEngine> logger, IHttpClientFactory http, IConfiguration config)
    {
        _logger  = logger;
        _http    = http;
        _config  = config;

        _headerUser    = config["Datacom:HeaderUser"]    ?? throw new InvalidOperationException("Missing config Datacom:HeaderUser");
        _headerPass    = config["Datacom:HeaderPass"]    ?? throw new InvalidOperationException("Missing config Datacom:HeaderPass");
        _agentAccount  = config["Datacom:AgentAccount"]  ?? throw new InvalidOperationException("Missing config Datacom:AgentAccount");
        _agentPassword = config["Datacom:AgentPassword"] ?? throw new InvalidOperationException("Missing config Datacom:AgentPassword");
        _productKey    = config["Datacom:ProductKey"]    ?? throw new InvalidOperationException("Missing config Datacom:ProductKey");
        _contactPhone  = config["Datacom:ContactPhone"]  ?? "84916463066";
    }

    // ── IFlightEngine ────────────────────────────────────────────────────────

    public async Task<IEnumerable<FareDataDto>> SearchFlightAsync(
        SearchFlightRequest req, CancellationToken ct = default)
    {
        try
        {
            int  limit = int.Parse(_config["Datacom:MaxIndex"] ?? "500");
            string soapRequest = BuildSearchSoap(req);
            string? rawResponse = await PostSoapAsync("Datacom", soapRequest, "Search", ct);
            if (rawResponse is null) return [];

            var fares = ParseSearchResponse(rawResponse, req);
            return fares.OrderBy(f => f.TotalFare).Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Datacom] SearchFlightAsync failed");
            return [];
        }
    }

    public async Task<FareDataDto?> VerifyFareAsync(string fareId, string sessionData, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return null; // Datacom: price is confirmed at book time — no separate verify step
    }

    /// <summary>
    /// Books via Datacom SOAP Book operation.
    /// Reconstructs FareDataInfo from fareData.SessionData (JSON) and segment SelectedValues.
    /// </summary>
    public async Task<BookResultDto> BookFlightAsync(
        BookFlightRequest req, FareDataDto fareData, CancellationToken ct = default)
    {
        try
        {
            // ── Decode session info stored during search ──────────────────────
            var session = ParseDatacomSession(fareData.SessionData);
            string apiSession  = session.ApiSession;
            string fareDataId  = session.FareDataId;

            // ── Build SOAP envelope ───────────────────────────────────────────
            string soapBody = BuildBookSoap(req, fareData, apiSession, fareDataId);
            string? rawResp = await PostSoapAsync("Datacom", soapBody, "Book", ct);
            if (rawResp is null)
                return Fail("[Datacom] Empty response from Book");

            return ParseBookResponse(rawResp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Datacom] BookFlightAsync failed");
            return Fail(ex.Message);
        }
    }

    /// <summary>
    /// Issues ticket via Datacom SOAP Issue operation.
    /// sessionData = airline code (extracted from bookingCode or passed by caller).
    /// bookingCode may contain pipe-separated codes (depart|return).
    /// </summary>
    public async Task<IssueTicketResultDto> IssueTicketAsync(
        string bookingCode, string sessionData, CancellationToken ct = default)
    {
        try
        {
            // sessionData holds the airline code for the booking
            string airline = sessionData;
            var tickets = new List<string>();

            // Handle depart|return codes
            foreach (string code in bookingCode.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                string soapBody = BuildIssueSoap(airline, code.Trim());
                string? rawResp = await PostSoapAsync("Datacom", soapBody, "Issue", ct);
                if (rawResp is null) continue;

                var nums = ParseIssueResponse(rawResp);
                tickets.AddRange(nums);
            }

            if (tickets.Count > 0)
                return new IssueTicketResultDto { IsSuccess = true, TicketNumbers = tickets };

            return new IssueTicketResultDto { IsSuccess = false, ErrorMessage = "[Datacom] No tickets returned" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Datacom] IssueTicketAsync failed");
            return new IssueTicketResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Retrieves baggage add-on options from Datacom SOAP GetBaggage endpoint.
    /// Ported from legacy DatacomEngine.GetBaggage — applies agent baggage fee markup,
    /// skips Vietnam Airlines (VN), handles VJ naming and QH zero-price special case.
    /// </summary>
    public async Task<BaggageInfoDto> GetBaggagesAsync(
        FareDataDto fareData, CancellationToken ct = default)
    {
        var result = new BaggageInfoDto();
        try
        {
            var session = ParseDatacomSession(fareData.SessionData);
            string apiSession = session.ApiSession;
            string fareDataId = session.FareDataId;

            double feePercent = double.TryParse(_config["BaggageFee:Percent"], out var fp) ? fp : 0;
            double feeAmount  = double.TryParse(_config["BaggageFee:Amount"],  out var fa) ? fa : 0;

            // ── Build ListFareData (depart + optional return) ─────────────────
            var fdInfos = new StringBuilder();

            // Depart leg
            if (fareData.OutboundSegments.Count > 0)
            {
                fdInfos.Append("<tem:FareDataInfo>");
                fdInfos.Append($"<tem:Session>{EscapeXml(apiSession)}</tem:Session>");
                fdInfos.Append($"<tem:FareDataId>{EscapeXml(fareDataId)}</tem:FareDataId>");
                fdInfos.Append("<tem:ListFlight>");
                foreach (var seg in fareData.OutboundSegments)
                    fdInfos.Append($"<tem:FlightInfo><tem:FlightValue>{EscapeXml(seg.SelectedValue)}</tem:FlightValue></tem:FlightInfo>");
                fdInfos.Append("</tem:ListFlight>");
                fdInfos.Append("</tem:FareDataInfo>");
            }

            // Return leg
            if (fareData.ReturnSegments.Count > 0)
            {
                string returnFdId = fareDataId + "_r";
                fdInfos.Append("<tem:FareDataInfo>");
                fdInfos.Append($"<tem:Session>{EscapeXml(apiSession)}</tem:Session>");
                fdInfos.Append($"<tem:FareDataId>{EscapeXml(returnFdId)}</tem:FareDataId>");
                fdInfos.Append("<tem:ListFlight>");
                foreach (var seg in fareData.ReturnSegments)
                    fdInfos.Append($"<tem:FlightInfo><tem:FlightValue>{EscapeXml(seg.SelectedValue)}</tem:FlightValue></tem:FlightInfo>");
                fdInfos.Append("</tem:ListFlight>");
                fdInfos.Append("</tem:FareDataInfo>");
            }

            // ── Build SOAP envelope ───────────────────────────────────────────
            string soapBody =
                $"<soap:Envelope xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/'>" +
                $"<soap:Body><tem:GetBaggage><tem:request>" +
                $"<tem:HeaderUser>{_headerUser}</tem:HeaderUser>" +
                $"<tem:HeaderPass>{_headerPass}</tem:HeaderPass>" +
                $"<tem:AgentAccount>{_agentAccount}</tem:AgentAccount>" +
                $"<tem:AgentPassword>{_agentPassword}</tem:AgentPassword>" +
                $"<tem:ProductKey>{_productKey}</tem:ProductKey>" +
                $"<tem:Currency>VND</tem:Currency>" +
                $"<tem:Language>en</tem:Language>" +
                $"<tem:ListFareData>{fdInfos}</tem:ListFareData>" +
                $"</tem:request></tem:GetBaggage></soap:Body></soap:Envelope>";

            string? rawResp = await PostSoapAsync("Datacom", soapBody, "GetBaggage", ct);
            if (rawResp is null) return result;

            // ── Parse response ────────────────────────────────────────────────
            var doc = new XmlDocument();
            doc.LoadXml(rawResp);
            var baggageNodes = doc.SelectNodes("//Baggage");
            if (baggageNodes == null || baggageNodes.Count == 0) return result;

            string departAirline = fareData.OutboundSegments.FirstOrDefault()?.Airline ?? "";
            string returnAirline = fareData.ReturnSegments.FirstOrDefault()?.Airline ?? "";

            foreach (XmlElement bag in baggageNodes)
            {
                int    leg         = int.TryParse(bag.SelectSingleNode("Leg/text()")?.Value, out var l) ? l : 0;
                string bagAirline  = bag.SelectSingleNode("Airline/text()")?.Value  ?? "";
                string code        = bag.SelectSingleNode("Code/text()")?.Value     ?? "";
                string value       = bag.SelectSingleNode("Value/text()")?.Value    ?? "";
                double price       = double.TryParse(bag.SelectSingleNode("Price/text()")?.Value, out var p) ? p : 0;
                string bagCurrency = bag.SelectSingleNode("Currency/text()")?.Value ?? "VND";

                // Skip Vietnam Airlines (VN) — VN manages baggage separately
                if (leg == 0 && departAirline == "VN") continue;
                if (leg == 1 && returnAirline == "VN") continue;

                // VJ: raw value as name; others: append "kg"
                string name = bagAirline == "VJ" ? value : value + "kg";

                // QH special case: price ≤ 0 → free; otherwise apply fee markup
                decimal finalPrice;
                if (bagAirline == "QH" && price <= 0)
                    finalPrice = 0;
                else
                    finalPrice = (decimal)(price + (price * feePercent / 100) + feeAmount);

                var option = new BaggageOptionDto
                {
                    AirlineCode = bagAirline,
                    Code        = code,
                    Name        = name,
                    Value       = value,
                    Price       = finalPrice,
                    Currency    = bagCurrency
                };

                if (leg == 0) result.DepartBaggages.Add(option);
                else if (leg == 1) result.ReturnBaggages.Add(option);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Datacom] GetBaggagesAsync failed");
        }
        return result;
    }

    /// <summary>Datacom does not expose fare rule text. Returns empty.</summary>
    public Task<List<FareRuleGroupDto>> GetFareRulesAsync(
        FareDataDto fareData, int itinerary, CancellationToken ct = default)
        => Task.FromResult(new List<FareRuleGroupDto>());

    // ── SOAP builders ─────────────────────────────────────────────────────────

    private string BuildSearchSoap(SearchFlightRequest req)
    {
        bool isRt  = req.TripType == TripType.RoundTrip;
        string flt1  = $"<tem:FlightRequest><tem:StartPoint>{req.Origin}</tem:StartPoint><tem:EndPoint>{req.Destination}</tem:EndPoint><tem:DepartDate>{req.DepartDate:ddMMyyyy}</tem:DepartDate><tem:Airline></tem:Airline></tem:FlightRequest>";
        string flt2  = isRt
            ? $"<tem:FlightRequest><tem:StartPoint>{req.Destination}</tem:StartPoint><tem:EndPoint>{req.Origin}</tem:EndPoint><tem:DepartDate>{req.ReturnDate:ddMMyyyy}</tem:DepartDate><tem:Airline></tem:Airline></tem:FlightRequest>"
            : string.Empty;

        return
            $"<soap:Envelope xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/'>" +
            $"<soap:Body><tem:Search><tem:request>" +
            $"<tem:HeaderUser>{_headerUser}</tem:HeaderUser>" +
            $"<tem:HeaderPass>{_headerPass}</tem:HeaderPass>" +
            $"<tem:AgentAccount>{_agentAccount}</tem:AgentAccount>" +
            $"<tem:AgentPassword>{_agentPassword}</tem:AgentPassword>" +
            $"<tem:ProductKey>{_productKey}</tem:ProductKey>" +
            $"<tem:Currency>VND</tem:Currency>" +
            $"<tem:Language>en</tem:Language>" +
            $"<tem:Adt>{req.AdultCount}</tem:Adt>" +
            $"<tem:Chd>{req.ChildCount}</tem:Chd>" +
            $"<tem:Inf>{req.InfantCount}</tem:Inf>" +
            $"<tem:ListFlight>{flt1}{flt2}</tem:ListFlight>" +
            $"</tem:request></tem:Search></soap:Body></soap:Envelope>";
    }

    private string BuildBookSoap(
        BookFlightRequest req, FareDataDto fareData, string apiSession, string fareDataId)
    {
        var sb = new StringBuilder();

        // ── Passengers ────────────────────────────────────────────────────────
        sb.Append("<tem:ListPassenger>");
        int paxIdx = 0;
        foreach (var pax in req.Passengers)
        {
            string bday = pax.BirthDate.HasValue ? pax.BirthDate.Value.ToString("ddMMyyyy") : "01011990";
            string bdayFull = pax.BirthDate.HasValue ? pax.BirthDate.Value.ToString("yyyy-MM-dd") : "1990-01-01";
            string paxType  = pax.Type == PassengerType.Adult ? "ADT"
                            : pax.Type == PassengerType.Child  ? "CHD" : "INF";

            sb.Append($"<tem:Passenger>" +
                      $"<tem:Index>{paxIdx}</tem:Index>" +
                      $"<tem:FirstName>{EscapeXml(pax.FirstName)}</tem:FirstName>" +
                      $"<tem:LastName>{EscapeXml(pax.LastName)}</tem:LastName>" +
                      $"<tem:Type>{paxType}</tem:Type>" +
                      $"<tem:Gender>{pax.Gender}</tem:Gender>" +
                      $"<tem:Birthday>{bday}</tem:Birthday>" +
                      $"<tem:DateOfBirth>{bdayFull}</tem:DateOfBirth>" +
                      $"<tem:ListBaggage/>" +
                      $"</tem:Passenger>");
            paxIdx++;
        }
        sb.Append("</tem:ListPassenger>");

        // ── FareData (depart) ─────────────────────────────────────────────────
        sb.Append("<tem:ListFareData>");
        sb.Append(BuildFareDataInfo(apiSession, fareDataId, fareData.OutboundSegments));

        // Return leg (if round-trip, some routes pack both legs into one FareDataInfo)
        if (fareData.TripType == TripType.RoundTrip && fareData.ReturnSegments.Count > 0)
        {
            string returnFareDataId = fareDataId + "_r"; // fallback; ideally stored separately
            sb.Append(BuildFareDataInfo(apiSession, returnFareDataId, fareData.ReturnSegments));
        }
        sb.Append("</tem:ListFareData>");

        // ── Contact ───────────────────────────────────────────────────────────
        string contactGender = req.Passengers.FirstOrDefault()?.Gender == "F" ? "F" : "M";
        string[] nameParts   = req.ContactName.Split(' ', 2);
        string contactFirst  = nameParts.Length > 1 ? nameParts[1] : req.ContactName;
        string contactLast   = nameParts[0];

        string contact =
            $"<tem:Contact>" +
            $"<tem:Gender>{contactGender}</tem:Gender>" +
            $"<tem:FirstName>{EscapeXml(contactFirst)}</tem:FirstName>" +
            $"<tem:LastName>{EscapeXml(contactLast)}</tem:LastName>" +
            $"<tem:Phone>{_contactPhone}</tem:Phone>" +
            $"<tem:Email>{EscapeXml(req.ContactEmail)}</tem:Email>" +
            $"<tem:Address>Hanoi, Vietnam</tem:Address>" +
            $"</tem:Contact>";

        return
            $"<soap:Envelope xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/'>" +
            $"<soap:Body><tem:Book><tem:request>" +
            $"<tem:HeaderUser>{_headerUser}</tem:HeaderUser>" +
            $"<tem:HeaderPass>{_headerPass}</tem:HeaderPass>" +
            $"<tem:AgentAccount>{_agentAccount}</tem:AgentAccount>" +
            $"<tem:AgentPassword>{_agentPassword}</tem:AgentPassword>" +
            $"<tem:ProductKey>{_productKey}</tem:ProductKey>" +
            $"<tem:Currency>VND</tem:Currency>" +
            $"<tem:Language>en</tem:Language>" +
            $"<tem:BookType>book-normal</tem:BookType>" +
            $"<tem:UseAgentContact>true</tem:UseAgentContact>" +
            contact +
            sb +
            $"<tem:Timeout>720</tem:Timeout>" +
            $"</tem:request></tem:Book></soap:Body></soap:Envelope>";
    }

    private string BuildFareDataInfo(
        string apiSession, string fareDataId, List<FlightSegmentDto> segments)
    {
        var sb = new StringBuilder();
        sb.Append("<tem:FareDataInfo>");
        sb.Append($"<tem:Session>{EscapeXml(apiSession)}</tem:Session>");
        sb.Append($"<tem:FareDataId>{EscapeXml(fareDataId)}</tem:FareDataId>");
        sb.Append("<tem:AutoIssue>false</tem:AutoIssue>");
        sb.Append("<tem:ListFlight>");
        foreach (var seg in segments)
        {
            string flightValue = seg.SelectedValue ?? string.Empty;
            sb.Append($"<tem:FlightInfo><tem:FlightValue>{EscapeXml(flightValue)}</tem:FlightValue></tem:FlightInfo>");
        }
        sb.Append("</tem:ListFlight>");
        sb.Append("</tem:FareDataInfo>");
        return sb.ToString();
    }

    private string BuildIssueSoap(string airline, string bookingCode) =>
        $"<soap:Envelope xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/'>" +
        $"<soap:Body><tem:Issue><tem:request>" +
        $"<tem:HeaderUser>{_headerUser}</tem:HeaderUser>" +
        $"<tem:HeaderPass>{_headerPass}</tem:HeaderPass>" +
        $"<tem:AgentAccount>{_agentAccount}</tem:AgentAccount>" +
        $"<tem:AgentPassword>{_agentPassword}</tem:AgentPassword>" +
        $"<tem:ProductKey>{_productKey}</tem:ProductKey>" +
        $"<tem:Currency>VND</tem:Currency>" +
        $"<tem:Language>en</tem:Language>" +
        $"<tem:IpRequest></tem:IpRequest>" +
        $"<tem:Airline>{EscapeXml(airline)}</tem:Airline>" +
        $"<tem:BookingCode>{EscapeXml(bookingCode)}</tem:BookingCode>" +
        $"</tem:request></tem:Issue></soap:Body></soap:Envelope>";

    // ── Response parsers ──────────────────────────────────────────────────────

    private List<FareDataDto> ParseSearchResponse(string xml, SearchFlightRequest req)
    {
        var result = new List<FareDataDto>();
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var fareNodes = doc.SelectNodes("//FareData");
            if (fareNodes == null) return result;

            int idx = 0;
            foreach (XmlElement fd in fareNodes)
            {
                string airline    = fd.SelectSingleNode("Airline/text()")?.Value ?? "";
                double fareAdt    = double.TryParse(fd.SelectSingleNode("FareAdt/text()")?.Value,  out var fa)  ? fa  : 0;
                double taxAdt     = double.TryParse(fd.SelectSingleNode("TaxAdt/text()")?.Value,   out var ta)  ? ta  : 0;
                double feeAdt     = double.TryParse(fd.SelectSingleNode("FeeAdt/text()")?.Value,   out var fea) ? fea : 0;
                double fareChd    = double.TryParse(fd.SelectSingleNode("FareChd/text()")?.Value,  out var fc)  ? fc  : 0;
                double taxChd     = double.TryParse(fd.SelectSingleNode("TaxChd/text()")?.Value,   out var tc)  ? tc  : 0;
                double fareInf    = double.TryParse(fd.SelectSingleNode("FareInf/text()")?.Value,  out var fi)  ? fi  : 0;
                double taxInf     = double.TryParse(fd.SelectSingleNode("TaxInf/text()")?.Value,   out var ti)  ? ti  : 0;
                string currency   = fd.SelectSingleNode("Currency/text()")?.Value ?? req.Currency;
                string apiSession = fd.SelectSingleNode("Session/text()")?.Value  ?? "";
                string fareDataId = fd.SelectSingleNode("FareDataId/text()")?.Value ?? idx.ToString();

                double totalAdult = fareAdt + taxAdt + feeAdt;
                double totalChild = fareChd + taxChd;
                double totalInf   = fareInf  + taxInf;
                double total      = (req.AdultCount * totalAdult) + (req.ChildCount * totalChild) + (req.InfantCount * totalInf);

                // ── Segments (with FlightValue for booking) ──────────────────
                // Datacom response: FareData → ListFlight → FlightInfo → FlightValue + ListSegment → Segment
                var segs = new List<FlightSegmentDto>();
                foreach (XmlElement flight in fd.SelectNodes(".//FlightInfo")!)
                {
                    string flightValue = flight.SelectSingleNode("FlightValue/text()")?.Value ?? "";

                    foreach (XmlElement seg in flight.SelectNodes(".//Segment")!)
                    {
                        string segAirline = seg.SelectSingleNode("Airline/text()")?.Value ?? airline;
                        string flightNum  = seg.SelectSingleNode("FlightNumber/text()")?.Value ?? "";
                        flightNum = PacificAirlineHelper.AppendPacificLabel(segAirline, flightNum);

                        segs.Add(new FlightSegmentDto
                        {
                            FlightNumber  = flightNum,
                            Airline       = segAirline,
                            Origin        = seg.SelectSingleNode("StartPoint/text()")?.Value ?? "",
                            Destination   = seg.SelectSingleNode("EndPoint/text()")?.Value   ?? "",
                            DepartTime    = ParseDateNode(seg, "StartTime"),
                            ArriveTime    = ParseDateNode(seg, "EndTime"),
                            CabinClass    = seg.SelectSingleNode("Class/text()")?.Value      ?? "",
                            AircraftType  = seg.SelectSingleNode("Plane/text()")?.Value,
                            StopCount     = 0,
                            SelectedValue = flightValue  // FlightValue needed for booking
                        });
                    }
                }

                // Encode session data as JSON for later use in book
                string sessionData = JsonSerializer.Serialize(new
                {
                    apiSession,
                    fareDataId
                });

                result.Add(new FareDataDto
                {
                    FareId           = $"dtc-{idx}-{fareDataId}",
                    Source           = FlightSource.Datacom,
                    Airline          = airline,
                    Origin           = req.Origin,
                    Destination      = req.Destination,
                    DepartDate       = req.DepartDate,
                    ReturnDate       = req.TripType == TripType.RoundTrip ? req.ReturnDate : null,
                    TripType         = req.TripType,
                    AdultCount       = req.AdultCount,
                    ChildCount       = req.ChildCount,
                    InfantCount      = req.InfantCount,
                    AdultFare        = (decimal)FlightEngineHelper.RoundFare(totalAdult, currency),
                    ChildFare        = (decimal)FlightEngineHelper.RoundFare(totalChild, currency),
                    InfantFare       = (decimal)FlightEngineHelper.RoundFare(totalInf,   currency),
                    TaxAmount        = (decimal)(taxAdt + taxChd + taxInf),
                    ServiceFee       = 0,
                    TotalFare        = (decimal)FlightEngineHelper.RoundFare(total, currency),
                    Currency         = currency,
                    SessionData      = sessionData,
                    OutboundSegments = segs,
                    ReturnSegments   = [],
                    CachedAt         = DateTime.UtcNow,
                    ExpiresAt        = DateTime.UtcNow.AddHours(1)
                });
                idx++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Datacom] ParseSearchResponse failed");
        }
        return result;
    }

    private static BookResultDto ParseBookResponse(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var bookingNodes = doc.SelectNodes("//Booking") ?? doc.SelectNodes("//BookingResult");
            if (bookingNodes == null || bookingNodes.Count == 0)
                return Fail("[Datacom] No booking result in response");

            var codes       = new List<string>();
            string? lastErr = null;
            DateTime? expiry = null;

            foreach (XmlElement node in bookingNodes)
            {
                string? code = node.SelectSingleNode("BookingCode/text()")?.Value;
                string? err  = node.SelectSingleNode("ErrorMessage/text()")?.Value;
                string? exp  = node.SelectSingleNode("ExpiryDate/text()")?.Value;

                if (!string.IsNullOrEmpty(code)) codes.Add(code);
                if (!string.IsNullOrEmpty(err))  lastErr = err;
                if (exp != null && DateTime.TryParse(exp, out var dt)) expiry = dt;
            }

            if (codes.Count > 0)
                return new BookResultDto
                {
                    IsSuccess   = true,
                    BookingCode = string.Join("|", codes),
                    ExpiresAt   = expiry ?? DateTime.UtcNow.AddMinutes(30)
                };

            return Fail(lastErr ?? "[Datacom] Booking failed");
        }
        catch (Exception ex)
        {
            return Fail($"[Datacom] ParseBookResponse: {ex.Message}");
        }
    }

    private static List<string> ParseIssueResponse(string xml)
    {
        var numbers = new List<string>();
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var nodes = doc.SelectNodes("//Ticket");
            if (nodes != null)
            {
                foreach (XmlElement ticket in nodes.Cast<XmlElement>())
                {
                    string? num = ticket.SelectSingleNode("TicketNumber/text()")?.Value;
                    if (!string.IsNullOrEmpty(num)) numbers.Add(num);
                }
            }
        }
        catch { /* swallow */ }
        return numbers;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string?> PostSoapAsync(string clientName, string soapBody, string action, CancellationToken ct)
    {
        string endpointUrl = _config["Datacom:EndpointUrl"]
            ?? throw new InvalidOperationException("Datacom:EndpointUrl not configured");

        using var client  = _http.CreateClient(clientName);
        using var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"\"{action}\"");

        var response = await client.PostAsync(endpointUrl, content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static (string ApiSession, string FareDataId) ParseDatacomSession(string? sessionData)
    {
        if (string.IsNullOrWhiteSpace(sessionData))
            return (string.Empty, "0");
        try
        {
            using var doc  = JsonDocument.Parse(sessionData);
            string session = doc.RootElement.TryGetProperty("apiSession", out var s)   ? s.GetString() ?? "" : "";
            string fdId    = doc.RootElement.TryGetProperty("fareDataId",  out var fid) ? fid.GetString() ?? "0" : "0";
            return (session, fdId);
        }
        catch { return (sessionData, "0"); } // fallback: treat raw string as session
    }

    private static DateTime ParseDateNode(XmlElement el, string tag)
    {
        string? val = el.SelectSingleNode($"{tag}/text()")?.Value;
        return DateTime.TryParse(val, out var dt) ? dt : DateTime.MinValue;
    }

    private static string EscapeXml(string? val) =>
        (val ?? string.Empty)
            .Replace("&", "&amp;").Replace("<", "&lt;")
            .Replace(">", "&gt;").Replace("\"", "&quot;");

    private static BookResultDto Fail(string msg) =>
        new() { IsSuccess = false, ErrorMessage = msg };
}

// ─────────────────────────────────────────────────────────────────────────────
// KiwiEngine — REST GET (tequila-api.kiwi.com)
// Migrated from KiwiEngine.cs
// ─────────────────────────────────────────────────────────────────────────────

public sealed class KiwiEngine : IFlightEngine
{
    private readonly string _baseUrl;
    private readonly string _apiKey;

    private readonly ILogger<KiwiEngine> _logger;
    private readonly IHttpClientFactory   _http;

    public FlightSource Source    => FlightSource.Kiwi;
    public bool         IsEnabled => true;

    public KiwiEngine(ILogger<KiwiEngine> logger, IHttpClientFactory http, IConfiguration config)
    {
        _logger  = logger;
        _http    = http;

        _baseUrl = config["Kiwi:BaseUrl"] ?? throw new InvalidOperationException("Missing config Kiwi:BaseUrl");
        _apiKey  = config["Kiwi:ApiKey"]  ?? throw new InvalidOperationException("Missing config Kiwi:ApiKey");
    }

    public async Task<IEnumerable<FareDataDto>> SearchFlightAsync(
        SearchFlightRequest req, CancellationToken ct = default)
    {
        try
        {
            bool isRt = req.TripType == TripType.RoundTrip;
            string url =
                $"{_baseUrl}?adults={req.AdultCount}&children={req.ChildCount}&infants={req.InfantCount}" +
                $"&fly_from={req.Origin}&fly_to={req.Destination}" +
                $"&dateFrom={req.DepartDate:dd/MM/yyyy}&dateTo={req.DepartDate:dd/MM/yyyy}";

            if (isRt && req.ReturnDate.HasValue)
                url += $"&return_from={req.ReturnDate.Value:dd/MM/yyyy}&return_to={req.ReturnDate.Value:dd/MM/yyyy}";

            using var client = _http.CreateClient("KiwiWS");
            client.DefaultRequestHeaders.Remove("apikey");
            client.DefaultRequestHeaders.Add("apikey", _apiKey);

            var httpResp = await client.GetAsync(url, ct);
            httpResp.EnsureSuccessStatusCode();

            string json = await httpResp.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json)) return [];

            using var doc = JsonDocument.Parse(json);
            return ParseKiwiResponse(doc, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kiwi] SearchFlightAsync failed");
            return [];
        }
    }

    public async Task<FareDataDto?> VerifyFareAsync(string fareId, string sessionData, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return null; // Kiwi uses booking_token at book time; no separate verify
    }

    public async Task<BookResultDto> BookFlightAsync(
        BookFlightRequest req, FareDataDto fareData, CancellationToken ct = default)
    {
        // Kiwi booking is through their hosted booking flow using the booking_token.
        // Direct API booking is not supported in the free/standard Tequila tier.
        await Task.CompletedTask;
        return new BookResultDto { IsSuccess = false, ErrorMessage = "[Kiwi] Direct booking not supported; use booking_token redirect flow." };
    }

    public async Task<IssueTicketResultDto> IssueTicketAsync(string bookingCode, string sessionData, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return new IssueTicketResultDto { IsSuccess = false, ErrorMessage = "[Kiwi] Issue ticket not applicable." };
    }

    /// <summary>Kiwi does not expose a baggage ancillary API. Returns empty.</summary>
    public Task<BaggageInfoDto> GetBaggagesAsync(
        FareDataDto fareData, CancellationToken ct = default)
        => Task.FromResult(new BaggageInfoDto());

    /// <summary>Kiwi does not expose fare rule text via the Tequila API. Returns empty.</summary>
    public Task<List<FareRuleGroupDto>> GetFareRulesAsync(
        FareDataDto fareData, int itinerary, CancellationToken ct = default)
        => Task.FromResult(new List<FareRuleGroupDto>());

    // ── Parsing ──────────────────────────────────────────────────────────────

    private List<FareDataDto> ParseKiwiResponse(JsonDocument doc, SearchFlightRequest req)
    {
        var result = new List<FareDataDto>();
        try
        {
            bool isRt = req.TripType == TripType.RoundTrip;
            string currency = doc.RootElement.TryGetProperty("currency", out var cur) ? cur.GetString() ?? req.Currency : req.Currency;

            if (!doc.RootElement.TryGetProperty("data", out var data)) return result;

            int idx = 0;
            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("routes", out var routes)) continue;
                var routeArr = routes.EnumerateArray().ToList();
                if (routeArr.Count == 0) continue;

                double fareAdt = 0, fareChd = 0, fareInf = 0;
                if (item.TryGetProperty("fare", out var fare))
                {
                    fareAdt = fare.TryGetProperty("adults",   out var a) ? a.GetDouble() : 0;
                    fareChd = fare.TryGetProperty("children", out var c) ? c.GetDouble() : 0;
                    fareInf = fare.TryGetProperty("infants",  out var i) ? i.GetDouble() : 0;
                }

                fareAdt = FlightEngineHelper.RoundFare(fareAdt, req.Currency);
                fareChd = FlightEngineHelper.RoundFare(fareChd, req.Currency);
                fareInf = FlightEngineHelper.RoundFare(fareInf, req.Currency);
                double total = (req.AdultCount * fareAdt) + (req.ChildCount * fareChd) + (req.InfantCount * fareInf);

                var departSegs = new List<FlightSegmentDto>();
                var returnSegs = new List<FlightSegmentDto>();

                if (item.TryGetProperty("route", out var route))
                {
                    foreach (var seg in route.EnumerateArray())
                    {
                        int rtFlag   = seg.TryGetProperty("return", out var r)  ? r.GetInt32()  : 0;
                        string al    = seg.TryGetProperty("airline",   out var a2) ? a2.GetString() ?? "" : "";
                        string fltNo = seg.TryGetProperty("flight_no", out var fn) ? fn.GetString() ?? "" : "";
                        string from  = seg.TryGetProperty("flyFrom",   out var ff) ? ff.GetString() ?? "" : "";
                        string to    = seg.TryGetProperty("flyTo",     out var ft) ? ft.GetString() ?? "" : "";
                        string cabin = seg.TryGetProperty("fare_classes", out var fc2) ? fc2.GetString() ?? "" : "";
                        string equip = seg.TryGetProperty("equipment",    out var eq)  ? eq.GetString() ?? "" : "";
                        DateTime dep = seg.TryGetProperty("local_departure", out var ld) && DateTime.TryParse(ld.GetString(), out var dt1) ? dt1 : DateTime.MinValue;
                        DateTime arr = seg.TryGetProperty("local_arrival",   out var la) && DateTime.TryParse(la.GetString(), out var dt2) ? dt2 : DateTime.MinValue;

                        fltNo = PacificAirlineHelper.AppendPacificLabel(al, fltNo);

                        var segDto = new FlightSegmentDto
                        {
                            FlightNumber = fltNo,
                            Airline      = al,
                            Origin       = from,
                            Destination  = to,
                            DepartTime   = dep,
                            ArriveTime   = arr,
                            CabinClass   = cabin,
                            AircraftType = equip,
                            StopCount    = 0
                        };

                        if (rtFlag == 0) departSegs.Add(segDto);
                        else             returnSegs.Add(segDto);
                    }
                }

                string bookingToken   = item.TryGetProperty("booking_token", out var bt) ? bt.GetString() ?? "" : "";
                string platingCarrier = departSegs.FirstOrDefault()?.Airline ?? "";

                result.Add(new FareDataDto
                {
                    FareId           = $"kiwi-{idx++}-{bookingToken[..Math.Min(12, bookingToken.Length)]}",
                    Source           = FlightSource.Kiwi,
                    Airline          = platingCarrier,
                    Origin           = req.Origin,
                    Destination      = req.Destination,
                    DepartDate       = req.DepartDate,
                    ReturnDate       = isRt ? req.ReturnDate : null,
                    TripType         = req.TripType,
                    AdultCount       = req.AdultCount,
                    ChildCount       = req.ChildCount,
                    InfantCount      = req.InfantCount,
                    AdultFare        = (decimal)fareAdt,
                    ChildFare        = (decimal)fareChd,
                    InfantFare       = (decimal)fareInf,
                    TaxAmount        = 0,
                    ServiceFee       = 0,
                    TotalFare        = (decimal)total,
                    Currency         = req.Currency,
                    SessionData      = bookingToken,
                    OutboundSegments = departSegs,
                    ReturnSegments   = returnSegs,
                    CachedAt         = DateTime.UtcNow,
                    ExpiresAt        = DateTime.UtcNow.AddMinutes(30)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Kiwi] ParseKiwiResponse failed");
        }
        return result;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// PkfareEngine — REST POST + gzip (hph-alotrip-api.pkfare.com)
// Migrated from PkfareEngine.cs
// VerifyFare implements /json/precisePricing_V6 (checks price + seat availability)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class PkfareEngine : IFlightEngine
{
    private readonly ILogger<PkfareEngine> _logger;
    private readonly IHttpClientFactory     _http;
    private readonly IConfiguration         _config;

    public FlightSource Source    => FlightSource.Pkfare;
    public bool         IsEnabled => true;

    public PkfareEngine(ILogger<PkfareEngine> logger, IHttpClientFactory http, IConfiguration config)
    {
        _logger  = logger;
        _http    = http;
        _config  = config;
    }

    public async Task<IEnumerable<FareDataDto>> SearchFlightAsync(
        SearchFlightRequest req, CancellationToken ct = default)
    {
        try
        {
            string baseUrl    = _config["Pkfare:Url"] ?? throw new InvalidOperationException("Missing config Pkfare:Url");
            string partnerId  = _config["Pkfare:PartnerId"]  ?? string.Empty;
            string partnerKey = _config["Pkfare:PartnerKey"] ?? string.Empty;

            string sign = ComputeMd5(partnerId + partnerKey);
            string url  = $"{baseUrl}/json/shoppingV4";
            bool   isRt = req.TripType == TripType.RoundTrip;

            var legs = new List<object>
            {
                new { departureDate = req.DepartDate.ToString("yyyy-MM-dd"), destination = req.Destination, origin = req.Origin }
            };
            if (isRt && req.ReturnDate.HasValue)
                legs.Add(new { departureDate = req.ReturnDate.Value.ToString("yyyy-MM-dd"), destination = req.Origin, origin = req.Destination });

            var body = new
            {
                authentication = new { partnerId, sign },
                search = new
                {
                    adults   = req.AdultCount,
                    children = req.ChildCount,
                    infants  = req.InfantCount,
                    nonstop  = 0,
                    solutions = 0,
                    searchAirLegs = legs
                }
            };

            string json = JsonSerializer.Serialize(body);
            using var client  = _http.CreateClient("PkfareWS");
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpResp = await client.PostAsync(url, content, ct);
            httpResp.EnsureSuccessStatusCode();

            string responseText = await DecompressResponseAsync(httpResp, ct);
            if (string.IsNullOrWhiteSpace(responseText)) return [];

            using var doc = JsonDocument.Parse(responseText);
            if (!doc.RootElement.TryGetProperty("errorCode", out var ec) || ec.GetString() != "0")
                return [];

            return ParsePkfareResponse(doc, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pkfare] SearchFlightAsync failed");
            return [];
        }
    }

    /// <summary>
    /// Calls /json/precisePricing_V6 to verify seat availability and price change.
    /// Returns null if price has changed beyond tolerance or seats unavailable.
    /// </summary>
    public async Task<FareDataDto?> VerifyFareAsync(
        string fareId, string sessionData, CancellationToken ct = default)
    {
        try
        {
            // sessionData = JSON: { "solutionId":"xxx", "segments": [...] }
            // We pass back the same fareData if the price is unchanged
            if (string.IsNullOrWhiteSpace(sessionData)) return null;

            string baseUrl    = _config["Pkfare:Url"] ?? throw new InvalidOperationException("Missing config Pkfare:Url");
            string partnerId  = _config["Pkfare:PartnerId"]  ?? string.Empty;
            string partnerKey = _config["Pkfare:PartnerKey"] ?? string.Empty;
            string sign       = ComputeMd5(partnerId + partnerKey);

            // Parse stored session (from search result)
            PkfareVerifySession? vs = null;
            try { vs = JsonSerializer.Deserialize<PkfareVerifySession>(sessionData); } catch { }
            if (vs is null) return null;

            // Build journeys
            var journey0 = vs.DepartSegments.Select(s => (object)new
            {
                airline        = s.Airline,
                flightNum      = s.FlightNum,
                arrival        = s.ArrivalAirport,
                arrivalDate    = s.ArrivalDate,
                arrivalTime    = s.ArrivalTime,
                departure      = s.DepartAirport,
                departureDate  = s.DepartDate,
                departureTime  = s.DepartTime,
                bookingCode    = s.BookingCode
            }).ToList();

            dynamic journeys = new System.Dynamic.ExpandoObject();
            ((IDictionary<string, object>)journeys)["journey_0"] = journey0;

            if (vs.ReturnSegments?.Count > 0)
            {
                var journey1 = vs.ReturnSegments.Select(s => (object)new
                {
                    airline       = s.Airline,
                    flightNum     = s.FlightNum,
                    arrival       = s.ArrivalAirport,
                    arrivalDate   = s.ArrivalDate,
                    arrivalTime   = s.ArrivalTime,
                    departure     = s.DepartAirport,
                    departureDate = s.DepartDate,
                    departureTime = s.DepartTime,
                    bookingCode   = s.BookingCode
                }).ToList();
                ((IDictionary<string, object>)journeys)["journey_1"] = journey1;
            }

            var bodyObj = new
            {
                authentication = new { partnerId, sign },
                pricing = new
                {
                    adults     = vs.Adults,
                    children   = vs.Children,
                    infants    = vs.Infants,
                    solutionId = vs.SolutionId,
                    journeys
                }
            };

            string jsonReq     = JsonSerializer.Serialize(bodyObj);
            string verifyUrl   = $"{baseUrl}/json/precisePricing_V6";
            using var client   = _http.CreateClient("PkfareWS");
            using var content  = new StringContent(jsonReq, Encoding.UTF8, "application/json");
            var httpResp       = await client.PostAsync(verifyUrl, content, ct);
            httpResp.EnsureSuccessStatusCode();

            string respText = await DecompressResponseAsync(httpResp, ct);
            if (string.IsNullOrWhiteSpace(respText)) return null;

            using var respDoc = JsonDocument.Parse(respText);
            if (!respDoc.RootElement.TryGetProperty("errorCode", out var ec) || ec.GetString() != "0")
            {
                _logger.LogWarning("[Pkfare] precisePricing_V6 error: {Msg}",
                    respDoc.RootElement.TryGetProperty("errorMsg", out var em) ? em.GetString() : "unknown");
                return null;
            }

            if (!respDoc.RootElement.TryGetProperty("data", out var data)) return null;
            if (!data.TryGetProperty("solution", out var sol)) return null;

            // Check seat availability
            if (data.TryGetProperty("segments", out var segsArr))
            {
                foreach (var seg in segsArr.EnumerateArray())
                {
                    if (seg.TryGetProperty("availabilityCount", out var ac) && ac.GetInt32() <= 0)
                    {
                        _logger.LogWarning("[Pkfare] No seats available on one of the segments");
                        return null;
                    }
                }
            }

            // Check price — return null if price jumped significantly
            string solCurrency = sol.TryGetProperty("currency", out var sc) ? sc.GetString() ?? "USD" : "USD";
            double adtFare = sol.TryGetProperty("adtFare", out var af) ? af.GetDouble() : 0;
            double adtTax  = sol.TryGetProperty("adtTax",  out var at) ? at.GetDouble() : 0;
            double chdFare = sol.TryGetProperty("chdFare", out var cf) ? cf.GetDouble() : 0;
            double chdTax  = sol.TryGetProperty("chdTax",  out var ct2) ? ct2.GetDouble() : 0;
            double infFare = sol.TryGetProperty("infFare", out var inf) ? inf.GetDouble() : 0;
            double infTax  = sol.TryGetProperty("infTax",  out var itx) ? itx.GetDouble() : 0;

            double newPrice = (vs.Adults    * (adtFare + adtTax))
                            + (vs.Children  * (chdFare + chdTax))
                            + (vs.Infants   * (infFare + infTax));

            // Return updated FareDataDto with verified price
            return new FareDataDto
            {
                FareId       = fareId,
                Source       = FlightSource.Pkfare,
                AdultFare    = (decimal)(adtFare + adtTax),
                ChildFare    = (decimal)(chdFare + chdTax),
                InfantFare   = (decimal)(infFare + infTax),
                TotalFare    = (decimal)newPrice,
                Currency     = vs.OutputCurrency,
                SessionData  = sessionData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pkfare] VerifyFareAsync failed");
            return null;
        }
    }

    /// <summary>
    /// Calls /json/bookingV4 to create a Pkfare order.
    /// SessionData must be a JSON-serialized <see cref="PkfareVerifySession"/> from VerifyFareAsync.
    /// </summary>
    public async Task<BookResultDto> BookFlightAsync(
        BookFlightRequest req, FareDataDto fareData, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fareData.SessionData))
                return new BookResultDto { IsSuccess = false, ErrorMessage = "[Pkfare] SessionData is missing." };

            var vs = JsonSerializer.Deserialize<PkfareVerifySession>(fareData.SessionData);
            if (vs is null)
                return new BookResultDto { IsSuccess = false, ErrorMessage = "[Pkfare] Invalid SessionData." };

            string baseUrl    = _config["Pkfare:Url"]        ?? "https://hph-alotrip-api.pkfare.com";
            string partnerId  = _config["Pkfare:PartnerId"]  ?? string.Empty;
            string partnerKey = _config["Pkfare:PartnerKey"] ?? string.Empty;
            string sign       = ComputeMd5(partnerId + partnerKey);

            // Build passenger list
            var passengers = req.Passengers.Select(p => new
            {
                fareType   = p.Type switch
                {
                    PassengerType.Child  => "CHD",
                    PassengerType.Infant => "INF",
                    _                    => "ADT"
                },
                firstName    = p.FirstName,
                lastName     = p.LastName,
                gender       = p.Gender?.ToUpperInvariant() == "F" ? "F" : "M",
                birthday     = p.BirthDate?.ToString("yyyy-MM-dd") ?? "",
                passportNo   = p.PassportNo      ?? "",
                passportExpiry = p.PassportExpiry?.ToString("yyyy-MM-dd") ?? "",
                nationality  = p.Nationality      ?? "VN"
            }).ToList();

            var body = new
            {
                authentication = new { partnerId, sign },
                order = new
                {
                    adults       = vs.Adults,
                    children     = vs.Children,
                    infants      = vs.Infants,
                    solutionId   = vs.SolutionId,
                    solutionKey  = vs.SolutionKey,
                    contactName  = req.ContactName,
                    contactEmail = req.ContactEmail,
                    contactPhone = req.ContactPhone,
                    passengers
                }
            };

            string json    = JsonSerializer.Serialize(body);
            string bookUrl = $"{baseUrl}/json/bookingV4";

            using var client  = _http.CreateClient("PkfareWS");
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpResp = await client.PostAsync(bookUrl, content, ct);
            httpResp.EnsureSuccessStatusCode();

            string respText = await DecompressResponseAsync(httpResp, ct);
            if (string.IsNullOrWhiteSpace(respText))
                return new BookResultDto { IsSuccess = false, ErrorMessage = "[Pkfare] Empty booking response." };

            using var doc = JsonDocument.Parse(respText);
            if (!doc.RootElement.TryGetProperty("errorCode", out var ec) || ec.GetString() != "0")
            {
                string msg = doc.RootElement.TryGetProperty("errorMsg", out var em) ? em.GetString() ?? "" : "booking failed";
                _logger.LogWarning("[Pkfare] BookFlight error: {Msg}", msg);
                return new BookResultDto { IsSuccess = false, ErrorMessage = $"[Pkfare] {msg}" };
            }

            // Extract orderNo from response
            string orderNo = "";
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("orderNo", out var on))
                orderNo = on.GetString() ?? "";

            _logger.LogInformation("[Pkfare] Booking success. OrderNo: {OrderNo}", orderNo);

            return new BookResultDto
            {
                IsSuccess    = true,
                BookingCode  = orderNo,
                PnrCode      = orderNo,
                TotalAmount  = fareData.TotalFare
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pkfare] BookFlightAsync failed");
            return new BookResultDto { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<IssueTicketResultDto> IssueTicketAsync(string bookingCode, string sessionData, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return new IssueTicketResultDto { IsSuccess = false, ErrorMessage = "[Pkfare] Issue ticket not yet implemented." };
    }

    /// <summary>Pkfare does not expose a baggage ancillary API in the current integration. Returns empty.</summary>
    public Task<BaggageInfoDto> GetBaggagesAsync(
        FareDataDto fareData, CancellationToken ct = default)
        => Task.FromResult(new BaggageInfoDto());

    /// <summary>Pkfare does not expose fare rule text via the current API tier. Returns empty.</summary>
    public Task<List<FareRuleGroupDto>> GetFareRulesAsync(
        FareDataDto fareData, int itinerary, CancellationToken ct = default)
        => Task.FromResult(new List<FareRuleGroupDto>());

    // ── Parsing ───────────────────────────────────────────────────────────────

    private List<FareDataDto> ParsePkfareResponse(JsonDocument doc, SearchFlightRequest req)
    {
        var result = new List<FareDataDto>();
        try
        {
            if (!doc.RootElement.TryGetProperty("data", out var data)) return result;
            if (!data.TryGetProperty("solutions", out var solutions)) return result;

            int idx = 0;
            foreach (var sol in solutions.EnumerateArray())
            {
                double fareAdt  = sol.TryGetProperty("adtFare",  out var ap) ? ap.GetDouble() : 0;
                double taxAdt   = sol.TryGetProperty("adtTax",   out var at) ? at.GetDouble() : 0;
                double fareChd  = sol.TryGetProperty("chdFare",  out var cp) ? cp.GetDouble() : 0;
                double taxChd   = sol.TryGetProperty("chdTax",   out var ct2) ? ct2.GetDouble() : 0;
                double fareInf  = sol.TryGetProperty("infFare",  out var ip) ? ip.GetDouble() : 0;
                double taxInf   = sol.TryGetProperty("infTax",   out var it) ? it.GetDouble() : 0;
                string solCur   = sol.TryGetProperty("currency", out var sc) ? sc.GetString() ?? req.Currency : req.Currency;

                double totalAdt = FlightEngineHelper.RoundFare(fareAdt + taxAdt, req.Currency);
                double totalChd = FlightEngineHelper.RoundFare(fareChd + taxChd, req.Currency);
                double totalInf = FlightEngineHelper.RoundFare(fareInf + taxInf, req.Currency);
                double total    = (req.AdultCount * totalAdt) + (req.ChildCount * totalChd) + (req.InfantCount * totalInf);

                var departSegs = ParsePkfareSegments(sol, 0);
                var returnSegs = req.TripType == TripType.RoundTrip
                               ? ParsePkfareSegments(sol, 1)
                               : new List<FlightSegmentDto>();

                string solId       = sol.TryGetProperty("solutionId",  out var si)  ? si.GetString() ?? "" : "";
                string solKey      = sol.TryGetProperty("solutionKey", out var sk)  ? sk.GetString() ?? "" : "";
                string platingAl   = departSegs.FirstOrDefault()?.Airline ?? "";

                // Build verify-session payload for VerifyFareAsync
                var verifySession = new PkfareVerifySession
                {
                    SolutionId     = solId,
                    SolutionKey    = solKey,
                    Adults         = req.AdultCount,
                    Children       = req.ChildCount,
                    Infants        = req.InfantCount,
                    OutputCurrency = req.Currency,
                    DepartSegments = departSegs.Select(s => new PkfareSegmentInfo
                    {
                        Airline       = s.Airline,
                        FlightNum     = s.FlightNumber.Replace(s.Airline, "").Trim(),
                        DepartAirport = s.Origin,
                        ArrivalAirport = s.Destination,
                        DepartDate    = s.DepartTime.ToString("yyyy-MM-dd"),
                        DepartTime    = s.DepartTime.ToString("HH:mm"),
                        ArrivalDate   = s.ArriveTime.ToString("yyyy-MM-dd"),
                        ArrivalTime   = s.ArriveTime.ToString("HH:mm"),
                        BookingCode   = s.CabinClass
                    }).ToList(),
                    ReturnSegments = returnSegs.Select(s => new PkfareSegmentInfo
                    {
                        Airline       = s.Airline,
                        FlightNum     = s.FlightNumber.Replace(s.Airline, "").Trim(),
                        DepartAirport = s.Origin,
                        ArrivalAirport = s.Destination,
                        DepartDate    = s.DepartTime.ToString("yyyy-MM-dd"),
                        DepartTime    = s.DepartTime.ToString("HH:mm"),
                        ArrivalDate   = s.ArriveTime.ToString("yyyy-MM-dd"),
                        ArrivalTime   = s.ArriveTime.ToString("HH:mm"),
                        BookingCode   = s.CabinClass
                    }).ToList()
                };

                result.Add(new FareDataDto
                {
                    FareId           = $"pkfare-{idx++}-{solId}",
                    Source           = FlightSource.Pkfare,
                    Airline          = platingAl,
                    Origin           = req.Origin,
                    Destination      = req.Destination,
                    DepartDate       = req.DepartDate,
                    ReturnDate       = req.TripType == TripType.RoundTrip ? req.ReturnDate : null,
                    TripType         = req.TripType,
                    AdultCount       = req.AdultCount,
                    ChildCount       = req.ChildCount,
                    InfantCount      = req.InfantCount,
                    AdultFare        = (decimal)totalAdt,
                    ChildFare        = (decimal)totalChd,
                    InfantFare       = (decimal)totalInf,
                    TaxAmount        = 0,
                    ServiceFee       = 0,
                    TotalFare        = (decimal)total,
                    Currency         = req.Currency,
                    SessionData      = JsonSerializer.Serialize(verifySession),
                    OutboundSegments = departSegs,
                    ReturnSegments   = returnSegs,
                    CachedAt         = DateTime.UtcNow,
                    ExpiresAt        = DateTime.UtcNow.AddMinutes(30)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pkfare] ParsePkfareResponse failed");
        }
        return result;
    }

    private static List<FlightSegmentDto> ParsePkfareSegments(JsonElement sol, int legIndex)
    {
        var segs = new List<FlightSegmentDto>();
        string legsKey = legIndex == 0 ? "departureFlight" : "returnFlight";
        if (!sol.TryGetProperty(legsKey, out var leg)) return segs;

        foreach (var seg in leg.EnumerateArray())
        {
            string al      = seg.TryGetProperty("carrier",      out var a2) ? a2.GetString() ?? "" : "";
            string fltNo   = seg.TryGetProperty("flightNumber", out var fn) ? fn.GetString() ?? "" : "";
            string from    = seg.TryGetProperty("depAirport",   out var da) ? da.GetString() ?? "" : "";
            string to      = seg.TryGetProperty("arrAirport",   out var aa) ? aa.GetString() ?? "" : "";
            string cabin   = seg.TryGetProperty("cabinClass",   out var cc) ? cc.GetString() ?? "" : "";
            string bkCode  = seg.TryGetProperty("bookingCode",  out var bc) ? bc.GetString() ?? cabin : cabin;
            string equip   = seg.TryGetProperty("aircraftCode", out var ac) ? ac.GetString() ?? "" : "";
            DateTime dep   = seg.TryGetProperty("depTime", out var dt) && DateTime.TryParse(dt.GetString(), out var d1) ? d1 : DateTime.MinValue;
            DateTime arr   = seg.TryGetProperty("arrTime", out var at) && DateTime.TryParse(at.GetString(), out var d2) ? d2 : DateTime.MinValue;

            fltNo = PacificAirlineHelper.AppendPacificLabel(al, fltNo);

            segs.Add(new FlightSegmentDto
            {
                FlightNumber = fltNo,
                Airline      = al,
                Origin       = from,
                Destination  = to,
                DepartTime   = dep,
                ArriveTime   = arr,
                CabinClass   = cabin,
                AircraftType = equip,
                StopCount    = 0,
                SelectedValue = bkCode
            });
        }
        return segs;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static async Task<string> DecompressResponseAsync(
        System.Net.Http.HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.Content.Headers.ContentEncoding.Contains("gzip"))
        {
            await using var stream     = await resp.Content.ReadAsStreamAsync(ct);
            await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var reader           = new StreamReader(gzipStream, Encoding.UTF8);
            return await reader.ReadToEndAsync(ct);
        }
        return await resp.Content.ReadAsStringAsync(ct);
    }

    private static string ComputeMd5(string input)
    {
        byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var    sb    = new StringBuilder();
        foreach (byte b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}

// ── Pkfare session/verify DTOs ────────────────────────────────────────────────

file sealed class PkfareVerifySession
{
    [JsonPropertyName("solutionId")]     public string SolutionId     { get; set; } = "";
    [JsonPropertyName("solutionKey")]    public string SolutionKey    { get; set; } = "";
    [JsonPropertyName("adults")]         public int    Adults         { get; set; }
    [JsonPropertyName("children")]       public int    Children       { get; set; }
    [JsonPropertyName("infants")]        public int    Infants        { get; set; }
    [JsonPropertyName("outputCurrency")] public string OutputCurrency { get; set; } = "VND";
    [JsonPropertyName("departSegs")]     public List<PkfareSegmentInfo> DepartSegments { get; set; } = [];
    [JsonPropertyName("returnSegs")]     public List<PkfareSegmentInfo>? ReturnSegments { get; set; }
}

file sealed class PkfareSegmentInfo
{
    [JsonPropertyName("airline")]  public string Airline        { get; set; } = "";
    [JsonPropertyName("fltNum")]   public string FlightNum      { get; set; } = "";
    [JsonPropertyName("depAp")]    public string DepartAirport  { get; set; } = "";
    [JsonPropertyName("arrAp")]    public string ArrivalAirport { get; set; } = "";
    [JsonPropertyName("depDate")]  public string DepartDate     { get; set; } = "";
    [JsonPropertyName("depTime")]  public string DepartTime     { get; set; } = "";
    [JsonPropertyName("arrDate")]  public string ArrivalDate    { get; set; } = "";
    [JsonPropertyName("arrTime")]  public string ArrivalTime    { get; set; } = "";
    [JsonPropertyName("bkCode")]   public string BookingCode    { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────
// MaybayEngine — SOAP POST (data.maybay.net/AirDataWS.asmx)
// Migrated from MaybayEngine.cs
// Book: Lcc_MakeReservationV2 (one-way per leg)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class MaybayEngine : IFlightEngine
{
    private readonly string _endpointBase;
    private readonly string _headerUser;
    private readonly string _headerPassword;
    private readonly string _email;
    private readonly string _password;
    private readonly string _contactEmail;
    private readonly string _contactPhone;
    private readonly string _contactName;

    private readonly ILogger<MaybayEngine> _logger;
    private readonly IHttpClientFactory     _http;
    private readonly IConfiguration         _config;

    public FlightSource Source    => FlightSource.Maybay;
    public bool         IsEnabled => true;

    public MaybayEngine(ILogger<MaybayEngine> logger, IHttpClientFactory http, IConfiguration config)
    {
        _logger  = logger;
        _http    = http;
        _config  = config;

        _endpointBase   = config["Maybay:SoapEndpoint"]    ?? throw new InvalidOperationException("Missing config Maybay:SoapEndpoint");
        _headerUser     = config["Maybay:Username"]        ?? throw new InvalidOperationException("Missing config Maybay:Username");
        _headerPassword = config["Maybay:Password"]        ?? throw new InvalidOperationException("Missing config Maybay:Password");
        _email          = config["Maybay:Email"]           ?? throw new InvalidOperationException("Missing config Maybay:Email");
        _password       = config["Maybay:UserPassword"]    ?? throw new InvalidOperationException("Missing config Maybay:UserPassword");
        _contactEmail   = config["Maybay:ContactEmail"]    ?? "vemaybay@alotrip.vn";
        _contactPhone   = config["Maybay:ContactPhone"]    ?? "+84916463066";
        _contactName    = config["Maybay:ContactName"]     ?? "AloTrip Vietnam";
    }

    public async Task<IEnumerable<FareDataDto>> SearchFlightAsync(
        SearchFlightRequest req, CancellationToken ct = default)
    {
        try
        {
            int  limit  = int.Parse(_config["Maybay:MaxIndex"] ?? "100");
            bool isRt   = req.TripType == TripType.RoundTrip;
            int  itType = isRt ? 2 : 1;

            string soapBody = BuildSearchSoap(req, itType);
            string?  rawResp = await PostSoapAsync(soapBody, "Lcc_SearchFlight",
                                   $"{_endpointBase}?op=Lcc_SearchFlight", ct);
            if (rawResp is null) return [];

            var fares = ParseMaybayResponse(rawResp, req);
            return fares.OrderBy(f => f.TotalFare).Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Maybay] SearchFlightAsync failed");
            return [];
        }
    }

    public async Task<FareDataDto?> VerifyFareAsync(string fareId, string sessionData, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return null; // Maybay: no separate verify; price confirmed at booking
    }

    /// <summary>
    /// Books via Maybay Lcc_MakeReservationV2.
    /// For round-trips, calls the endpoint twice (once per leg) using the SelectedValue
    /// stored on each segment.
    /// </summary>
    public async Task<BookResultDto> BookFlightAsync(
        BookFlightRequest req, FareDataDto fareData, CancellationToken ct = default)
    {
        try
        {
            // Depart leg: airline from first outbound segment
            string departAirline = fareData.OutboundSegments.FirstOrDefault()?.Airline ?? "";
            string departSelectValue = fareData.OutboundSegments.FirstOrDefault()?.SelectedValue ?? "";
            double departTotalPrice  = (double)fareData.TotalFare;

            string departSoap = BuildMaybayBookSoap(
                departAirline,
                fareData.Origin, fareData.Destination,
                fareData.DepartDate.ToString("dd/MM/yyyy"),
                departSelectValue, departTotalPrice,
                req.Passengers);

            string? departRaw = await PostSoapAsync(departSoap, "Lcc_MakeReservationV2",
                                    $"{_endpointBase}?op=Lcc_MakeReservationV2", ct);
            if (departRaw is null)
                return Fail("[Maybay] No response from book");

            string departCode = ParseMaybayBookCode(departRaw);
            if (string.IsNullOrEmpty(departCode))
                return Fail("[Maybay] No booking code returned");

            string fullCode = departCode;

            // Return leg (round-trip)
            if (fareData.TripType == TripType.RoundTrip
                && fareData.ReturnDate.HasValue
                && fareData.ReturnSegments.Count > 0)
            {
                string returnAirline      = fareData.ReturnSegments.FirstOrDefault()?.Airline ?? departAirline;
                string returnSelectValue  = fareData.ReturnSegments.FirstOrDefault()?.SelectedValue ?? "";
                double returnTotalPrice   = (double)fareData.TotalFare; // same total; Maybay prices per-leg

                string returnSoap = BuildMaybayBookSoap(
                    returnAirline,
                    fareData.Destination, fareData.Origin,
                    fareData.ReturnDate.Value.ToString("dd/MM/yyyy"),
                    returnSelectValue, returnTotalPrice,
                    req.Passengers);

                string? returnRaw = await PostSoapAsync(returnSoap, "Lcc_MakeReservationV2",
                                        $"{_endpointBase}?op=Lcc_MakeReservationV2", ct);

                if (returnRaw != null)
                {
                    string returnCode = ParseMaybayBookCode(returnRaw);
                    if (!string.IsNullOrEmpty(returnCode))
                        fullCode += $"|{returnCode}";
                }
            }

            return new BookResultDto
            {
                IsSuccess   = true,
                BookingCode = fullCode,
                ExpiresAt   = DateTime.UtcNow.AddMinutes(30)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Maybay] BookFlightAsync failed");
            return Fail(ex.Message);
        }
    }

    public async Task<IssueTicketResultDto> IssueTicketAsync(string bookingCode, string sessionData, CancellationToken ct = default)
    {
        // Maybay LCC tickets are auto-issued on booking confirmation.
        await Task.CompletedTask;
        return new IssueTicketResultDto
        {
            IsSuccess     = true,
            TicketNumbers = [bookingCode],
            ErrorMessage  = null
        };
    }

    /// <summary>
    /// Retrieves baggage add-on options from Maybay SOAP Lcc_GetBaggages endpoint.
    /// Ported from legacy MaybayEngine.GetBaggage → GetBaggageMaybayOneWay.
    /// Calls the endpoint once per leg (one-way approach), applies fee markup,
    /// skips Vietnam Airlines (VN), handles QH zero-price special case.
    /// </summary>
    public async Task<BaggageInfoDto> GetBaggagesAsync(
        FareDataDto fareData, CancellationToken ct = default)
    {
        var result = new BaggageInfoDto();
        try
        {
            double feePercent = double.TryParse(_config["BaggageFee:Percent"], out var fp) ? fp : 0;
            double feeAmount  = double.TryParse(_config["BaggageFee:Amount"],  out var fa) ? fa : 0;

            string departAirline     = fareData.OutboundSegments.FirstOrDefault()?.Airline       ?? "";
            string departSelectValue = fareData.OutboundSegments.FirstOrDefault()?.SelectedValue ?? "";

            // ── Depart baggages (skip VN airline) ─────────────────────────────
            if (!string.IsNullOrEmpty(departSelectValue) && departAirline != "VN")
            {
                var departBags = await GetMaybayBaggagesOneWayAsync(
                    departAirline, departSelectValue, feePercent, feeAmount, ct);
                result.DepartBaggages.AddRange(departBags);
            }

            // ── Return baggages (round-trip only) ─────────────────────────────
            if (fareData.TripType == TripType.RoundTrip && fareData.ReturnSegments.Count > 0)
            {
                string returnAirline     = fareData.ReturnSegments.FirstOrDefault()?.Airline       ?? "";
                string returnSelectValue = fareData.ReturnSegments.FirstOrDefault()?.SelectedValue ?? "";

                if (!string.IsNullOrEmpty(returnSelectValue) && returnAirline != "VN")
                {
                    var returnBags = await GetMaybayBaggagesOneWayAsync(
                        returnAirline, returnSelectValue, feePercent, feeAmount, ct);
                    result.ReturnBaggages.AddRange(returnBags);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Maybay] GetBaggagesAsync failed");
        }
        return result;
    }

    /// <summary>Maybay does not expose fare rule text. Returns empty.</summary>
    public Task<List<FareRuleGroupDto>> GetFareRulesAsync(
        FareDataDto fareData, int itinerary, CancellationToken ct = default)
        => Task.FromResult(new List<FareRuleGroupDto>());

    /// <summary>
    /// Calls Maybay Lcc_GetBaggages with ItineraryType=1 for a single leg.
    /// Parses the BaggageDepart section from the XML response.
    /// </summary>
    private async Task<List<BaggageOptionDto>> GetMaybayBaggagesOneWayAsync(
        string airline, string selectValue,
        double feePercent, double feeAmount,
        CancellationToken ct)
    {
        var baggages = new List<BaggageOptionDto>();

        string soapBody =
            $"<x:Envelope xmlns:x='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/'>" +
            $"<x:Header><tem:Authentication>" +
            $"<tem:HeaderUser>{_headerUser}</tem:HeaderUser>" +
            $"<tem:HeaderPassword>{_headerPassword}</tem:HeaderPassword>" +
            $"</tem:Authentication></x:Header>" +
            $"<x:Body><tem:Lcc_GetBaggages>" +
            $"<tem:Email>{_email}</tem:Email>" +
            $"<tem:Password>{_password}</tem:Password>" +
            $"<tem:AirlineCode>{EscapeXml(airline)}</tem:AirlineCode>" +
            $"<tem:ItineraryType>1</tem:ItineraryType>" +
            $"<tem:DepartureSelectValue>{EscapeXml(selectValue)}</tem:DepartureSelectValue>" +
            $"<tem:ReturnSelectValue></tem:ReturnSelectValue>" +
            $"<tem:SessionAll></tem:SessionAll>" +
            $"<tem:isDomestic>true</tem:isDomestic>" +
            $"<tem:NumberOfAvailFlightsDepart>1</tem:NumberOfAvailFlightsDepart>" +
            $"<tem:NumberOfAvailFlightsReturn>0</tem:NumberOfAvailFlightsReturn>" +
            $"</tem:Lcc_GetBaggages></x:Body></x:Envelope>";

        string? rawResp = await PostSoapAsync(soapBody, "Lcc_GetBaggages",
                               $"{_endpointBase}?op=Lcc_GetBaggages", ct);
        if (rawResp is null) return baggages;

        // ── Parse XML response ────────────────────────────────────────────
        rawResp = rawResp.Replace("xmlns=\"http://tempuri.org/\"", "");
        var doc = new XmlDocument();
        doc.LoadXml(rawResp);

        var resultNodes = doc.GetElementsByTagName("Lcc_GetBaggagesResult");
        if (resultNodes.Count == 0) return baggages;

        var innerEl = resultNodes[0] as XmlElement;
        if (innerEl is null) return baggages;

        // Parse BaggageDepart section (one-way call → only depart data returned)
        var baggageDepartNode = innerEl.SelectSingleNode("BaggageDepart");
        if (baggageDepartNode is null) return baggages;

        var bagNodes = baggageDepartNode.SelectNodes(".//Baggage");
        if (bagNodes is null) return baggages;

        foreach (XmlElement bagEl in bagNodes)
        {
            string airlineCode = bagEl.SelectSingleNode("AirlineCode/text()")?.Value ?? airline;
            string code        = bagEl.SelectSingleNode("Code/text()")?.Value        ?? "";
            string value       = bagEl.SelectSingleNode("Value/text()")?.Value       ?? "";
            double price       = double.TryParse(bagEl.SelectSingleNode("Price/text()")?.Value, out var p) ? p : 0;
            string bagCurrency = bagEl.SelectSingleNode("Currency/text()")?.Value    ?? "VND";

            // Name: append "kg" (VJ raw value is handled by Datacom, Maybay always appends "kg")
            string name = value + "kg";

            // QH special case: price ≤ 0 → free; otherwise apply fee markup
            decimal finalPrice;
            if (airlineCode == "QH" && price <= 0)
                finalPrice = 0;
            else
                finalPrice = (decimal)(price + (price * feePercent / 100) + feeAmount);

            baggages.Add(new BaggageOptionDto
            {
                AirlineCode = airlineCode,
                Code        = code,
                Name        = name,
                Value       = value,
                Price       = finalPrice,
                Currency    = bagCurrency
            });
        }

        return baggages;
    }

    // ── SOAP builders ─────────────────────────────────────────────────────────

    private string BuildSearchSoap(SearchFlightRequest req, int itType) =>
        $"<x:Envelope xmlns:x='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/'>" +
        $"<x:Header><tem:Authentication>" +
        $"<tem:HeaderUser>{_headerUser}</tem:HeaderUser>" +
        $"<tem:HeaderPassword>{_headerPassword}</tem:HeaderPassword>" +
        $"</tem:Authentication></x:Header>" +
        $"<x:Body><tem:Lcc_SearchFlight>" +
        $"<tem:Email>{_email}</tem:Email>" +
        $"<tem:Password>{_password}</tem:Password>" +
        $"<tem:ItineraryType>{itType}</tem:ItineraryType>" +
        $"<tem:DepartureAirportCode>{req.Origin}</tem:DepartureAirportCode>" +
        $"<tem:DestinationAirportCode>{req.Destination}</tem:DestinationAirportCode>" +
        $"<tem:DepartureDate>{req.DepartDate:dd/MM/yyyy}</tem:DepartureDate>" +
        $"<tem:ReturnDate>{(req.ReturnDate ?? req.DepartDate):dd/MM/yyyy}</tem:ReturnDate>" +
        $"<tem:Adult>{req.AdultCount}</tem:Adult>" +
        $"<tem:Children>{req.ChildCount}</tem:Children>" +
        $"<tem:Infant>{req.InfantCount}</tem:Infant>" +
        $"</tem:Lcc_SearchFlight></x:Body></x:Envelope>";

    private string BuildMaybayBookSoap(
        string airline, string origin, string destination,
        string departureDate, string selectValue, double totalPrice,
        List<PassengerBookingDto> passengers)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < passengers.Count; i++)
        {
            var p       = passengers[i];
            string type = p.Type == PassengerType.Adult ? "ADT"
                        : p.Type == PassengerType.Child  ? "CHD" : "INF";
            string bday = p.BirthDate.HasValue ? p.BirthDate.Value.ToString("dd/MM/yyyy") : "01/01/1990";
            string pexp = p.PassportExpiry.HasValue ? p.PassportExpiry.Value.ToString("dd/MM/yyyy") : "";
            string pno  = p.PassportNo ?? "";

            sb.Append(
                $"<tem:Passenger>" +
                $"<tem:Index>{i}</tem:Index>" +
                $"<tem:FirstName>{EscapeXml(p.FirstName)}</tem:FirstName>" +
                $"<tem:LastName>{EscapeXml(p.LastName)}</tem:LastName>" +
                $"<tem:Type>{type}</tem:Type>" +
                $"<tem:Gender>{p.Gender}</tem:Gender>" +
                $"<tem:Email>{_contactEmail}</tem:Email>" +
                $"<tem:Phone>{_contactPhone}</tem:Phone>" +
                $"<tem:Birthday>{bday}</tem:Birthday>" +
                $"<tem:PassportExpiryDate>{pexp}</tem:PassportExpiryDate>" +
                $"<tem:PassportIssueCountry></tem:PassportIssueCountry>" +
                $"<tem:PassportNumber>{EscapeXml(pno)}</tem:PassportNumber>" +
                $"<tem:BaggageDeparture></tem:BaggageDeparture>" +
                $"<tem:BaggageReturn></tem:BaggageReturn>" +
                $"</tem:Passenger>");
        }

        int adt = passengers.Count(p => p.Type == PassengerType.Adult);
        int chd = passengers.Count(p => p.Type == PassengerType.Child);
        int inf = passengers.Count(p => p.Type == PassengerType.Infant);

        return
            $"<x:Envelope xmlns:x='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tem='http://tempuri.org/'>" +
            $"<x:Header><tem:Authentication>" +
            $"<tem:HeaderUser>{_headerUser}</tem:HeaderUser>" +
            $"<tem:HeaderPassword>{_headerPassword}</tem:HeaderPassword>" +
            $"</tem:Authentication></x:Header>" +
            $"<x:Body><tem:Lcc_MakeReservationV2>" +
            $"<tem:Email>{_email}</tem:Email>" +
            $"<tem:Password>{_password}</tem:Password>" +
            $"<tem:AirlineCode>{EscapeXml(airline)}</tem:AirlineCode>" +
            $"<tem:ItineraryType>1</tem:ItineraryType>" +
            $"<tem:DepartureAirportCode>{EscapeXml(origin)}</tem:DepartureAirportCode>" +
            $"<tem:DestinationAirportCode>{EscapeXml(destination)}</tem:DestinationAirportCode>" +
            $"<tem:DepartureDate>{departureDate}</tem:DepartureDate>" +
            $"<tem:ReturnDate></tem:ReturnDate>" +
            $"<tem:Adult>{adt}</tem:Adult>" +
            $"<tem:Children>{chd}</tem:Children>" +
            $"<tem:Infant>{inf}</tem:Infant>" +
            $"<tem:DepartureSelectValue>{EscapeXml(selectValue)}</tem:DepartureSelectValue>" +
            $"<tem:ReturnSelectValue></tem:ReturnSelectValue>" +
            $"<tem:TotalPriceDepart>{totalPrice}</tem:TotalPriceDepart>" +
            $"<tem:TotalPriceReturn></tem:TotalPriceReturn>" +
            $"<tem:ListPassengers>{sb}</tem:ListPassengers>" +
            $"<tem:ContactInfo>" +
            $"<tem:Gender>true</tem:Gender>" +
            $"<tem:FirstName>AloTrip</tem:FirstName>" +
            $"<tem:LastName>Vietnam</tem:LastName>" +
            $"<tem:Phone>{_contactPhone}</tem:Phone>" +
            $"<tem:Email>{_contactEmail}</tem:Email>" +
            $"<tem:ContactEmail>{_contactEmail}</tem:ContactEmail>" +
            $"<tem:Address>hanoi, hanoi</tem:Address>" +
            $"<tem:Company>Alotrip</tem:Company>" +
            $"</tem:ContactInfo>" +
            $"</tem:Lcc_MakeReservationV2></x:Body></x:Envelope>";
    }

    // ── Response parsers ──────────────────────────────────────────────────────

    private List<FareDataDto> ParseMaybayResponse(string xml, SearchFlightRequest req)
    {
        var result = new List<FareDataDto>();
        try
        {
            xml = xml.Replace("xmlns=\"http://tempuri.org/\"", "");

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var resultNodes = doc.GetElementsByTagName("Lcc_SearchFlightResult");
            if (resultNodes.Count == 0) return result;

            string innerXml = resultNodes[0]!.InnerXml;
            if (string.IsNullOrWhiteSpace(innerXml)) return result;

            var innerDoc = new XmlDocument();
            innerDoc.LoadXml($"<root>{innerXml}</root>");

            int idx = 0;
            foreach (XmlElement ff in innerDoc.SelectNodes("//FlightFare")!)
            {
                string airline     = ff.SelectSingleNode("Airline/text()")?.Value  ?? "";
                string from        = ff.SelectSingleNode("DepartureAirportCode/text()")?.Value  ?? req.Origin;
                string to          = ff.SelectSingleNode("DestinationAirportCode/text()")?.Value ?? req.Destination;
                double fareAdt     = double.TryParse(ff.SelectSingleNode("FareAdult/text()")?.Value,  out var fa) ? fa : 0;
                double fareChd     = double.TryParse(ff.SelectSingleNode("FareChild/text()")?.Value,  out var fc) ? fc : 0;
                double fareInf     = double.TryParse(ff.SelectSingleNode("FareInfant/text()")?.Value, out var fi) ? fi : 0;
                double taxAdt      = double.TryParse(ff.SelectSingleNode("TaxAdult/text()")?.Value,   out var ta) ? ta : 0;
                double taxChd      = double.TryParse(ff.SelectSingleNode("TaxChild/text()")?.Value,   out var tc) ? tc : 0;
                double taxInf      = double.TryParse(ff.SelectSingleNode("TaxInfant/text()")?.Value,  out var ti) ? ti : 0;
                string currency    = req.Currency;
                string selectValue = ff.SelectSingleNode("SelectValue/text()")?.Value ?? "";

                double totalAdt = FlightEngineHelper.RoundFare(fareAdt + taxAdt, currency);
                double totalChd = FlightEngineHelper.RoundFare(fareChd + taxChd, currency);
                double totalInf = FlightEngineHelper.RoundFare(fareInf + taxInf, currency);
                double total    = (req.AdultCount * totalAdt) + (req.ChildCount * totalChd) + (req.InfantCount * totalInf);

                // Segment (Maybay typically returns single segment per FlightFare)
                string fltNo      = ff.SelectSingleNode("FlightNumber/text()")?.Value ?? "";
                string cabinClass = ff.SelectSingleNode("Class/text()")?.Value ?? "";
                DateTime depTime  = ParseMaybayDate(ff, "DepartureTime");
                DateTime arrTime  = ParseMaybayDate(ff, "ArrivalTime");

                fltNo = PacificAirlineHelper.AppendPacificLabel(airline, fltNo);

                var seg = new FlightSegmentDto
                {
                    FlightNumber  = fltNo,
                    Airline       = airline,
                    Origin        = from,
                    Destination   = to,
                    DepartTime    = depTime,
                    ArriveTime    = arrTime,
                    CabinClass    = cabinClass,
                    StopCount     = 0,
                    SelectedValue = selectValue  // needed for booking
                };

                result.Add(new FareDataDto
                {
                    FareId           = $"maybay-{idx++}-{airline}-{depTime:yyyyMMddHHmm}",
                    Source           = FlightSource.Maybay,
                    Airline          = airline,
                    Origin           = req.Origin,
                    Destination      = req.Destination,
                    DepartDate       = req.DepartDate,
                    ReturnDate       = req.TripType == TripType.RoundTrip ? req.ReturnDate : null,
                    TripType         = req.TripType,
                    AdultCount       = req.AdultCount,
                    ChildCount       = req.ChildCount,
                    InfantCount      = req.InfantCount,
                    AdultFare        = (decimal)totalAdt,
                    ChildFare        = (decimal)totalChd,
                    InfantFare       = (decimal)totalInf,
                    TaxAmount        = (decimal)(taxAdt + taxChd + taxInf),
                    ServiceFee       = 0,
                    TotalFare        = (decimal)total,
                    Currency         = currency,
                    SessionData      = selectValue,
                    OutboundSegments = [seg],
                    ReturnSegments   = [],
                    CachedAt         = DateTime.UtcNow,
                    ExpiresAt        = DateTime.UtcNow.AddMinutes(30)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Maybay] ParseMaybayResponse failed");
        }
        return result;
    }

    private static string ParseMaybayBookCode(string xml)
    {
        try
        {
            xml = xml.Replace("xmlns=\"http://tempuri.org/\"", "");
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            // Try booking number node from Lcc_MakeReservationResult
            var resNodes = doc.GetElementsByTagName("Lcc_MakeReservationResult");
            if (resNodes.Count == 0) return string.Empty;

            var innerEl = resNodes[0] as XmlElement;
            // BookingNumber or similar
            string? code = innerEl?.SelectSingleNode("BookingNumber/text()")?.Value
                        ?? innerEl?.SelectSingleNode("BookingCode/text()")?.Value
                        ?? innerEl?.SelectSingleNode("PNR/text()")?.Value;

            return code ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private async Task<string?> PostSoapAsync(string soapBody, string action, string url, CancellationToken ct)
    {
        using var client  = _http.CreateClient("MaybayWS");
        using var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", action);

        var resp = await client.PostAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    private static DateTime ParseMaybayDate(XmlElement el, string tag)
    {
        string? val = el.SelectSingleNode($"{tag}/text()")?.Value;
        if (DateTime.TryParseExact(val, "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        return DateTime.TryParse(val, out var dt2) ? dt2 : DateTime.MinValue;
    }

    private static string EscapeXml(string? val) =>
        (val ?? string.Empty)
            .Replace("&", "&amp;").Replace("<", "&lt;")
            .Replace(">", "&gt;").Replace("\"", "&quot;");

    private static BookResultDto Fail(string msg) =>
        new() { IsSuccess = false, ErrorMessage = msg };
}

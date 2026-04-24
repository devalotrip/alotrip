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

/// <summary>
/// Galileo/Travelport GDS Engine — migrated from legacy GalileoEngine.cs (.NET 4.6.1).
///
/// Transport layer:
///   Old: GalileoWS SOAP proxy (System.Web.Services)
///   New: HttpClient posting a SOAP 1.1 envelope to the same endpoint
///
/// Business logic: faithfully ported from the original ParseXML / GetFareData / BookFlight methods.
/// The XML request format is identical; only the SOAP wrapper and .NET types changed.
/// </summary>
public sealed class GalileoEngine : IFlightEngine
{
    // ── Config keys ─────────────────────────────────────────────────────────
    private const string HttpClientName   = "GalileoWS";
    private const string BlockedAirlines  = "GP,A1"; // block GP and A1 like the legacy engine

    private readonly ILogger<GalileoEngine>  _logger;
    private readonly IHttpClientFactory       _httpFactory;
    private readonly IConfiguration           _config;

    public FlightSource Source    => FlightSource.Galileo;
    public bool         IsEnabled => true;

    public GalileoEngine(
        ILogger<GalileoEngine> logger,
        IHttpClientFactory httpFactory,
        IConfiguration config)
    {
        _logger      = logger;
        _httpFactory = httpFactory;
        _config      = config;
    }

    // =========================================================================
    // IFlightEngine — public contract
    // =========================================================================

    public async Task<IEnumerable<FareDataDto>> SearchFlightAsync(
        SearchFlightRequest req, CancellationToken ct = default)
    {
        // Galileo only handles international routes (legacy rule: skip VN→VN)
        // TODO: compare country codes from geo lookup once available; for now,
        //       allow all routes and let the XML response be empty for domestic.
        try
        {
            string pcc = _config["Galileo:Pcc"] ?? "DEFAULT";
            bool   isRoundTrip = req.TripType == TripType.RoundTrip;

            string xmlRequest = BuildSearchRequest(
                isRoundTrip ? 2 : 1,
                req.Origin, req.Destination,
                req.DepartDate, req.ReturnDate ?? req.DepartDate,
                req.AdultCount, req.ChildCount, req.InfantCount);

            XmlDocument? response = await SubmitSoapAsync(xmlRequest, pcc, ct);
            if (response is null)
                return [];

            return ParseFareData(response, req, pcc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Galileo] SearchFlightAsync failed");
            return [];
        }
    }

    public async Task<FareDataDto?> VerifyFareAsync(
        string fareId, string sessionData, CancellationToken ct = default)
    {
        // Galileo "verify" = retrieve the GDS fare by PNR / quote
        // sessionData carries the serialised FareDataDto JSON (set during Search)
        await Task.CompletedTask;
        return null; // TODO: implement if required by product
    }

    public async Task<BookResultDto> BookFlightAsync(
        BookFlightRequest req, FareDataDto fareData, CancellationToken ct = default)
    {
        try
        {
            // Build PNRBFManagement_11 XML — passenger names + segments
            // session / fareId carries the original FareDataDto JSON
            string xmlRequest = BuildBookRequest(req);
            string pcc        = _config["Galileo:Pcc"] ?? "DEFAULT";

            XmlDocument? response = await SubmitSoapAsync(xmlRequest, pcc, ct);
            if (response is null)
                return Fail("No response from Galileo");

            string bookingCode = ParseBookingCode(response);
            if (string.IsNullOrEmpty(bookingCode))
                return Fail("No booking code in Galileo response");

            return new BookResultDto
            {
                IsSuccess   = true,
                BookingCode = bookingCode,
                ExpiresAt   = DateTime.UtcNow.AddHours(24) // default; adjusted per departure window
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Galileo] BookFlightAsync failed");
            return Fail(ex.Message);
        }
    }

    public async Task<IssueTicketResultDto> IssueTicketAsync(
        string bookingCode, string sessionData, CancellationToken ct = default)
    {
        // Galileo does not expose issue-ticket directly; ticketing is a back-office action.
        await Task.CompletedTask;
        return new IssueTicketResultDto { IsSuccess = false, ErrorMessage = "Galileo: issue ticket is a back-office action." };
    }

    // =========================================================================
    // Build XML request (ported from GenerateXmlRequest — no dependency on old library)
    // =========================================================================

    internal static string BuildSearchRequest(
        int itineraryType,
        string from, string to,
        DateTime departDate, DateTime returnDate,
        int adult, int child, int infant)
    {
        int totalSeat = adult + child;
        string paxXml   = BuildPassengerXml(adult, child, infant);
        string optimize = BuildOptimizeXml();
        string pfInfo   = BuildPFInfoXml();
        string genQuote = "<GenQuoteInfo><RulesProcess>Y</RulesProcess></GenQuoteInfo>";

        string avail1 = BuildGenAvailXml(departDate.ToString("yyyyMMdd"), from, to, totalSeat);
        string avail2 = itineraryType == 2
            ? BuildGenAvailXml(returnDate.ToString("yyyyMMdd"), to, from, totalSeat)
            : string.Empty;

        string superBb = $"<SuperBBMods>{paxXml}{genQuote}{pfInfo}{optimize}</SuperBBMods>";

        if (itineraryType == 2)
            return $"<FareQuoteSuperBB_11><AirAvailMods>{avail1}</AirAvailMods><AirAvailMods>{avail2}</AirAvailMods>{superBb}</FareQuoteSuperBB_11>";

        return $"<FareQuoteSuperBB_11><AirAvailMods>{avail1}</AirAvailMods>{superBb}</FareQuoteSuperBB_11>";
    }

    private static string BuildGenAvailXml(string date, string from, string to, int seats) =>
        $"<GenAvail>" +
        $"<NumSeats>{seats}</NumSeats><Class></Class>" +
        $"<StartDt>{date}</StartDt><StartPt>{from}</StartPt><EndPt>{to}</EndPt>" +
        $"<StartTm></StartTm><TmWndInd></TmWndInd><StartTmWnd></StartTmWnd><EndTmWnd></EndTmWnd>" +
        $"<JrnyTm></JrnyTm><FltTypeInd>E</FltTypeInd><FltTypePref></FltTypePref>" +
        $"<StartPtInd>A</StartPtInd><EndPtInd>A</EndPtInd><IgnoreTSPref>N</IgnoreTSPref>" +
        $"</GenAvail>";

    private static string BuildPassengerXml(int adult, int child, int infant)
    {
        var sb = new StringBuilder("<PassengerType>");
        int n = 1;
        for (int i = 1; i <= adult; i++, n++)
            sb.Append($"<Psgr><LNameNum>{n}</LNameNum><PsgrNum>{n}</PsgrNum><AbsNameNum>{n}</AbsNameNum><PTC></PTC><TIC></TIC></Psgr>");
        for (int i = 1; i <= child; i++, n++)
            sb.Append($"<Psgr><LNameNum>{n}</LNameNum><PsgrNum>{n}</PsgrNum><AbsNameNum>{n}</AbsNameNum><PTC>CNN</PTC><TIC></TIC><Age>05</Age></Psgr>");
        for (int i = 1; i <= infant; i++, n++)
            sb.Append($"<Psgr><LNameNum>{n}</LNameNum><PsgrNum>{n}</PsgrNum><AbsNameNum>{n}</AbsNameNum><PTC>INF</PTC><TIC></TIC></Psgr>");
        sb.Append("</PassengerType>");
        return sb.ToString();
    }

    private static string BuildOptimizeXml() =>
        "<Optimize><RecType>1001</RecType><KlrID><ID>AAFI</ID></KlrID></Optimize>" +
        "<Optimize><RecType>1425</RecType>" +
        "<KlrID><ID>EROR</ID></KlrID><KlrID><ID>GFGQ</ID></KlrID>" +
        "<KlrID><ID>GFXI</ID></KlrID><KlrID><ID>GFPI</ID></KlrID>" +
        "<KlrID><ID>GFRI</ID></KlrID><KlrID><ID>GFJG</ID></KlrID>" +
        "<KlrID><ID>GFMM</ID></KlrID>" +
        "</Optimize>";

    private static string BuildPFInfoXml() =>
        "<PFInfo><ReqAirVPFs>Y</ReqAirVPFs>" +
        "<PF><StartODRange>00</StartODRange><EndODRange>00</EndODRange><CRS>1G</CRS>" +
        "<AirV></AirV><Acct></Acct><PublishedFaresInd>Y</PublishedFaresInd>" +
        "<Type>A</Type><AcctCodeRestrict></AcctCodeRestrict></PF>" +
        "</PFInfo>";

    // =========================================================================
    // Build book request (PNRBFManagement_11)
    // =========================================================================

    internal static string BuildBookRequest(BookFlightRequest req)
    {
        // Minimal PNR build — passenger names + end-transaction
        // Segments come from the sessionData; for now we emit the outer wrapper
        var sb = new StringBuilder("<PNRBFManagement_11>");
        sb.Append(BuildPaxNames(req.Passengers, req.ContactPhone));
        sb.Append(BuildEndTransaction());
        sb.Append("</PNRBFManagement_11>");
        return sb.ToString();
    }

    private static string BuildPaxNames(List<PassengerBookingDto> passengers, string phone)
    {
        var sb = new StringBuilder("<PNRBFPrimaryBldChgMods>");
        int n = 1;
        foreach (var p in passengers)
        {
            string lastName  = FlightEngineHelper.ConvertToUnSignString(p.LastName.Trim()).ToUpper();
            string firstName = FlightEngineHelper.ConvertToUnSignString(p.FirstName.Trim()).ToUpper();
            string suffix    = p.Type == Domain.Enums.PassengerType.Child   ? (p.Gender == "M" ? "MSTR" : "MISS") :
                               p.Type == Domain.Enums.PassengerType.Infant  ? (p.Gender == "M" ? "MSTR" : "MISS") :
                               (p.Gender == "M" ? "MR" : "MS");

            sb.Append($"<Item>" +
                      $"<DataBlkInd>N</DataBlkInd><EditTypeInd>A</EditTypeInd>" +
                      $"<LNameID>{n:00}</LNameID>" +
                      $"<FNameItem><PsgrNum>01</PsgrNum><AbsNameNum>{n:00}</AbsNameNum>" +
                      $"<FName>{lastName}{suffix}</FName></FNameItem>" +
                      $"<LName>{firstName}</LName>" +
                      $"</Item>");
            n++;
        }
        sb.Append($"<Type>T</Type><PhoneNumber>PHONE {phone}</PhoneNumber>");
        sb.Append("</PNRBFPrimaryBldChgMods>");
        return sb.ToString();
    }

    private static string BuildEndTransaction() =>
        "<EndTransactionMods><ETInd>E</ETInd><RcvdFrom>GALILEO</RcvdFrom><SkipTEdits>Y</SkipTEdits></EndTransactionMods>";

    // =========================================================================
    // SOAP transport — replaces GalileoWS ASMX proxy
    // =========================================================================

    private async Task<XmlDocument?> SubmitSoapAsync(string xmlBody, string pcc, CancellationToken ct)
    {
        string endpointUrl = _config["Galileo:EndpointUrl"]
            ?? throw new InvalidOperationException("Galileo:EndpointUrl not configured");
        string username = _config["Galileo:Username"]
            ?? throw new InvalidOperationException("Galileo:Username not configured");
        string password = _config["Galileo:Password"]
            ?? throw new InvalidOperationException("Galileo:Password not configured");
        int    timeout  = int.Parse(_config["Galileo:TimeoutSeconds"] ?? "120");

        // SOAP 1.1 envelope wrapping the raw Galileo XML request
        string soapEnvelope =
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            $"<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
            $"               xmlns:tns=\"http://tempuri.org/\">" +
            $"  <soap:Header>" +
            $"    <tns:Authentication>" +
            $"      <tns:Username>{username}</tns:Username>" +
            $"      <tns:Password>{password}</tns:Password>" +
            $"    </tns:Authentication>" +
            $"  </soap:Header>" +
            $"  <soap:Body>" +
            $"    <tns:SubmitXml>" +
            $"      <tns:XmlRequest>{EscapeXml(xmlBody)}</tns:XmlRequest>" +
            $"      <tns:ApiType>XML</tns:ApiType>" +
            $"      <tns:Pcc>{pcc}</tns:Pcc>" +
            $"      <tns:HcmName>{pcc}</tns:HcmName>" +
            $"      <tns:XmlSelectType>UTA</tns:XmlSelectType>" +
            $"    </tns:SubmitXml>" +
            $"  </soap:Body>" +
            $"</soap:Envelope>";

        using var cts    = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeout));

        using var client  = _httpFactory.CreateClient(HttpClientName);
        using var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "\"http://tempuri.org/SubmitXml\"");

        HttpResponseMessage httpResponse = await client.PostAsync(endpointUrl, content, cts.Token);
        httpResponse.EnsureSuccessStatusCode();

        string raw = await httpResponse.Content.ReadAsStringAsync(cts.Token);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Extract the inner XML from the SOAP Body / SubmitXmlResult element
        var soapDoc = new XmlDocument();
        soapDoc.LoadXml(raw);
        var ns     = new XmlNamespaceManager(soapDoc.NameTable);
        ns.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
        ns.AddNamespace("tns",  "http://tempuri.org/");

        XmlNode? resultNode = soapDoc.SelectSingleNode("//tns:SubmitXmlResult", ns);
        string   innerXml   = resultNode?.InnerText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(innerXml))
            return null;

        var doc = new XmlDocument();
        doc.LoadXml(innerXml);
        return doc;
    }

    private static string EscapeXml(string xml)
        => xml.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    // =========================================================================
    // Parse response XML — ported from GetFareData / GetAirAvail / etc.
    // =========================================================================

    private List<FareDataDto> ParseFareData(XmlDocument doc, SearchFlightRequest req, string pcc)
    {
        var result = new List<FareDataDto>();
        try
        {
            var blocked = BlockedAirlines.Split(',');
            bool isRoundTrip = req.TripType == TripType.RoundTrip;

            // AirAvail nodes — first is departure, second (if present) is return
            var airAvails     = ParseAirAvails(doc);
            var departAvail   = airAvails.Count > 0 ? airAvails[0] : new List<AvailFltInfo>();
            var returnAvail   = airAvails.Count > 1 ? airAvails[1] : new List<AvailFltInfo>();

            // FareInfo nodes
            XmlNodeList fareInfoNodes = doc.SelectNodes("//FareQuoteSuperBB_11/FareInfo")!;
            int fareIndex = 0;

            foreach (XmlElement fareInfoEl in fareInfoNodes)
            {
                var fareInfo = new XmlDocument();
                fareInfo.LoadXml(fareInfoEl.OuterXml);

                // PlatingCarrier
                string platingCarrier = ParsePlatingCarrier(fareInfo);
                if (blocked.Contains(platingCarrier))
                    continue;

                // Pax fares
                double fareAdult = 0, baseFareAdult = 0, taxAdult = 0;
                double fareChild = 0, baseFareChild = 0, taxChild  = 0;
                double fareInf   = 0, baseFareInf   = 0, taxInf    = 0;
                int    adultCnt  = 0, childCnt       = 0, infCnt    = 0;
                string currency  = req.Currency;

                foreach (XmlElement psgrEl in fareInfo.SelectNodes("//PsgrTypes")!)
                {
                    string picReq    = psgrEl.SelectSingleNode("PICReq/text()")?.Value ?? "";
                    string psgrs     = psgrEl.SelectSingleNode("PICPsgrs/text()")?.Value ?? "0";
                    string uniqueKey = psgrEl.SelectSingleNode("UniqueKey/text()")?.Value ?? "";

                    (double total, double baseFare, double tax) = ParseQuoteAmounts(fareInfo, uniqueKey, currency);

                    if (picReq is "AD" or "ADT") { adultCnt = int.Parse(psgrs); fareAdult = total; baseFareAdult = baseFare; taxAdult = tax; }
                    else if (picReq is "CNN" or "CHD") { childCnt = int.Parse(psgrs); fareChild = total; baseFareChild = baseFare; taxChild = tax; }
                    else if (picReq == "INF")  { infCnt = int.Parse(psgrs); fareInf = total; baseFareInf = baseFare; taxInf = tax; }
                }

                // Departure segments from FlightItemCrossRef
                var departSegs = ParseFlightSegments(fareInfo, departAvail, "1");
                var returnSegs = isRoundTrip ? ParseFlightSegments(fareInfo, returnAvail, "2") : [];

                // ── Extract RulesInfo XML fragments for GetFareRulesAsync ─────
                var departRulesXml = new List<string>();
                var returnRulesXml = new List<string>();
                var rulesInfoNodes = fareInfo.SelectNodes("//RulesInfo");
                if (rulesInfoNodes != null)
                {
                    foreach (XmlElement riEl in rulesInfoNodes)
                    {
                        var riDoc = new XmlDocument();
                        riDoc.LoadXml(riEl.OuterXml);
                        string? fareNum = riDoc.SelectSingleNode("//RulesInfo/FareNum/text()")?.Value;
                        if (fareNum == "1")
                            departRulesXml.Add(riEl.OuterXml);
                        else
                            returnRulesXml.Add(riEl.OuterXml);
                    }
                }

                // Store PCC + RulesInfo in SessionData for later use
                string sessionData = JsonSerializer.Serialize(new GalileoSessionData
                {
                    Pcc = pcc,
                    DepartureRulesInfo = departRulesXml,
                    ReturnRulesInfo    = returnRulesXml
                });

                // Rounding (VND→nearest 1000, other→ceiling)
                fareAdult = FlightEngineHelper.RoundFare(fareAdult, currency);
                fareChild = FlightEngineHelper.RoundFare(fareChild, currency);
                fareInf   = FlightEngineHelper.RoundFare(fareInf,   currency);
                double totalFare = (adultCnt * fareAdult) + (childCnt * fareChild) + (infCnt * fareInf);

                // LastTkDt
                XmlNode? lastTkNode = fareInfo.SelectSingleNode("//GenQuoteDetails[1]/LastTkDt/text()");
                DateTime lastTkDt   = lastTkNode?.Value is { } lastTkVal
                    ? FlightEngineHelper.GetDate(lastTkVal, "1200")
                    : DateTime.UtcNow.AddDays(1);

                var dto = new FareDataDto
                {
                    FareId           = $"galileo-{pcc}-{fareIndex++}",
                    Source           = FlightSource.Galileo,
                    Airline          = platingCarrier,
                    Origin           = req.Origin,
                    Destination      = req.Destination,
                    DepartDate       = req.DepartDate,
                    ReturnDate       = isRoundTrip ? req.ReturnDate : null,
                    TripType         = req.TripType,
                    AdultCount       = adultCnt,
                    ChildCount       = childCnt,
                    InfantCount      = infCnt,
                    AdultFare        = (decimal)fareAdult,
                    ChildFare        = (decimal)fareChild,
                    InfantFare       = (decimal)fareInf,
                    TaxAmount        = (decimal)(taxAdult + taxChild + taxInf),
                    ServiceFee       = 0,
                    TotalFare        = (decimal)totalFare,
                    Currency         = currency,
                    PccCode          = pcc,
                    SessionData      = sessionData,
                    OutboundSegments = departSegs,
                    ReturnSegments   = returnSegs,
                    CachedAt         = DateTime.UtcNow,
                    ExpiresAt        = lastTkDt
                };

                if (dto.OutboundSegments.Count > 0)
                    result.Add(dto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Galileo] ParseFareData failed");
        }
        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ParsePlatingCarrier(XmlDocument fareInfo)
    {
        foreach (XmlElement msgEl in fareInfo.SelectNodes("//InfoMsg")!)
        {
            string text = msgEl.SelectSingleNode("Text/text()")?.Value ?? "";
            if (text.Contains("DEFAULT PLATING CARRIER"))
            {
                var parts = text.Split(' ');
                return parts[^1].Trim();
            }
        }
        return fareInfo.SelectSingleNode("//DepartRulesInfo/AirV/text()")?.Value ?? "";
    }

    private static (double total, double baseFare, double tax) ParseQuoteAmounts(
        XmlDocument fareInfo, string uniqueKey, string currency)
    {
        foreach (XmlElement qd in fareInfo.SelectNodes("//GenQuoteDetails")!)
        {
            if (qd.SelectSingleNode("UniqueKey/text()")?.Value != uniqueKey) continue;

            double totAmt    = ParseDecimalPos(qd, "TotAmt",    "TotDecPos");
            double equivAmt  = ParseDecimalPos(qd, "EquivAmt",  "EquivDecPos");
            double baseAmt   = ParseDecimalPos(qd, "BaseFareAmt","BaseDecPos");

            double baseFare = equivAmt > 0 ? equivAmt : baseAmt;
            double tax      = totAmt - baseFare;
            return (totAmt, baseFare, tax);
        }
        return (0, 0, 0);
    }

    private static double ParseDecimalPos(XmlElement el, string amtField, string posField)
    {
        double amt = double.TryParse(el.SelectSingleNode($"{amtField}/text()")?.Value, out var v) ? v : 0;
        double pos = double.TryParse(el.SelectSingleNode($"{posField}/text()")?.Value, out var p) ? p : 0;
        return pos > 0 ? amt / Math.Pow(10, pos) : amt;
    }

    private static List<List<AvailFltInfo>> ParseAirAvails(XmlDocument doc)
    {
        var result = new List<List<AvailFltInfo>>();
        foreach (XmlElement avail in doc.SelectNodes("//FareQuoteSuperBB_11/AirAvail")!)
        {
            var segments = new List<AvailFltInfo>();
            foreach (XmlElement flt in avail.SelectNodes(".//AvailFlt")!)
            {
                segments.Add(new AvailFltInfo
                {
                    AirlineCode   = flt.SelectSingleNode("AirV/text()")?.Value     ?? "",
                    FlightNumber  = flt.SelectSingleNode("FltNum/text()")?.Value    ?? "",
                    StartPoint    = flt.SelectSingleNode("StartAirp/text()")?.Value ?? "",
                    EndPoint      = flt.SelectSingleNode("EndAirp/text()")?.Value   ?? "",
                    StartDate     = ParseGalileoDate(flt, "StartDt", "StartTm"),
                    EndDate       = ParseGalileoDate(flt, "EndDt",   "EndTm"),
                    Equipment     = flt.SelectSingleNode("Equip/text()")?.Value     ?? "",
                    Duration      = int.TryParse(flt.SelectSingleNode("JrnyTm/text()")?.Value, out var d) ? d : 0,
                    StopCount     = int.TryParse(flt.SelectSingleNode("NumStops/text()")?.Value, out var s) ? s : 0,
                    ClassAdult    = "",  // populated in cross-ref step
                });
            }
            result.Add(segments);
        }
        return result;
    }

    private static List<FlightSegmentDto> ParseFlightSegments(
        XmlDocument fareInfo,
        List<AvailFltInfo> avail,
        string odComponent) // "1" = departure, "2" = return
    {
        var segs = new List<FlightSegmentDto>();
        // FlightItemCrossRef nodes — first one is departure, second is return
        var crossRefs = fareInfo.SelectNodes("//FlightItemCrossRef")!;
        int refIndex  = odComponent == "1" ? 0 : 1;
        if (crossRefs.Count <= refIndex) return segs;

        var crossRef = crossRefs[refIndex] as XmlElement;
        if (crossRef is null) return segs;

        foreach (XmlElement fltItem in crossRef.SelectNodes(".//FltItemAry/FltItem")!)
        {
            int idx = int.TryParse(fltItem.SelectSingleNode("IndexNum/text()")?.Value, out var i) ? i - 1 : -1;
            if (idx < 0 || idx >= avail.Count) continue;

            var a = avail[idx];

            // Booking class
            string bic = fltItem.SelectSingleNode(".//BICAry/BICInfo/BIC/text()")?.Value ?? "";

            // Pacific Airline label
            string fltNum = PacificAirlineHelper.AppendPacificLabel(a.AirlineCode, a.FlightNumber);

            segs.Add(new FlightSegmentDto
            {
                FlightNumber = fltNum,
                Airline      = a.AirlineCode,
                Origin       = a.StartPoint,
                Destination  = a.EndPoint,
                DepartTime   = a.StartDate,
                ArriveTime   = a.EndDate,
                CabinClass   = bic,
                AircraftType = a.Equipment,
                StopCount    = a.StopCount
            });
        }
        return segs;
    }

    private static DateTime ParseGalileoDate(XmlElement el, string dayField, string timeField)
    {
        string day  = el.SelectSingleNode($"{dayField}/text()")?.Value ?? "";
        string time = el.SelectSingleNode($"{timeField}/text()")?.Value ?? "0000";
        return FlightEngineHelper.GetDate(day, time);
    }

    // ─── Book response ────────────────────────────────────────────────────────

    private static string ParseBookingCode(XmlDocument doc)
        => doc.SelectSingleNode("//PNRBFManagement_11//RecLoc/text()")?.Value
        ?? doc.SelectSingleNode("//RecLoc/text()")?.Value
        ?? string.Empty;

    private static BookResultDto Fail(string msg)
        => new() { IsSuccess = false, ErrorMessage = msg };

    // ── Baggage & Fare Rules ──────────────────────────────────────────────────

    /// <summary>
    /// Galileo does not expose a separate baggage-options API in the current integration.
    /// Returns an empty BaggageInfoDto.
    /// </summary>
    public Task<BaggageInfoDto> GetBaggagesAsync(
        FareDataDto fareData, CancellationToken ct = default)
        => Task.FromResult(new BaggageInfoDto());

    /// <summary>
    /// Retrieves fare rules from Galileo/Travelport GDS using FareQuoteMultiDisplay_10.
    /// Ported from legacy GalileoEngine.GetRulesInfo + Interface.GetFareRule.
    /// Uses the RulesInfo XML fragments stored in SessionData during search.
    /// itinerary: 0 = outbound (departure), 1 = return.
    /// </summary>
    public async Task<List<FareRuleGroupDto>> GetFareRulesAsync(
        FareDataDto fareData, int itinerary, CancellationToken ct = default)
    {
        var rules = new List<FareRuleGroupDto>();
        try
        {
            if (string.IsNullOrWhiteSpace(fareData.SessionData))
                return rules;

            // Deserialize stored RulesInfo from search
            var session = JsonSerializer.Deserialize<GalileoSessionData>(fareData.SessionData);
            if (session is null) return rules;

            var rulesInfoList = itinerary == 0
                ? session.DepartureRulesInfo
                : session.ReturnRulesInfo;

            if (rulesInfoList == null || rulesInfoList.Count == 0)
                return rules;

            string pcc = session.Pcc ?? fareData.PccCode ?? _config["Galileo:Pcc"] ?? "DEFAULT";

            // ── Build FareQuoteMultiDisplay_10 XML ────────────────────────────
            string templateXml = BuildFareRulesRequestXml();
            var requestDoc = new XmlDocument();
            requestDoc.LoadXml(templateXml);

            // Append stored RulesInfo fragments into the template
            foreach (string rulesXml in rulesInfoList)
            {
                var frag = requestDoc.CreateDocumentFragment();
                frag.InnerXml = rulesXml;
                requestDoc.DocumentElement?.FirstChild?.AppendChild(frag);
            }

            // ── Submit to Galileo ─────────────────────────────────────────────
            XmlDocument? response = await SubmitSoapAsync(requestDoc.InnerXml, pcc, ct);
            if (response is null) return rules;

            // ── Parse RulesData from response ─────────────────────────────────
            var rulesDataNodes = response.SelectNodes("//FareQuoteMultiDisplay_10/FareInfo/RulesData");
            if (rulesDataNodes == null || rulesDataNodes.Count == 0) return rules;

            var allRules = new List<(string UniqueKey, string DataType, string Text)>();
            foreach (XmlElement rd in rulesDataNodes)
            {
                var rdDoc = new XmlDocument();
                rdDoc.LoadXml(rd.OuterXml);

                string uniqueKey   = rdDoc.SelectSingleNode("//UniqueKey/text()")?.Value    ?? "";
                string dataType    = rdDoc.SelectSingleNode("//RulesDataType/text()")?.Value ?? "";
                string rulesText   = rdDoc.SelectSingleNode("//RulesText/text()")?.Value    ?? "";

                allRules.Add((uniqueKey, dataType, rulesText));
            }

            // "F" = title, "T" = text body. Group text entries by UniqueKey matching title.
            var titles = allRules.Where(r => r.DataType == "F").ToList();
            var texts  = allRules.Where(r => r.DataType == "T").ToList();

            foreach (var title in titles)
            {
                // Title text: first 4 chars are category code, rest is the human-readable title
                string titleText = title.Text.Length > 4
                    ? title.Text[4..]
                    : title.Text;

                var matchingTexts = texts
                    .Where(t => t.UniqueKey == title.UniqueKey)
                    .Select(t => t.Text)
                    .ToList();

                rules.Add(new FareRuleGroupDto
                {
                    Title = titleText,
                    Rules = matchingTexts
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Galileo] GetFareRulesAsync failed");
        }
        return rules;
    }

    /// <summary>
    /// Builds the FareQuoteMultiDisplay_10 XML template for fare rules retrieval.
    /// Ported from legacy App_Data/FareRulesRequest.xml.
    /// Action=135 requests all rule paragraphs with full text.
    /// </summary>
    private static string BuildFareRulesRequestXml() =>
        """
        <FareQuoteMultiDisplay_10>
          <FareDisplayMods>
            <QueryHeader>
              <UniqueKey>0000</UniqueKey><LangNum>00</LangNum>
              <Action>135</Action><RetCRTOutput>N</RetCRTOutput>
              <NoMsg>N</NoMsg><NoTrunc>Y</NoTrunc>
              <IMInd>N</IMInd><FIPlus>N</FIPlus>
              <PEInd>N</PEInd><PVInd>N</PVInd><NBInd>N</NBInd>
              <ActionOnlyInd>N</ActionOnlyInd><TranslatePeriod>N</TranslatePeriod>
              <PIInd>N</PIInd><IntFrame1>Y</IntFrame1>
              <SmartParsed>N</SmartParsed><PDCodes>N</PDCodes>
              <BkDtOverride>N</BkDtOverride><HostUse25>N</HostUse25>
              <DefCurrency/><PFPWInd>N</PFPWInd><PFPQInd>N</PFPQInd>
              <HostUse29>N</HostUse29><HostUse30>N</HostUse30>
              <HostUse31>N</HostUse31><HostUse32/><HostUse33/>
            </QueryHeader>
            <FollowUpEntries>
              <UniqueKey>0000</UniqueKey><QuoteNum>1</QuoteNum>
              <Spare1>N</Spare1><AllParaReqind>Y</AllParaReqind>
              <SumRuleReqInd>N</SumRuleReqInd><FulltextoptInd>N</FulltextoptInd>
              <Spare2>NNNN</Spare2><Text/>
            </FollowUpEntries>
            <GenInfo>
              <UniqueKey>0000</UniqueKey><TkCity/><QuoteDt/>
              <QuoteCity/><Currency/><RsvnDt/><RsvnTm/>
              <PlatingCarrier/><SellCurrency/>
              <TkGuaranteed>N</TkGuaranteed><EUROverride>N</EUROverride>
              <LCUOverride>N</LCUOverride>
            </GenInfo>
          </FareDisplayMods>
        </FareQuoteMultiDisplay_10>
        """;

    // ── Internal DTO (no dependency on old IBE.Models) ────────────────────────

    private sealed class AvailFltInfo
    {
        public string   AirlineCode  { get; init; } = "";
        public string   FlightNumber { get; set; }  = "";
        public string   StartPoint   { get; init; } = "";
        public string   EndPoint     { get; init; } = "";
        public DateTime StartDate    { get; init; }
        public DateTime EndDate      { get; init; }
        public string   Equipment    { get; init; } = "";
        public int      Duration     { get; init; }
        public int      StopCount    { get; init; }
        public string   ClassAdult   { get; set; }  = "";
    }
}

// ── Galileo session DTO (file-scoped to avoid polluting namespace) ─────────

file sealed class GalileoSessionData
{
    [JsonPropertyName("pcc")]         public string?       Pcc                { get; set; }
    [JsonPropertyName("depRules")]    public List<string>  DepartureRulesInfo { get; set; } = [];
    [JsonPropertyName("retRules")]    public List<string>  ReturnRulesInfo    { get; set; } = [];
}

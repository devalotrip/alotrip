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
    private static readonly string[] BlockedAirlines = ["GP", "A1"];

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
        try
        {
            string pcc = req.PccCode ?? _config["Galileo:Pcc"] ?? "DEFAULT";
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
        await Task.CompletedTask;
        return null;
    }

    public async Task<BookResultDto> BookFlightAsync(
        BookFlightRequest req, FareDataDto fareData, CancellationToken ct = default)
    {
        try
        {
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
                ExpiresAt   = DateTime.UtcNow.AddHours(24)
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
        await Task.CompletedTask;
        return new IssueTicketResultDto { IsSuccess = false, ErrorMessage = "Galileo: issue ticket is a back-office action." };
    }

    // =========================================================================
    // Build XML request
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
        $"<NumSeats>{seats}</NumSeats>" +
        $"<StartDt>{date}</StartDt><StartPt>{from}</StartPt><EndPt>{to}</EndPt>" +
        $"<FltTypeInd>E</FltTypeInd><StartPtInd>A</StartPtInd><EndPtInd>A</EndPtInd><IgnoreTSPref>N</IgnoreTSPref>" +
        $"</GenAvail>";

    private static string BuildPassengerXml(int adult, int child, int infant)
    {
        var sb = new StringBuilder("<PassengerType><PsgrAry>");
        int n = 1;
        for (int i = 1; i <= adult; i++, n++)
            sb.Append($"<Psgr><LNameNum>{n}</LNameNum><PsgrNum>{n}</PsgrNum><AbsNameNum>{n}</AbsNameNum><PTC></PTC><Age></Age><PricePTCOnly></PricePTCOnly><DiscOrIncrInd></DiscOrIncrInd><AmtOrPercent></AmtOrPercent><PersonalGeoType></PersonalGeoType><PersonalGeoData></PersonalGeoData><TIC></TIC><TkDesignator></TkDesignator><TkCode></TkCode></Psgr>");
        for (int i = 1; i <= child; i++, n++)
            sb.Append($"<Psgr><LNameNum>{n}</LNameNum><PsgrNum>{n}</PsgrNum><AbsNameNum>{n}</AbsNameNum><PTC>CNN</PTC><TIC></TIC><Age>05</Age><PricePTCOnly></PricePTCOnly><DiscOrIncrInd></DiscOrIncrInd><AmtOrPercent></AmtOrPercent><PersonalGeoType></PersonalGeoType><PersonalGeoData></PersonalGeoData><TkDesignator></TkDesignator><TkCode></TkCode></Psgr>");
        for (int i = 1; i <= infant; i++, n++)
            sb.Append($"<Psgr><LNameNum>{n}</LNameNum><PsgrNum>{n}</PsgrNum><AbsNameNum>{n}</AbsNameNum><PTC>INF</PTC><TIC></TIC><Age></Age><PricePTCOnly></PricePTCOnly><DiscOrIncrInd></DiscOrIncrInd><AmtOrPercent></AmtOrPercent><PersonalGeoType></PersonalGeoType><PersonalGeoData></PersonalGeoData><TkDesignator></TkDesignator><TkCode></TkCode></Psgr>");
        sb.Append("</PsgrAry></PassengerType>");
        return sb.ToString();
    }

    private static string BuildOptimizeXml() =>
        "<Optimize><RecType>1001</RecType><KlrIDAry><KlrID>AAFI</KlrID></KlrIDAry></Optimize>" +
        "<Optimize><RecType>1425</RecType><KlrIDAry>" +
        "<KlrID>EROR</KlrID><KlrID>GFGQ</KlrID>" +
        "<KlrID>GFXI</KlrID><KlrID>GFPI</KlrID>" +
        "<KlrID>GFRI</KlrID><KlrID>GFJG</KlrID>" +
        "<KlrID>GFMM</KlrID>" +
        "</KlrIDAry></Optimize>";

    private static string BuildPFInfoXml() =>
        "<PFInfo><ReqAirVPFs>Y</ReqAirVPFs>" +
        "<PFAry><PF><StartODRange>00</StartODRange><EndODRange>00</EndODRange><CRS>1G</CRS>" +
        "<PCC></PCC><AirV></AirV><Acct></Acct><Contract></Contract><PublishedFaresInd>Y</PublishedFaresInd>" +
        "<Type>A</Type><PFTypeRestrict></PFTypeRestrict><AcctCodeRestrict></AcctCodeRestrict><Spare1></Spare1></PF></PFAry>" +
        "</PFInfo>";

    // =========================================================================
    // Build book request
    // =========================================================================

    internal static string BuildBookRequest(BookFlightRequest req)
    {
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
    // SOAP transport
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
    // Parse response XML — matches old GetFareData logic exactly
    // =========================================================================

    private List<FareDataDto> ParseFareData(XmlDocument doc, SearchFlightRequest req, string pcc)
    {
        var result = new List<FareDataDto>();
        try
        {
            bool isRoundTrip = req.TripType == TripType.RoundTrip;

            // 1. Parse AirAvail lists (departure + optional return)
            var airAvailLists = ParseAirAvailLists(doc);
            var departAirAvail = airAvailLists.Count > 0 ? airAvailLists[0] : new List<AvailFltInfo>();
            var returnAirAvail = airAvailLists.Count > 1 ? airAvailLists[1] : new List<AvailFltInfo>();

            // 2. Parse FareInfo nodes
            XmlNodeList fareInfoNodes = doc.SelectNodes("//FareQuoteSuperBB_11/FareInfo")!;
            int fareIndex = 0;

            foreach (XmlElement fareInfoEl in fareInfoNodes)
            {
                var fareInfo = new XmlDocument();
                fareInfo.LoadXml(fareInfoEl.OuterXml);

                // ─ PlatingCarrier ────────────────────────────────────────────
                string platingCarrier = ParsePlatingCarrier(fareInfo);
                if (BlockedAirlines.Contains(platingCarrier))
                    continue;

                // ── Pax fares ─────────────────────────────────────────────────
                double fareAdult = 0, baseFareAdult = 0, taxAdult = 0;
                double fareChild = 0, baseFareChild = 0, taxChild  = 0;
                double fareInf   = 0, baseFareInf   = 0, taxInf    = 0;
                int    adultCnt  = 0, childCnt       = 0, infCnt    = 0;
                string currency  = req.Currency;

                // Collect PsgrTypes for BIC mapping later
                var psgrTypesList = new List<(string UniqueKey, string PICReq, int PICPsgrs)>();

                foreach (XmlElement psgrEl in fareInfo.SelectNodes("//FareInfo/PsgrTypes")!)
                {
                    string picReq    = psgrEl.SelectSingleNode("PICReq/text()")?.Value ?? "";
                    string psgrs     = psgrEl.SelectSingleNode("PICPsgrs/text()")?.Value ?? "0";
                    string uniqueKey = psgrEl.SelectSingleNode("UniqueKey/text()")?.Value ?? "";

                    psgrTypesList.Add((uniqueKey, picReq, int.TryParse(psgrs, out var c) ? c : 0));

                    (double total, double baseFare, double tax) = ParseQuoteAmounts(fareInfo, uniqueKey, currency);

                    if (picReq is "AD" or "ADT") { adultCnt = int.Parse(psgrs); fareAdult = total; baseFareAdult = baseFare; taxAdult = tax; }
                    else if (picReq is "CNN" or "CHD") { childCnt = int.Parse(psgrs); fareChild = total; baseFareChild = baseFare; taxChild = tax; }
                    else if (picReq == "INF")  { infCnt = int.Parse(psgrs); fareInf = total; baseFareInf = baseFare; taxInf = tax; }
                }

                // ── LastTkDt ──────────────────────────────────────────────────
                DateTime lastTkDt = DateTime.UtcNow.AddDays(1);
                foreach (XmlElement gqd in fareInfo.SelectNodes("//FareInfo/GenQuoteDetails")!)
                {
                    string? lastTkVal = gqd.SelectSingleNode("LastTkDt/text()")?.Value;
                    if (!string.IsNullOrEmpty(lastTkVal))
                    {
                        var dt = FlightEngineHelper.GetDate(lastTkVal, "1200");
                        if (dt > lastTkDt) lastTkDt = dt;
                    }
                }

                // ─ RulesInfo XML fragments ───────────────────────────────────
                var departRulesXml = new List<string>();
                var returnRulesXml = new List<string>();
                var rulesInfoNodes = fareInfo.SelectNodes("//FareInfo/RulesInfo");
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

                // ─ PenaltyRules (for NoRefund check) ─────────────────────────
                var penaltyRulesDepart = new List<PenaltyRuleInfo>();
                var penaltyRulesReturn = new List<PenaltyRuleInfo>();
                var penaltyNodes = fareInfo.SelectNodes("//FareInfo/PenaltyRules");
                if (penaltyNodes != null)
                {
                    foreach (XmlElement prEl in penaltyNodes)
                    {
                        var fareCompNum = prEl.SelectSingleNode("FareComponentNum/text()")?.Value ?? "";
                        var depItems = new List<DepRequiredItem>();
                        foreach (XmlElement depEl in prEl.SelectNodes(".//DepRequiredAry/DepRequiredAryItem")!)
                        {
                            depItems.Add(new DepRequiredItem
                            {
                                TkNonRef = depEl.SelectSingleNode("TkNonRef/text()")?.Value ?? ""
                            });
                        }
                        var info = new PenaltyRuleInfo { FareComponentNum = fareCompNum, DepRequiredItems = depItems };
                        if (fareCompNum == "1") penaltyRulesDepart.Add(info);
                        else if (fareCompNum == "2") penaltyRulesReturn.Add(info);
                    }
                }

                // ── Build departure flight options ────────────────────────────
                var departOptions = BuildFlightOptions(fareInfo, departAirAvail, psgrTypesList, penaltyRulesDepart, "1");

                // ── Build return flight options (if round-trip) ───────────────
                var returnOptions = new List<FlightOptionDto>();
                int itineraryType = 1;
                if (isRoundTrip && airAvailLists.Count > 1)
                {
                    returnOptions = BuildFlightOptions(fareInfo, returnAirAvail, psgrTypesList, penaltyRulesReturn, "2");
                    itineraryType = 2;
                }

                // ── Validate: must have at least one valid option ─────────────
                if (departOptions.Count == 0) continue;
                if (itineraryType == 2 && returnOptions.Count == 0) continue;

                // ── Rounding (VND→nearest 1000, other→ceiling) ──────────────
                fareAdult = FlightEngineHelper.RoundFare(fareAdult, currency);
                fareChild = FlightEngineHelper.RoundFare(fareChild, currency);
                fareInf   = FlightEngineHelper.RoundFare(fareInf,   currency);
                double totalFare = (adultCnt * fareAdult) + (childCnt * fareChild) + (infCnt * fareInf);

                // ─ SessionData ───────────────────────────────────────────────
                string sessionData = JsonSerializer.Serialize(new GalileoSessionData
                {
                    Pcc = pcc,
                    DepartureRulesInfo = departRulesXml,
                    ReturnRulesInfo    = returnRulesXml
                });

                var dto = new FareDataDto
                {
                    FareId             = $"galileo-{pcc}-{fareIndex++}",
                    Source             = FlightSource.Galileo,
                    Airline            = platingCarrier,
                    Origin             = req.Origin,
                    Destination        = req.Destination,
                    DepartDate         = req.DepartDate,
                    ReturnDate         = isRoundTrip ? req.ReturnDate : null,
                    TripType           = isRoundTrip ? TripType.RoundTrip : TripType.OneWay,
                    AdultCount         = adultCnt,
                    ChildCount         = childCnt,
                    InfantCount        = infCnt,
                    AdultFare          = (decimal)fareAdult,
                    ChildFare          = (decimal)fareChild,
                    InfantFare         = (decimal)fareInf,
                    BaseFareAdult      = (decimal)baseFareAdult,
                    BaseFareChild      = (decimal)baseFareChild,
                    BaseFareInfant     = (decimal)baseFareInf,
                    TaxAdult           = (decimal)taxAdult,
                    TaxChild           = (decimal)taxChild,
                    TaxInfant          = (decimal)taxInf,
                    ServiceFee         = 0,
                    TotalFare          = (decimal)totalFare,
                    Currency           = currency,
                    PccCode            = pcc,
                    SessionData        = sessionData,
                    OutboundOptions    = departOptions,
                    ReturnOptions      = returnOptions,
                    DepartureRulesInfo = departRulesXml,
                    ReturnRulesInfo    = returnRulesXml,
                    CachedAt           = DateTime.UtcNow,
                    ExpiresAt          = lastTkDt
                };

                // Populate backward-compatible flat segment lists
                dto.FlattenSegments();

                result.Add(dto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Galileo] ParseFareData failed");
        }
        return result;
    }

    // ── Flight option builder (matches old Chunk + Flight construction) ───────

    /// <summary>
    /// Builds flight options from FlightItemCrossRef + AirAvail data.
    /// Matches old code: GetAvailFlt → Chunk by ODNumLegs → build Flight objects.
    /// </summary>
    private List<FlightOptionDto> BuildFlightOptions(
        XmlDocument fareInfo,
        List<AvailFltInfo> airAvail,
        List<(string UniqueKey, string PICReq, int PICPsgrs)> psgrTypes,
        List<PenaltyRuleInfo> penaltyRules,
        string odComponent) // "1" = departure, "2" = return
    {
        var options = new List<FlightOptionDto>();

        var crossRefs = fareInfo.SelectNodes("//FareInfo/FlightItemCrossRef")!;
        int refIndex = odComponent == "1" ? 0 : 1;
        if (crossRefs.Count <= refIndex) return options;

        var crossRef = crossRefs[refIndex] as XmlElement;
        if (crossRef is null) return options;

        // ODNumLegs = số segment tạo thành 1 option
        int segmentNum = 1;
        string? odNumLegs = crossRef.SelectSingleNode("ODNumLegs/text()")?.Value;
        if (!string.IsNullOrEmpty(odNumLegs))
            segmentNum = int.Parse(odNumLegs);

        // Build flat list of AvailFlt from cross-ref
        var flatAvailFlts = new List<AvailFltInfo>();
        foreach (XmlElement fltItem in crossRef.SelectNodes(".//FltItemAry/FltItem")!)
        {
            int idx = int.TryParse(fltItem.SelectSingleNode("IndexNum/text()")?.Value, out var i) ? i - 1 : -1;
            if (idx < 0 || idx >= airAvail.Count) continue;

            var a = airAvail[idx].ShallowCopy();

            // Pacific Airline label
            a.FlightNumber = PacificAirlineHelper.AppendPacificLabel(a.AirlineCode, a.FlightNumber);

            // BIC (booking class) mapping per passenger type
            ApplyBicMapping(fltItem, a, psgrTypes);

            flatAvailFlts.Add(a);
        }

        // Chunk into options (matches old Common.Chunk)
        var chunks = Chunk(flatAvailFlts, segmentNum);
        int optionId = 0;

        foreach (var chunk in chunks)
        {
            if (chunk.Count == 0) continue;

            // Check blocked airlines in any segment
            bool isBlocked = chunk.Any(s => BlockedAirlines.Contains(s.AirlineCode));
            if (isBlocked) continue;

            // Check NoRefund from PenaltyRules
            bool noRefund = false;
            foreach (var pr in penaltyRules)
            {
                foreach (var dep in pr.DepRequiredItems)
                {
                    if (dep.TkNonRef == "Y") { noRefund = true; break; }
                }
                if (noRefund) break;
            }

            // Calculate StopTime between consecutive segments
            for (int i = 0; i < chunk.Count - 1; i++)
            {
                chunk[i].StopTime = (int)(chunk[i + 1].StartDate - chunk[i].EndDate).TotalMinutes;
            }

            // Mark last segment
            if (chunk.Count > 0)
                chunk[^1].IsLastSegment = true;

            var option = new FlightOptionDto
            {
                OptionId    = optionId++,
                Airline     = chunk[0].AirlineCode,
                Origin      = chunk[0].StartPoint,
                Destination = chunk[^1].EndPoint,
                DepartDate  = chunk[0].StartDate,
                ArriveDate  = chunk[^1].EndDate,
                Duration    = chunk[0].Duration,
                StopCount   = chunk.Count - 1,
                NoRefund    = noRefund,
                Segments    = chunk.Select(s => new FlightSegmentDto
                {
                    FlightNumber     = s.FlightNumber,
                    Airline          = s.AirlineCode,
                    Origin           = s.StartPoint,
                    Destination      = s.EndPoint,
                    DepartTime       = s.StartDate,
                    ArriveTime       = s.EndDate,
                    ClassAdult       = s.ClassAdult,
                    ClassChild       = s.ClassChild,
                    ClassInfant      = s.ClassInfant,
                    AircraftType     = s.Equipment,
                    StopCount        = s.StopCount,
                    Duration         = s.Duration,
                    StopTime         = s.StopTime,
                    AirportChange    = s.AirportChange,
                    OperatingAirline = s.OperatingAirline,
                    StartTerminal    = s.StartTerminal,
                    EndTerminal      = s.EndTerminal,
                    IsLastSegment    = s.IsLastSegment,
                }).ToList()
            };

            options.Add(option);
        }

        return options;
    }

    /// <summary>
    /// Maps booking class (BIC) from FlightItemCrossRef to AvailFlt per passenger type.
    /// Matches old code: fltItem.BICAry → availFlt.ClassAdult/ClassChild/ClassInfant.
    /// </summary>
    private static void ApplyBicMapping(
        XmlElement fltItem,
        AvailFltInfo avail,
        List<(string UniqueKey, string PICReq, int PICPsgrs)> psgrTypes)
    {
        var bicInfos = fltItem.SelectNodes(".//BICAry/BICInfo");
        if (bicInfos is null || bicInfos.Count == 0) return;

        if (bicInfos.Count == 1)
        {
            // Single BIC → apply to all passenger types
            string bic = bicInfos[0].SelectSingleNode("BIC/text()")?.Value ?? "";
            avail.ClassAdult  = bic;
            avail.ClassChild  = bic;
            avail.ClassInfant = bic;
        }
        else
        {
            // Multiple BIC → map by PsgrDescNumAry
            foreach (XmlElement bicInfoEl in bicInfos)
            {
                string bic = bicInfoEl.SelectSingleNode("BIC/text()")?.Value ?? "";
                var numNodes = bicInfoEl.SelectNodes(".//PsgrDescNumAry/Num");

                if (numNodes is null || numNodes.Count == 0)
                {
                    // No passenger mapping → apply to all
                    avail.ClassAdult  = bic;
                    avail.ClassChild  = bic;
                    avail.ClassInfant = bic;
                }
                else
                {
                    foreach (XmlElement numEl in numNodes)
                    {
                        int psgrIndex = int.Parse(numEl.InnerText) - 1;
                        if (psgrIndex < 0 || psgrIndex >= psgrTypes.Count) continue;

                        var ptc = psgrTypes[psgrIndex].PICReq;
                        if (ptc is "AD" or "ADT")      avail.ClassAdult  = bic;
                        else if (ptc is "CNN" or "CHD") avail.ClassChild  = bic;
                        else if (ptc == "INF")           avail.ClassInfant = bic;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Splits a list into chunks of specified size (matches old Common.Chunk).
    /// </summary>
    private static List<List<T>> Chunk<T>(List<T> source, int chunkSize)
    {
        var result = new List<List<T>>();
        for (int i = 0; i < source.Count; i += chunkSize)
        {
            result.Add(source.GetRange(i, Math.Min(chunkSize, source.Count - i)));
        }
        return result;
    }

    // ── AirAvail parser ───────────────────────────────────────────────────────

    private static List<List<AvailFltInfo>> ParseAirAvailLists(XmlDocument doc)
    {
        var result = new List<List<AvailFltInfo>>();
        foreach (XmlElement avail in doc.SelectNodes("//FareQuoteSuperBB_11/AirAvail")!)
        {
            var segments = new List<AvailFltInfo>();
            foreach (XmlElement flt in avail.SelectNodes(".//AvailFlt")!)
            {
                segments.Add(new AvailFltInfo
                {
                    AirlineCode      = flt.SelectSingleNode("AirV/text()")?.Value ?? "",
                    FlightNumber     = flt.SelectSingleNode("FltNum/text()")?.Value ?? "",
                    StartPoint       = flt.SelectSingleNode("StartAirp/text()")?.Value ?? "",
                    EndPoint         = flt.SelectSingleNode("EndAirp/text()")?.Value ?? "",
                    StartDate        = ParseGalileoDate(flt, "StartDt", "StartTm"),
                    EndDate          = ParseGalileoDate(flt, "EndDt", "EndTm"),
                    Equipment        = flt.SelectSingleNode("Equip/text()")?.Value ?? "",
                    Duration         = int.TryParse(flt.SelectSingleNode("JrnyTm/text()")?.Value, out var d) ? d : 0,
                    StopCount        = int.TryParse(flt.SelectSingleNode("NumStops/text()")?.Value, out var s) ? s : 0,
                    AirportChange    = flt.SelectSingleNode("AirpChg/text()")?.Value,
                    OperatingAirline = flt.SelectSingleNode("OpAirV/text()")?.Value,
                    StartTerminal    = flt.SelectSingleNode("StartTerminal/text()")?.Value,
                    EndTerminal      = flt.SelectSingleNode("EndTerminal/text()")?.Value,
                    FlightTime       = int.TryParse(flt.SelectSingleNode("FltTm/text()")?.Value, out var ft) ? ft : 0,
                });
            }
            result.Add(segments);
        }
        return result;
    }

    // ── Fare parsing helpers ──────────────────────────────────────────────────

    private static string ParsePlatingCarrier(XmlDocument fareInfo)
    {
        foreach (XmlElement msgEl in fareInfo.SelectNodes("//FareInfo/InfoMsg")!)
        {
            string text = msgEl.SelectSingleNode("Text/text()")?.Value ?? "";
            if (text.Contains("DEFAULT PLATING CARRIER"))
            {
                var parts = text.Split(' ');
                return parts[^1].Trim();
            }
        }
        return fareInfo.SelectSingleNode("//FareInfo/DepartRulesInfo/AirV/text()")?.Value ?? "";
    }

    private static (double total, double baseFare, double tax) ParseQuoteAmounts(
        XmlDocument fareInfo, string uniqueKey, string currency)
    {
        foreach (XmlElement qd in fareInfo.SelectNodes("//FareInfo/GenQuoteDetails")!)
        {
            if (qd.SelectSingleNode("UniqueKey/text()")?.Value != uniqueKey) continue;

            double totAmt   = ParseDecimalPos(qd, "TotAmt",    "TotDecPos");
            double equivAmt = ParseDecimalPos(qd, "EquivAmt",  "EquivDecPos");
            double baseAmt  = ParseDecimalPos(qd, "BaseFareAmt","BaseDecPos");

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

    public Task<BaggageInfoDto> GetBaggagesAsync(
        FareDataDto fareData, CancellationToken ct = default)
        => Task.FromResult(new BaggageInfoDto());

    public async Task<List<FareRuleGroupDto>> GetFareRulesAsync(
        FareDataDto fareData, int itinerary, CancellationToken ct = default)
    {
        var rules = new List<FareRuleGroupDto>();
        try
        {
            if (string.IsNullOrWhiteSpace(fareData.SessionData))
                return rules;

            var session = JsonSerializer.Deserialize<GalileoSessionData>(fareData.SessionData);
            if (session is null) return rules;

            var rulesInfoList = itinerary == 0
                ? session.DepartureRulesInfo
                : session.ReturnRulesInfo;

            if (rulesInfoList == null || rulesInfoList.Count == 0)
                return rules;

            string pcc = session.Pcc ?? fareData.PccCode ?? _config["Galileo:Pcc"] ?? "DEFAULT";

            string templateXml = BuildFareRulesRequestXml();
            var requestDoc = new XmlDocument();
            requestDoc.LoadXml(templateXml);

            foreach (string rulesXml in rulesInfoList)
            {
                var frag = requestDoc.CreateDocumentFragment();
                frag.InnerXml = rulesXml;
                requestDoc.DocumentElement?.FirstChild?.AppendChild(frag);
            }

            XmlDocument? response = await SubmitSoapAsync(requestDoc.InnerXml, pcc, ct);
            if (response is null) return rules;

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

            var titles = allRules.Where(r => r.DataType == "F").ToList();
            var texts  = allRules.Where(r => r.DataType == "T").ToList();

            foreach (var title in titles)
            {
                string titleText = title.Text.Length > 4 ? title.Text[4..] : title.Text;

                var matchingTexts = texts
                    .Where(t => t.UniqueKey == title.UniqueKey)
                    .Select(t => t.Text)
                    .ToList();

                rules.Add(new FareRuleGroupDto { Title = titleText, Rules = matchingTexts });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Galileo] GetFareRulesAsync failed");
        }
        return rules;
    }

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

    // ── Internal DTOs ─────────────────────────────────────────────────────────

    private sealed class AvailFltInfo
    {
        public string   AirlineCode      { get; init; } = "";
        public string   FlightNumber     { get; set; }  = "";
        public string   StartPoint       { get; init; } = "";
        public string   EndPoint         { get; init; } = "";
        public DateTime StartDate        { get; init; }
        public DateTime EndDate          { get; init; }
        public string   Equipment        { get; init; } = "";
        public int      Duration         { get; init; }
        public int      StopCount        { get; init; }
        public string   ClassAdult       { get; set; }  = "";
        public string   ClassChild       { get; set; }  = "";
        public string   ClassInfant      { get; set; }  = "";
        public int      StopTime         { get; set; }
        public string?  AirportChange    { get; init; }
        public string?  OperatingAirline { get; init; }
        public string?  StartTerminal    { get; init; }
        public string?  EndTerminal      { get; init; }
        public int      FlightTime       { get; init; }
        public bool     IsLastSegment    { get; set; }

        public AvailFltInfo ShallowCopy() => new()
        {
            AirlineCode      = AirlineCode,
            FlightNumber     = FlightNumber,
            StartPoint       = StartPoint,
            EndPoint         = EndPoint,
            StartDate        = StartDate,
            EndDate          = EndDate,
            Equipment        = Equipment,
            Duration         = Duration,
            StopCount        = StopCount,
            ClassAdult       = ClassAdult,
            ClassChild       = ClassChild,
            ClassInfant      = ClassInfant,
            StopTime         = StopTime,
            AirportChange    = AirportChange,
            OperatingAirline = OperatingAirline,
            StartTerminal    = StartTerminal,
            EndTerminal      = EndTerminal,
            FlightTime       = FlightTime,
            IsLastSegment    = IsLastSegment,
        };
    }

    private sealed class PenaltyRuleInfo
    {
        public string FareComponentNum { get; init; } = "";
        public List<DepRequiredItem> DepRequiredItems { get; init; } = [];
    }

    private sealed class DepRequiredItem
    {
        public string TkNonRef { get; init; } = "";
    }
}

// ── Galileo session DTO ──────────────────────────────────────────────────────

file sealed class GalileoSessionData
{
    [JsonPropertyName("pcc")]         public string?       Pcc                { get; set; }
    [JsonPropertyName("depRules")]    public List<string>  DepartureRulesInfo { get; set; } = [];
    [JsonPropertyName("retRules")]    public List<string>  ReturnRulesInfo    { get; set; } = [];
}

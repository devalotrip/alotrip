using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

public sealed class TripCancellationEntity : Entity<int>
{
    public Guid BookingId { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal BookingPrice { get; set; }
    public string? Value { get; set; }

    private TripCancellationEntity() { }

    public static TripCancellationEntity Create(
        Guid bookingId, decimal markupAmount, decimal markupPercent,
        decimal price, string currency, decimal bookingPrice, string? value)
    {
        return new TripCancellationEntity
        {
            BookingId = bookingId,
            MarkupAmount = markupAmount,
            MarkupPercent = markupPercent,
            Price = price,
            Currency = currency ?? "VND",
            BookingPrice = bookingPrice,
            Value = value,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class AircraftEntity : Entity<int>
{
    public string IATA { get; set; } = default!;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public bool Visible { get; set; } = true;

    private AircraftEntity() { }

    public static AircraftEntity Create(string iata, string? manufacturer, string? model)
    {
        return new AircraftEntity
        {
            IATA = iata,
            Manufacturer = manufacturer,
            Model = model,
            Visible = true,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class AirlineEntity : Entity<int>
{
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public string? Logo { get; set; }
    public bool Visible { get; set; } = true;

    private AirlineEntity() { }

    public static AirlineEntity Create(string code, string? name, string? logo)
    {
        return new AirlineEntity
        {
            Code = code,
            Name = name,
            Logo = logo,
            Visible = true,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class AirlineTypeEntity : Entity<int>
{
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Visible { get; set; } = true;

    private AirlineTypeEntity() { }

    public static AirlineTypeEntity Create(string code, string? name, string? description)
    {
        return new AirlineTypeEntity
        {
            Code = code,
            Name = name,
            Description = description,
            Visible = true,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class BaggageEntity : Entity<int>
{
    public string? BaggageCode { get; set; }
    public int? FlightId { get; set; }
    public string? FlightNumber { get; set; }
    public int? PaxId { get; set; }
    public Guid? BookingId { get; set; }
    public int? Weight { get; set; }
    public int? WeightUnit { get; set; }
    public int? PieceAllowance { get; set; }
    public string? CabinClass { get; set; }
    public string? BaggageType { get; set; }

    private BaggageEntity() { }

    public static BaggageEntity Create(
        string? baggageCode, int? flightId, string? flightNumber,
        int? paxId, Guid? bookingId, int? weight, int? weightUnit,
        int? pieceAllowance, string? cabinClass, string? baggageType)
    {
        return new BaggageEntity
        {
            BaggageCode = baggageCode,
            FlightId = flightId,
            FlightNumber = flightNumber,
            PaxId = paxId,
            BookingId = bookingId,
            Weight = weight,
            WeightUnit = weightUnit,
            PieceAllowance = pieceAllowance,
            CabinClass = cabinClass,
            BaggageType = baggageType,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class PartnerEntity : Entity<int>
{
    public string Name { get; set; } = default!;
    public bool Active { get; set; } = true;

    private PartnerEntity() { }

    public static PartnerEntity Create(string name)
    {
        return new PartnerEntity
        {
            Name = name,
            Active = true,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class AgentPartnerEntity : Entity<int>
{
    public int AgentId { get; set; }
    public int PartnerId { get; set; }
    public int IgnoredMode { get; set; }
    public string? ListStartPoint { get; set; }
    public bool Active { get; set; } = true;

    private AgentPartnerEntity() { }

    public static AgentPartnerEntity Create(int agentId, int partnerId, int ignoredMode, string? listStartPoint)
    {
        return new AgentPartnerEntity
        {
            AgentId = agentId,
            PartnerId = partnerId,
            IgnoredMode = ignoredMode,
            ListStartPoint = listStartPoint,
            Active = true,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class ClassAndNoteEntity : Entity<int>
{
    public string AirlineCode { get; set; } = default!;
    public string Class { get; set; } = default!;
    public string? ShowClass { get; set; }
    public bool NonRefundable { get; set; }
    public bool Visible { get; set; } = true;
    public string? StartAirportCode { get; set; }
    public string? EndAirportCode { get; set; }
    public string? StartCityCode { get; set; }
    public string? EndCityCode { get; set; }
    public string? StartCountryCode { get; set; }
    public string? EndCountryCode { get; set; }
    public string? StartContinentCode { get; set; }
    public string? EndContinentCode { get; set; }

    private ClassAndNoteEntity() { }

    public static ClassAndNoteEntity Create(
        string airlineCode, string classCode, string? showClass,
        bool nonRefundable, string? startAirport, string? endAirport)
    {
        return new ClassAndNoteEntity
        {
            AirlineCode = airlineCode,
            Class = classCode,
            ShowClass = showClass,
            NonRefundable = nonRefundable,
            Visible = true,
            StartAirportCode = startAirport,
            EndAirportCode = endAirport,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class UserRoleEntity : Entity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    private UserRoleEntity() { }

    public static UserRoleEntity Create(string name, string? description)
    {
        return new UserRoleEntity
        {
            Name = name,
            Description = description,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class SearchAnalyticEntity : Entity<int>
{
    /// <summary>Agent code (string) — replaces old int AgentId to match new auth model.</summary>
    public string? AgentCode { get; set; }
    public DateTime Time { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
    public int Itinerary { get; set; }
    public DateTime DepartDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    /// <summary>true = Domestic (within Vietnam), false = International.</summary>
    public bool FlightType { get; set; }
    public string? IPAddress { get; set; }
    /// <summary>
    /// Comma-separated list of engine sources queried for this search (e.g. "Datacom,Maybay,Galileo").
    /// "CACHE" if result was served from cache. Maps to old tblSearchDetail.System concept.
    /// </summary>
    public string? Sources { get; set; }

    private SearchAnalyticEntity() { }

    public static SearchAnalyticEntity Create(
        string? agentCode, string? startPoint, string? endPoint,
        int itinerary, DateTime departDate, DateTime? returnDate,
        bool flightType, string? ipAddress, string? sources = null)
    {
        return new SearchAnalyticEntity
        {
            AgentCode = agentCode,
            Time = DateTime.UtcNow,
            StartPoint = startPoint,
            EndPoint = endPoint,
            Itinerary = itinerary,
            DepartDate = departDate,
            ReturnDate = returnDate,
            FlightType = flightType,
            IPAddress = ipAddress,
            Sources = sources,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class AirlineIgnoreEntity : Entity<int>
{
    public int AgentId { get; set; }
    public string Airline { get; set; } = default!;
    public bool FilterByPlatingCarrier { get; set; }
    public bool FilterByAnySegment { get; set; }
    public bool FilterByAllSegment { get; set; }

    private AirlineIgnoreEntity() { }

    public static AirlineIgnoreEntity Create(
        int agentId, string airline,
        bool filterByPlatingCarrier, bool filterByAnySegment, bool filterByAllSegment)
    {
        return new AirlineIgnoreEntity
        {
            AgentId = agentId,
            Airline = airline,
            FilterByPlatingCarrier = filterByPlatingCarrier,
            FilterByAnySegment = filterByAnySegment,
            FilterByAllSegment = filterByAllSegment,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class AgentPccEntity : Entity<int>
{
    public int AgentId { get; set; }
    public string Pcc { get; set; } = default!;
    public int IgnoredMode { get; set; }
    public string? ListStartPoint { get; set; }
    public bool Active { get; set; } = true;

    private AgentPccEntity() { }

    public static AgentPccEntity Create(int agentId, string pcc, int ignoredMode, string? listStartPoint)
    {
        return new AgentPccEntity
        {
            AgentId = agentId,
            Pcc = pcc,
            IgnoredMode = ignoredMode,
            ListStartPoint = listStartPoint,
            Active = true,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class PccEntity : Entity<string>
{
    public bool Active { get; set; } = true;

    private PccEntity() { }

    public static PccEntity Create(string pcc)
    {
        return new PccEntity
        {
            Id = pcc,
            Active = true,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

public sealed class LccInfoEntity : Entity<int>
{
    public int AgentId { get; set; }
    public string Airline { get; set; } = default!;
    public bool AllowSearch { get; set; }
    public bool AllowBook { get; set; }
    public string? ProxyServerId { get; set; }
    public string? ProxyServerBookId { get; set; }

    private LccInfoEntity() { }

    public static LccInfoEntity Create(int agentId, string airline, bool allowSearch, bool allowBook)
    {
        return new LccInfoEntity
        {
            AgentId = agentId,
            Airline = airline,
            AllowSearch = allowSearch,
            AllowBook = allowBook,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}

// ── Agent ─────────────────────────────────────────────────────────────────────
/// <summary>
/// Maps to the "agents" PostgreSQL table.
/// Replaces old tblAgent (30 columns) from LINQ-to-SQL.
/// Previously used raw SQL only; now EF-managed for full CRUD.
/// </summary>
public sealed class AgentEntity : Entity<int>
{
    public string AgentCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Tel { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    // ── LCC flags ─────────────────────────────────────────────────────────
    public bool LccVnActiveDomestic { get; set; }
    public bool LccVnActiveGlobal { get; set; }

    // ── Galileo config ────────────────────────────────────────────────────
    public string? GalileoPcc { get; set; }
    public bool GalileoActive { get; set; }

    // ── System settings ───────────────────────────────────────────────────
    public string? DefaultCurrency { get; set; }
    public bool EnableCache { get; set; }
    public int CacheTimeMinutes { get; set; }
    public bool SendMailInApi { get; set; }
    public int EmailSender { get; set; }
    public int CombinedMode { get; set; }

    // ── Agent lifecycle ───────────────────────────────────────────────────
    public DateTime ExpiryDate { get; set; }
    public bool Active { get; set; } = true;

    // ── Fee markup ────────────────────────────────────────────────────────
    public decimal BaggageFeePercent { get; set; }
    public decimal BaggageFeeAmount { get; set; }

    private AgentEntity() { }

    public static AgentEntity Create(
        string agentCode, string name, string email, string passwordHash,
        string? address = null, string? tel = null,
        bool lccVnActiveDomestic = false, bool lccVnActiveGlobal = false,
        string? galileoPcc = null, bool galileoActive = false,
        string? defaultCurrency = "VND", bool enableCache = false, int cacheTimeMinutes = 15,
        bool sendMailInApi = false, int emailSender = 0, int combinedMode = 0,
        DateTime? expiryDate = null,
        decimal baggageFeePercent = 0, decimal baggageFeeAmount = 0)
    {
        return new AgentEntity
        {
            AgentCode = agentCode,
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Address = address,
            Tel = tel,
            LccVnActiveDomestic = lccVnActiveDomestic,
            LccVnActiveGlobal = lccVnActiveGlobal,
            GalileoPcc = galileoPcc,
            GalileoActive = galileoActive,
            DefaultCurrency = defaultCurrency,
            EnableCache = enableCache,
            CacheTimeMinutes = cacheTimeMinutes,
            SendMailInApi = sendMailInApi,
            EmailSender = emailSender,
            CombinedMode = combinedMode,
            ExpiryDate = expiryDate ?? DateTime.UtcNow.AddYears(1),
            Active = true,
            BaggageFeePercent = baggageFeePercent,
            BaggageFeeAmount = baggageFeeAmount,
            CreatedOnUtc = DateTime.UtcNow
        };
    }

    /// <summary>Applies a full update (PUT semantics — all mutable fields).</summary>
    public void Update(
        string name, string email, string? address, string? tel,
        bool lccVnActiveDomestic, bool lccVnActiveGlobal,
        string? galileoPcc, bool galileoActive,
        string? defaultCurrency, bool enableCache, int cacheTimeMinutes,
        bool sendMailInApi, int emailSender, int combinedMode,
        DateTime expiryDate, bool active,
        decimal baggageFeePercent, decimal baggageFeeAmount)
    {
        Name = name;
        Email = email;
        Address = address;
        Tel = tel;
        LccVnActiveDomestic = lccVnActiveDomestic;
        LccVnActiveGlobal = lccVnActiveGlobal;
        GalileoPcc = galileoPcc;
        GalileoActive = galileoActive;
        DefaultCurrency = defaultCurrency;
        EnableCache = enableCache;
        CacheTimeMinutes = cacheTimeMinutes;
        SendMailInApi = sendMailInApi;
        EmailSender = emailSender;
        CombinedMode = combinedMode;
        ExpiryDate = expiryDate;
        Active = active;
        BaggageFeePercent = baggageFeePercent;
        BaggageFeeAmount = baggageFeeAmount;
    }

    /// <summary>Soft-delete: sets DeletedAt + Active=false.</summary>
    public void SoftDelete()
    {
        Active = false;
        DeletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Lookup table for passenger types (ADT, CHD, INF).
/// PK is Code (string, not auto-increment).
/// Matches old tblPassengerType with multilingual name fields.
/// </summary>
public sealed class PassengerTypeEntity
{
    public string Code       { get; set; } = default!;
    public string? Icon      { get; set; }
    public string? NameVi    { get; set; }
    public string? NameEn    { get; set; }
    public string? NameFr    { get; set; }
    public string? Description { get; set; }

    private PassengerTypeEntity() { }

    public static PassengerTypeEntity Create(
        string code, string? icon, string? nameVi, string? nameEn, string? nameFr, string? description)
    {
        return new PassengerTypeEntity
        {
            Code = code,
            Icon = icon,
            NameVi = nameVi,
            NameEn = nameEn,
            NameFr = nameFr,
            Description = description,
        };
    }

    public void Update(string? icon, string? nameVi, string? nameEn, string? nameFr, string? description)
    {
        Icon = icon;
        NameVi = nameVi;
        NameEn = nameEn;
        NameFr = nameFr;
        Description = description;
    }
}
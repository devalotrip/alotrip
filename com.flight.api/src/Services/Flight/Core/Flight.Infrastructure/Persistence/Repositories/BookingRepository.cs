using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Enums;
using Flight.Domain.Repositories;
using Flight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DomainDto = Flight.Domain.Repositories;

namespace Flight.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(ApplicationDbContext db)
    : IBookingRepository,
      IAgentRepository,
      IReferenceDataRepository,
      IBookingAddonRepository,
      IUserRepository,
      ISearchAnalyticRepository,
      IAdminBookingRepository
{
    // Core Booking Operations
    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);

    public async Task<BookingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Bookings.FindAsync([id], ct);

    public async Task<BookingEntity?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await db.Bookings
            .Include(b => b.Flights).ThenInclude(f => f.Segments)
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<BookingEntity?> GetByCodeAsync(string bookingCode, CancellationToken ct = default)
        => await db.Bookings.FirstOrDefaultAsync(b => b.BookingCode == bookingCode, ct);

    public async Task<IEnumerable<BookingEntity>> GetByAgentAsync(string agentCode, int page, int pageSize, CancellationToken ct = default)
    {
        return await db.Bookings
            .Where(b => b.AgentCode == agentCode)
            .OrderByDescending(b => b.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsActiveByFareIdAsync(string fareId, string agentCode, CancellationToken ct = default)
        => await db.Bookings.AnyAsync(b => b.FareId == fareId && b.AgentCode == agentCode && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed), ct);

    public async Task<List<BookingEntity>> GetCandidateDuplicateBookingsAsync(
        string origin, string destination, DateTime departDate, DateTime? returnDate,
        int passengerCount, string agentCode, double duplicateWindowMinutes,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-duplicateWindowMinutes);

        // Use range comparison instead of .Date (which Npgsql may not translate / prevents index usage)
        var departStart = departDate.Date;                   // start of day
        var departEnd   = departStart.AddDays(1);            // start of next day

        var query = db.Bookings
            .Include(b => b.Flights).ThenInclude(f => f.Segments)
            .Include(b => b.Passengers)
            .Where(b => b.AgentCode == agentCode
                && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                && b.Origin == origin
                && b.Destination == destination
                && b.DepartDate >= departStart && b.DepartDate < departEnd
                && b.CreatedOnUtc >= cutoff);

        // Return date filter: roundtrip must match return date; one-way must have no return date
        if (returnDate.HasValue)
        {
            var retStart = returnDate.Value.Date;
            var retEnd   = retStart.AddDays(1);
            query = query.Where(b => b.ReturnDate.HasValue
                && b.ReturnDate.Value >= retStart && b.ReturnDate.Value < retEnd);
        }
        else
        {
            query = query.Where(b => !b.ReturnDate.HasValue);
        }

        // Filter by passenger count in memory (no direct count column)
        var candidates = await query.ToListAsync(ct);
        return candidates.Where(b => b.Passengers.Count == passengerCount).ToList();
    }

    public async Task<TicketEntity?> GetTicketByNumberAsync(string ticketNumber, CancellationToken ct = default)
        => await db.Tickets.FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber, ct);

    public async Task AddAsync(BookingEntity booking, CancellationToken ct = default)
    {
        await db.Bookings.AddAsync(booking, ct);
        await db.SaveChangesAsync(ct);
    }

    public void Update(BookingEntity booking)
        => db.Bookings.Update(booking);

    // Airline Operations
    public IQueryable<AirlineEntity> GetAirlinesQuery()
        => db.Airlines;

    public async Task<AirlineEntity?> GetAirlineByIdAsync(int id, CancellationToken ct = default)
        => await db.Airlines.FindAsync([id], ct);

    public async Task AddAirlineAsync(AirlineEntity entity, CancellationToken ct = default)
    {
        await db.Airlines.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // Aircraft Operations
    public IQueryable<AircraftEntity> GetAircraftsQuery()
        => db.Aircrafts;

    public async Task<AircraftEntity?> GetAircraftByIdAsync(int id, CancellationToken ct = default)
        => await db.Aircrafts.FindAsync([id], ct);

    public async Task AddAircraftAsync(AircraftEntity entity, CancellationToken ct = default)
    {
        await db.Aircrafts.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // AirlineType Operations
    public IQueryable<AirlineTypeEntity> GetAirlineTypesQuery()
        => db.AirlineTypes;

    public async Task<AirlineTypeEntity?> GetAirlineTypeByIdAsync(int id, CancellationToken ct = default)
        => await db.AirlineTypes.FindAsync([id], ct);

    public async Task AddAirlineTypeAsync(AirlineTypeEntity entity, CancellationToken ct = default)
    {
        await db.AirlineTypes.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // PassengerType Operations
    public IQueryable<PassengerTypeEntity> GetPassengerTypesQuery()
        => db.PassengerTypes;

    public async Task<PassengerTypeEntity?> GetPassengerTypeByCodeAsync(string code, CancellationToken ct = default)
        => await db.PassengerTypes.FindAsync([code], ct);

    public async Task AddPassengerTypeAsync(PassengerTypeEntity entity, CancellationToken ct = default)
    {
        await db.PassengerTypes.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    public void RemovePassengerType(PassengerTypeEntity entity)
        => db.PassengerTypes.Remove(entity);

    // Baggage Operations
    public IQueryable<BaggageEntity> GetBaggagesQuery()
        => db.Baggages;

    public async Task<BaggageEntity?> GetBaggageByIdAsync(int id, CancellationToken ct = default)
        => await db.Baggages.FindAsync([id], ct);

    public async Task AddBaggageAsync(BaggageEntity entity, CancellationToken ct = default)
    {
        await db.Baggages.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<BaggageEntity>> GetBaggagesByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await db.Baggages.Where(b => b.BookingId == bookingId && b.DeletedAt == null).ToListAsync(ct);

    // Partner Operations
    public IQueryable<PartnerEntity> GetPartnersQuery()
        => db.Partners;

    public async Task<PartnerEntity?> GetPartnerByIdAsync(int id, CancellationToken ct = default)
        => await db.Partners.FindAsync([id], ct);

    public async Task AddPartnerAsync(PartnerEntity entity, CancellationToken ct = default)
    {
        await db.Partners.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // AgentPartner Operations
    public IQueryable<AgentPartnerEntity> GetAgentPartnersQuery()
        => db.AgentPartners;

    public async Task<AgentPartnerEntity?> GetAgentPartnerByIdAsync(int id, CancellationToken ct = default)
        => await db.AgentPartners.FindAsync([id], ct);

    public async Task AddAgentPartnerAsync(AgentPartnerEntity entity, CancellationToken ct = default)
    {
        await db.AgentPartners.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // ClassNote Operations
    public IQueryable<ClassAndNoteEntity> GetClassNotesQuery()
        => db.ClassAndNotes;

    public async Task<ClassAndNoteEntity?> GetClassNoteByIdAsync(int id, CancellationToken ct = default)
        => await db.ClassAndNotes.FindAsync([id], ct);

    public async Task AddClassNoteAsync(ClassAndNoteEntity entity, CancellationToken ct = default)
    {
        await db.ClassAndNotes.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // UserRole Operations
    public IQueryable<UserRoleEntity> GetUserRolesQuery()
        => db.UserRoles;

    public async Task<UserRoleEntity?> GetUserRoleByIdAsync(int id, CancellationToken ct = default)
        => await db.UserRoles.FindAsync([id], ct);

    public async Task AddUserRoleAsync(UserRoleEntity entity, CancellationToken ct = default)
    {
        await db.UserRoles.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // AirlineIgnore Operations
    public IQueryable<AirlineIgnoreEntity> GetAirlineIgnoresQuery()
        => db.AirlineIgnores;

    public async Task<AirlineIgnoreEntity?> GetAirlineIgnoreByIdAsync(int id, CancellationToken ct = default)
        => await db.AirlineIgnores.FindAsync([id], ct);

    public async Task AddAirlineIgnoreAsync(AirlineIgnoreEntity entity, CancellationToken ct = default)
    {
        await db.AirlineIgnores.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // UserAccount Operations
    public IQueryable<UserAccountEntity> GetUserAccountsQuery()
        => db.UserAccounts;

    public async Task<UserAccountEntity?> GetUserAccountByIdAsync(int id, CancellationToken ct = default)
        => await db.UserAccounts.FindAsync([id], ct);

    public async Task AddUserAccountAsync(UserAccountEntity entity, CancellationToken ct = default)
    {
        await db.UserAccounts.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UserAccountExistsAsync(string email, CancellationToken ct = default)
        => await db.UserAccounts.AnyAsync(u => u.Email == email, ct);

    // Invoice Operations
    public IQueryable<InvoiceEntity> GetInvoicesQuery()
        => db.Invoices;

    public async Task<InvoiceEntity?> GetInvoiceByIdAsync(int id, CancellationToken ct = default)
        => await db.Invoices.FindAsync([id], ct);

    public async Task AddInvoiceAsync(InvoiceEntity entity, CancellationToken ct = default)
    {
        await db.Invoices.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // Insurance Operations
    public IQueryable<InsuranceEntity> GetInsurancesQuery()
        => db.Insurances;

    public async Task<InsuranceEntity?> GetInsuranceByIdAsync(int id, CancellationToken ct = default)
        => await db.Insurances.FindAsync([id], ct);

    public async Task AddInsuranceAsync(InsuranceEntity entity, CancellationToken ct = default)
    {
        await db.Insurances.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // CarRental Operations
    public IQueryable<CarRentalEntity> GetCarRentalsQuery()
        => db.CarRentals;

    public async Task<CarRentalEntity?> GetCarRentalByIdAsync(int id, CancellationToken ct = default)
        => await db.CarRentals.FindAsync([id], ct);

    public async Task AddCarRentalAsync(CarRentalEntity entity, CancellationToken ct = default)
    {
        await db.CarRentals.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // TripTour Operations
    public IQueryable<TripTourEntity> GetTripToursQuery()
        => db.TripTours;

    public async Task<TripTourEntity?> GetTripTourByIdAsync(int id, CancellationToken ct = default)
        => await db.TripTours.FindAsync([id], ct);

    public async Task AddTripTourAsync(TripTourEntity entity, CancellationToken ct = default)
    {
        await db.TripTours.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // TripVisa Operations
    public IQueryable<TripVisaEntity> GetTripVisasQuery()
        => db.TripVisas;

    public async Task<TripVisaEntity?> GetTripVisaByIdAsync(int id, CancellationToken ct = default)
        => await db.TripVisas.FindAsync([id], ct);

    public async Task AddTripVisaAsync(TripVisaEntity entity, CancellationToken ct = default)
    {
        await db.TripVisas.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // TripCancellation Operations
    public IQueryable<TripCancellationEntity> GetTripCancellationsQuery()
        => db.TripCancellations;

    public async Task<TripCancellationEntity?> GetTripCancellationByIdAsync(int id, CancellationToken ct = default)
        => await db.TripCancellations.FindAsync([id], ct);

    public async Task AddTripCancellationAsync(TripCancellationEntity entity, CancellationToken ct = default)
    {
        await db.TripCancellations.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    // Agent Operations
    public async Task<IEnumerable<AgentDto>> GetAgentsAsync(int page, int pageSize, bool? active, CancellationToken ct = default)
    {
        var sql = "SELECT id AS \"Id\", agent_code AS \"AgentCode\", name AS \"Name\", email AS \"Email\", " +
                 "tel AS \"Tel\", active AS \"Active\", default_currency AS \"DefaultCurrency\", " +
                 "created_at AS \"CreatedAt\", expiry_date AS \"ExpiryDate\" FROM agents";
        if (active.HasValue)
            sql += " WHERE active = {0}";
        sql += " ORDER BY id OFFSET {1} LIMIT {2}";

        int offset = (page - 1) * pageSize;
        if (active.HasValue)
            return await db.Database.SqlQueryRaw<DomainDto.AgentDto>(sql, active.Value, offset, pageSize).ToListAsync(ct);
        else
            return await db.Database.SqlQueryRaw<DomainDto.AgentDto>(sql, offset, pageSize).ToListAsync(ct);
    }

    public async Task<AgentDetailDto?> GetAgentByIdAsync(int id, CancellationToken ct = default)
    {
        var sql = "SELECT id AS \"Id\", agent_code AS \"AgentCode\", name AS \"Name\", email AS \"Email\", " +
                 "tel AS \"Tel\", address AS \"Address\", active AS \"Active\", galileo_pcc AS \"GalileoPcc\", " +
                 "galileo_active AS \"GalileoActive\", lcc_vn_active_domestic AS \"LccVnActiveDomestic\", " +
                 "lcc_vn_active_global AS \"LccVnActiveGlobal\", enable_cache AS \"EnableCache\", " +
                 "cache_time_minutes AS \"CacheTimeMinutes\", default_currency AS \"DefaultCurrency\", " +
                 "baggage_fee_percent AS \"BaggageFeePercent\", baggage_fee_amount AS \"BaggageFeeAmount\", " +
                 "created_at AS \"CreatedAt\", expiry_date AS \"ExpiryDate\" FROM agents WHERE id = {0}";
        
        var result = await db.Database.SqlQueryRaw<DomainDto.AgentDetailDto>(sql, id).FirstOrDefaultAsync(ct);
        return result;
    }

    public async Task<bool> ToggleAgentActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var rows = await db.Database.ExecuteSqlRawAsync("UPDATE agents SET active = {0} WHERE id = {1}", active, id, ct);
        return rows > 0;
    }

    public async Task<bool> ChangeAgentPasswordAsync(int id, string passwordHash, CancellationToken ct = default)
    {
        var rows = await db.Database.ExecuteSqlRawAsync("UPDATE agents SET password_hash = {0} WHERE id = {1}", passwordHash, id, ct);
        return rows > 0;
    }

    public async Task<AgentEntity> CreateAgentAsync(AgentEntity entity, CancellationToken ct = default)
    {
        await db.Agents.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<AgentEntity?> GetAgentEntityByIdAsync(int id, CancellationToken ct = default)
        => await db.Agents.FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, ct);

    public async Task<bool> AgentCodeExistsAsync(string agentCode, CancellationToken ct = default)
        => await db.Agents.AnyAsync(a => a.AgentCode == agentCode && a.DeletedAt == null, ct);

    public async Task UpdateAgentAsync(AgentEntity entity, CancellationToken ct = default)
    {
        db.Agents.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAgentAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Agents.FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, ct);
        if (entity is null) return false;
        entity.SoftDelete();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<PccEntity> GetPccsQuery()
        => db.Pccs;

    public async Task<PccEntity?> GetPccByIdAsync(string pcc, CancellationToken ct = default)
        => await db.Pccs.FindAsync([pcc], ct);

    public async Task AddPccAsync(PccEntity entity, CancellationToken ct = default)
        => await db.Pccs.AddAsync(entity, ct);

    public async Task<bool> DeletePccAsync(string pcc, CancellationToken ct = default)
    {
        var entity = await db.Pccs.FindAsync([pcc], ct);
        if (entity is null) return false;
        entity.Active = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<LccInfoEntity> GetLccInfosQuery()
        => db.LccInfos;

    public async Task<LccInfoEntity?> GetLccInfoByIdAsync(int id, CancellationToken ct = default)
        => await db.LccInfos.FindAsync([id], ct);

    public async Task AddLccInfoAsync(LccInfoEntity entity, CancellationToken ct = default)
    {
        await db.LccInfos.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> LccInfoExistsAsync(int agentId, string airline, CancellationToken ct = default)
        => await db.LccInfos.AnyAsync(l => l.AgentId == agentId && l.Airline == airline && l.DeletedAt == null, ct);

    public async Task<bool> DeleteLccInfoAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.LccInfos.FindAsync([id], ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<AgentPccEntity> GetAgentPccsQuery()
        => db.AgentPccs;

    public async Task<AgentPccEntity?> GetAgentPccByIdAsync(int id, CancellationToken ct = default)
        => await db.AgentPccs.FindAsync([id], ct);

    public async Task AddAgentPccAsync(AgentPccEntity entity, CancellationToken ct = default)
    {
        await db.AgentPccs.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> AgentPccExistsAsync(int agentId, string pcc, CancellationToken ct = default)
        => await db.AgentPccs.AnyAsync(a => a.AgentId == agentId && a.Pcc == pcc && a.Active, ct);

    public async Task<bool> DeleteAgentPccAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.AgentPccs.FindAsync([id], ct);
        if (entity is null) return false;
        entity.Active = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<SearchAnalyticEntity> GetSearchAnalyticsQuery()
        => db.SearchAnalytics;

    public async Task<SearchAnalyticEntity?> GetSearchAnalyticByIdAsync(int id, CancellationToken ct = default)
        => await db.SearchAnalytics.FindAsync([id], ct);

    public async Task AddSearchAnalyticAsync(SearchAnalyticEntity entity, CancellationToken ct = default)
    {
        await db.SearchAnalytics.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<CommissionDto>> GetCommissionsAsync(int? agentId, CancellationToken ct = default)
    {
        string whereSql = agentId.HasValue ? "WHERE c.agent_id = {0}" : "";
        string sql = $"""
            SELECT c.id AS "Id", c.agent_id AS "AgentId", a.agent_code AS "AgentCode",
                   c.airline_group AS "AirlineGroup",
                   c.start_region AS "StartRegion", c.end_region AS "EndRegion",
                   c.currency AS "Currency",
                   c.fee_adt_one_way AS "FeeAdtOneWay",
                   c.fee_chd_one_way AS "FeeChdOneWay",
                   c.fee_inf_one_way AS "FeeInfOneWay",
                   c.fee_adt_round_trip AS "FeeAdtRoundTrip",
                   c.fee_chd_round_trip AS "FeeChdRoundTrip",
                   c.fee_inf_round_trip AS "FeeInfRoundTrip",
                   c.fee_by_percent AS "FeeByPercent",
                   c.commission AS "Commission",
                   c.com_by_percent AS "ComByPercent",
                   c.com_percent_on_base_fare AS "ComPercentOnBaseFare"
            FROM   commissions c
            JOIN   agents a ON a.id = c.agent_id
            {whereSql}
            ORDER  BY c.agent_id, c.airline_group, c.start_region, c.end_region
            """;
        if (agentId.HasValue)
            return await db.Database.SqlQueryRaw<CommissionDto>(sql, agentId.Value).ToListAsync(ct);
        else
            return await db.Database.SqlQueryRaw<CommissionDto>(sql).ToListAsync(ct);
    }

    public async Task<bool> UpsertCommissionAsync(UpsertCommissionRequest req, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO commissions
                (agent_id, airline_group, start_region, end_region, currency,
                 fee_adt_one_way, fee_chd_one_way, fee_inf_one_way,
                 fee_adt_round_trip, fee_chd_round_trip, fee_inf_round_trip,
                 fee_by_percent, commission, com_by_percent, com_percent_on_base_fare)
            VALUES
                ({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14})
            ON CONFLICT (agent_id, airline_group, start_region, end_region) DO UPDATE SET
                currency             = EXCLUDED.currency,
                fee_adt_one_way      = EXCLUDED.fee_adt_one_way,
                fee_chd_one_way      = EXCLUDED.fee_chd_one_way,
                fee_inf_one_way      = EXCLUDED.fee_inf_one_way,
                fee_adt_round_trip   = EXCLUDED.fee_adt_round_trip,
                fee_chd_round_trip   = EXCLUDED.fee_chd_round_trip,
                fee_inf_round_trip   = EXCLUDED.fee_inf_round_trip,
                fee_by_percent       = EXCLUDED.fee_by_percent,
                commission           = EXCLUDED.commission,
                com_by_percent       = EXCLUDED.com_by_percent,
                com_percent_on_base_fare = EXCLUDED.com_percent_on_base_fare
            """,
            req.AgentId, req.AirlineGroup, req.StartRegion, req.EndRegion, req.Currency,
            req.FeeAdtOneWay, req.FeeChdOneWay, req.FeeInfOneWay,
            req.FeeAdtRoundTrip, req.FeeChdRoundTrip, req.FeeInfRoundTrip,
            req.FeeByPercent, req.Commission, req.ComByPercent, req.ComPercentOnBaseFare);
        return true;
    }

    public async Task<bool> DeleteCommissionAsync(int id, CancellationToken ct = default)
    {
        int rows = await db.Database.ExecuteSqlRawAsync("DELETE FROM commissions WHERE id = {0}", id, ct);
        return rows > 0;
    }

    public async Task<IEnumerable<AdminBookingListDto>> GetAdminBookingsAsync(string? agentCode, string? status, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Bookings
            .Include(b => b.Passengers)
            .Include(b => b.Tickets)
            .Where(b => (agentCode == null || b.AgentCode == agentCode)
                     && (status == null || b.Status.ToString() == status)
                     && (from == null || b.DepartDate >= from)
                     && (to == null || b.DepartDate <= to));

        int skip = (Math.Max(1, page) - 1) * pageSize;
        var list = await query
            .OrderByDescending(b => b.CreatedOnUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);

        return list.Select(b => new AdminBookingListDto
        {
            Id = b.Id, BookingCode = b.BookingCode, AgentCode = b.AgentCode,
            Source = b.Source.ToString(), TripType = b.TripType.ToString(), Status = b.Status.ToString(),
            Origin = b.Origin, Destination = b.Destination, DepartDate = b.DepartDate, ReturnDate = b.ReturnDate,
            TotalAmount = b.TotalAmount, Currency = b.Currency,
            ContactName = b.ContactName, ContactEmail = b.ContactEmail,
            ExpiresAt = b.ExpiresAt ?? DateTime.MinValue, CreatedOnUtc = b.CreatedOnUtc,
            PassengerCount = b.Passengers.Count, TicketCount = b.Tickets.Count
        });
    }

    public async Task<AdminBookingDetailDto?> GetAdminBookingByIdAsync(Guid id, CancellationToken ct = default)
    {
        var booking = await db.Bookings
            .Include(b => b.Flights).ThenInclude(f => f.Segments)
            .Include(b => b.Passengers)
            .Include(b => b.Tickets)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (booking is null) return null;

        return new AdminBookingDetailDto
        {
            Id = booking.Id, BookingCode = booking.BookingCode, AgentCode = booking.AgentCode,
            Source = booking.Source.ToString(), TripType = booking.TripType.ToString(), Status = booking.Status.ToString(),
            Origin = booking.Origin, Destination = booking.Destination,
            DepartDate = booking.DepartDate, ReturnDate = booking.ReturnDate,
            TotalAmount = booking.TotalAmount, ServiceFee = booking.ServiceFee, Currency = booking.Currency,
            ContactName = booking.ContactName, ContactEmail = booking.ContactEmail, ContactPhone = booking.ContactPhone,
            ExpiresAt = booking.ExpiresAt ?? DateTime.MinValue, CreatedOnUtc = booking.CreatedOnUtc,
            FareId = booking.FareId, PccCode = booking.PccCode,
            Passengers = booking.Passengers.Select(p => new AdminPassengerDto
            {
                FirstName = p.FirstName, LastName = p.LastName, Gender = p.Gender.ToString(),
                Type = p.Type.ToString(), BirthDate = p.BirthDate?.ToString("yyyy-MM-dd"),
                PassportNo = p.PassportNo, BaggageKg = p.BaggageKg, FareAmount = p.FareAmount
            }).ToList(),
            Flights = booking.Flights.Select(f => new AdminFlightDto
            {
                Airline = f.Airline, Origin = f.Origin, Destination = f.Destination,
                DepartTime = f.DepartTime, ArriveTime = f.ArriveTime,
                Segments = f.Segments.Select(s => new AdminSegmentDto
                {
                    FlightNumber = s.FlightNumber, Airline = s.Airline, Origin = s.Origin, Destination = s.Destination,
                    DepartTime = s.DepartTime, ArriveTime = s.ArriveTime, CabinClass = s.CabinClass, AircraftType = s.AircraftType ?? string.Empty
                }).ToList()
            }).ToList(),
            Tickets = booking.Tickets.Select(t => new AdminTicketDto
            {
                TicketNumber = t.TicketNumber, PassengerName = t.PassengerName, Airline = t.Airline, IssuedAt = t.IssuedAt
            }).ToList()
        };
    }

    public async Task<bool> UpdateBookingStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        int rows = await db.Database.ExecuteSqlRawAsync(
            "UPDATE bookings SET status = {0} WHERE id = {1}",
            status.ToUpperInvariant(), id, ct);
        return rows > 0;
    }

    public async Task<DomainDto.BookingsSummaryDto> GetBookingsSummaryAsync(string? agentCode, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var query = db.Bookings.AsQueryable();

        if (!string.IsNullOrEmpty(agentCode))
            query = query.Where(b => b.AgentCode == agentCode);
        if (fromDate.HasValue)
            query = query.Where(b => b.CreatedOnUtc >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(b => b.CreatedOnUtc <= toDate.Value);

        // Server-side aggregation — avoids loading all rows into memory
        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalBookings     = g.Count(),
                ConfirmedBookings = g.Count(b => b.Status == BookingStatus.Confirmed),
                PendingBookings   = g.Count(b => b.Status == BookingStatus.Pending),
                CancelledBookings = g.Count(b => b.Status == BookingStatus.Cancelled),
                ExpiredBookings   = g.Count(b => b.Status == BookingStatus.Expired),
                TotalRevenue      = g.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => b.TotalAmount)
            })
            .FirstOrDefaultAsync(ct);

        return new DomainDto.BookingsSummaryDto
        {
            TotalBookings     = summary?.TotalBookings ?? 0,
            ConfirmedBookings = summary?.ConfirmedBookings ?? 0,
            PendingBookings   = summary?.PendingBookings ?? 0,
            CancelledBookings = summary?.CancelledBookings ?? 0,
            ExpiredBookings   = summary?.ExpiredBookings ?? 0,
            TotalRevenue      = summary?.TotalRevenue ?? 0,
            Currency          = "VND"
        };
    }

    public async Task<DomainDto.TicketsIssuedDto> GetTicketsIssuedAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var count = await db.Tickets
            .Where(t => t.IssuedAt >= from && t.IssuedAt <= to)
            .CountAsync(ct);

        return new DomainDto.TicketsIssuedDto
        {
            TotalTickets = count,
            FromDate = from,
            ToDate = to
        };
    }

    public async Task<DomainDto.SearchDetailsDto> GetSearchDetailsAsync(string? agentCode, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.SearchAnalytics.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(s => s.Time >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(s => s.Time <= toDate.Value);

        var total = await query.CountAsync(ct);

        int skip = (Math.Max(1, page) - 1) * pageSize;
        var searches = await query
            .OrderByDescending(s => s.Time)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);

        return new DomainDto.SearchDetailsDto
        {
            TotalSearches = total,
            Page = page,
            PageSize = pageSize,
            Searches = searches.Select(s => new DomainDto.SearchDetailItemDto
            {
                AgentCode = s.AgentCode ?? "",
                Origin = s.StartPoint ?? "",
                Destination = s.EndPoint ?? "",
                DepartDate = s.DepartDate,
                AdultCount = 1,
                ChildCount = 0,
                InfantCount = 0,
                SearchedAt = s.Time
            }).ToList()
        };
    }
}

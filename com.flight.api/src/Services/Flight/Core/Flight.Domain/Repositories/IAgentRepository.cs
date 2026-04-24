using Flight.Domain.Aggregates.Booking;

namespace Flight.Domain.Repositories;

public interface IAgentRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);

    // Agent CRUD
    Task<IEnumerable<AgentDto>> GetAgentsAsync(int page, int pageSize, bool? active, CancellationToken ct = default);
    Task<AgentDetailDto?> GetAgentByIdAsync(int id, CancellationToken ct = default);
    Task<AgentEntity?> GetAgentEntityByIdAsync(int id, CancellationToken ct = default);
    Task<bool> AgentCodeExistsAsync(string agentCode, CancellationToken ct = default);
    Task<AgentEntity> CreateAgentAsync(AgentEntity entity, CancellationToken ct = default);
    Task UpdateAgentAsync(AgentEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAgentAsync(int id, CancellationToken ct = default);
    Task<bool> ToggleAgentActiveAsync(int id, bool active, CancellationToken ct = default);
    Task<bool> ChangeAgentPasswordAsync(int id, string passwordHash, CancellationToken ct = default);

    // Commission operations
    Task<IEnumerable<CommissionDto>> GetCommissionsAsync(int? agentId, CancellationToken ct = default);
    Task<bool> UpsertCommissionAsync(UpsertCommissionRequest req, CancellationToken ct = default);
    Task<bool> DeleteCommissionAsync(int id, CancellationToken ct = default);

    // Partner operations
    IQueryable<PartnerEntity> GetPartnersQuery();
    Task<PartnerEntity?> GetPartnerByIdAsync(int id, CancellationToken ct = default);
    Task AddPartnerAsync(PartnerEntity entity, CancellationToken ct = default);

    // AgentPartner operations
    IQueryable<AgentPartnerEntity> GetAgentPartnersQuery();
    Task<AgentPartnerEntity?> GetAgentPartnerByIdAsync(int id, CancellationToken ct = default);
    Task AddAgentPartnerAsync(AgentPartnerEntity entity, CancellationToken ct = default);

    // AgentPcc operations
    IQueryable<AgentPccEntity> GetAgentPccsQuery();
    Task<AgentPccEntity?> GetAgentPccByIdAsync(int id, CancellationToken ct = default);
    Task AddAgentPccAsync(AgentPccEntity entity, CancellationToken ct = default);
    Task<bool> AgentPccExistsAsync(int agentId, string pcc, CancellationToken ct = default);
    Task<bool> DeleteAgentPccAsync(int id, CancellationToken ct = default);

    // LccInfo operations
    IQueryable<LccInfoEntity> GetLccInfosQuery();
    Task<LccInfoEntity?> GetLccInfoByIdAsync(int id, CancellationToken ct = default);
    Task AddLccInfoAsync(LccInfoEntity entity, CancellationToken ct = default);
    Task<bool> LccInfoExistsAsync(int agentId, string airline, CancellationToken ct = default);
    Task<bool> DeleteLccInfoAsync(int id, CancellationToken ct = default);

    // AirlineIgnore operations
    IQueryable<AirlineIgnoreEntity> GetAirlineIgnoresQuery();
    Task<AirlineIgnoreEntity?> GetAirlineIgnoreByIdAsync(int id, CancellationToken ct = default);
    Task AddAirlineIgnoreAsync(AirlineIgnoreEntity entity, CancellationToken ct = default);

    // Pcc operations
    IQueryable<PccEntity> GetPccsQuery();
    Task<PccEntity?> GetPccByIdAsync(string pcc, CancellationToken ct = default);
    Task AddPccAsync(PccEntity entity, CancellationToken ct = default);
    Task<bool> DeletePccAsync(string pcc, CancellationToken ct = default);
}

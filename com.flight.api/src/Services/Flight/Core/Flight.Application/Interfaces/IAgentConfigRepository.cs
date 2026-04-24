using Flight.Application.Dtos;

namespace Flight.Application.Interfaces;

/// <summary>
/// Loads per-agent configuration used to control engine selection and airline filtering.
/// </summary>
public interface IAgentConfigRepository
{
    /// <summary>
    /// Returns the agent's configuration, or <c>null</c> if the agent code is not found
    /// or the agent is inactive.
    /// </summary>
    Task<AgentConfigDto?> GetByCodeAsync(string agentCode, CancellationToken ct = default);
}

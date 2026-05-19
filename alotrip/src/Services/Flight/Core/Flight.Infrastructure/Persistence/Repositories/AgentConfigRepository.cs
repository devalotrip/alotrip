using System.Data;
using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Flight.Application.Features.Admin.Agents;
using Flight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Flight.Infrastructure.Persistence.Repositories;

/// <summary>
/// Loads agent config from PostgreSQL using raw ADO.NET (no EF entity needed).
///
/// Queries:
///   1. agents — galileo_active, lcc_vn_active_domestic, lcc_vn_active_global
///   2. agent_partners JOIN partners — per-partner active flags
///   3. agent_airline_ignores — per-airline suppression rules
/// </summary>
public sealed class AgentConfigRepository(ApplicationDbContext db) : IAgentConfigRepository
{
    public async Task<AgentConfigDto?> GetByCodeAsync(string agentCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentCode)) return null;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        // ── 1. Load agent row ────────────────────────────────────────────────
        int agentId;
        bool galileoActive, lccDomestic, lccGlobal;

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT id, galileo_active, lcc_vn_active_domestic, lcc_vn_active_global
                FROM   agents
                WHERE  agent_code = '{EscapeSql(agentCode)}'
                  AND  active = TRUE
                LIMIT 1
                """;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            agentId       = reader.GetInt32(0);
            galileoActive = reader.GetBoolean(1);
            lccDomestic   = reader.GetBoolean(2);
            lccGlobal     = reader.GetBoolean(3);
        }

        // ── 2. Load partner flags + route rules ────────────────────────────────
        bool kiwiActive = false, pkfareActive = false, maybayActive = false, datacomActive = false;
        var routeRules = new List<PartnerRouteRuleDto>();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT lower(p.name), ap.ignored_mode, ap.list_start_point
                FROM   agent_partners ap
                JOIN   partners       p  ON p.id = ap.partner_id
                WHERE  ap.agent_id = {agentId}
                  AND  ap.active   = TRUE
                  AND  p.active    = TRUE
                """;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var name = reader.GetString(0).Trim();
                if (name.Contains("kiwi"))    kiwiActive    = true;
                if (name.Contains("pkfare"))  pkfareActive  = true;
                if (name.Contains("maybay"))  maybayActive  = true;
                if (name.Contains("datacom")) datacomActive = true;

                // Load route restriction rules (IgnoredMode + ListStartPoint)
                var ignoredMode = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var listStartPoint = reader.IsDBNull(2) ? null : reader.GetString(2);

                if (!string.IsNullOrWhiteSpace(listStartPoint))
                {
                    var routes = listStartPoint
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();

                    if (routes.Count > 0)
                    {
                        routeRules.Add(new PartnerRouteRuleDto
                        {
                            PartnerName = name,
                            IgnoredMode = ignoredMode,
                            Routes = routes,
                        });
                    }
                }
            }
        }

        // ── 3. Load airline ignores ──────────────────────────────────────────
        var ignores = new List<AirlineIgnoreDto>();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT airline, filter_by_plating_carrier, filter_by_any_segment, filter_by_all_segment
                FROM   airline_ignores
                WHERE  agent_id = {agentId}
                """;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                ignores.Add(new AirlineIgnoreDto
                {
                    AirlineCode         = reader.GetString(0),
                    FilterByPlating     = reader.GetBoolean(1),
                    FilterByAnySegment  = reader.GetBoolean(2),
                    FilterByAllSegments = reader.GetBoolean(3),
                });
            }
        }

        return new AgentConfigDto
        {
            AgentCode            = agentCode,
            GalileoActive        = galileoActive,
            LccVnActiveDomestic  = lccDomestic,
            LccVnActiveGlobal    = lccGlobal,
            KiwiActive           = kiwiActive,
            PkfareActive         = pkfareActive,
            MaybayActive         = maybayActive,
            DatacomActive        = datacomActive,
            PartnerRouteRules    = routeRules.AsReadOnly(),
            AirlineIgnores       = ignores.AsReadOnly(),
        };
    }

    /// <summary>Minimal SQL injection protection for agent codes (they come from validated JWT claims).</summary>
    private static string EscapeSql(string s) => s.Replace("'", "''");
}

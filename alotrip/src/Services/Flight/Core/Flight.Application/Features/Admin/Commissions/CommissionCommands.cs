using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Commissions;

public sealed record GetCommissionsQuery(int? AgentId = null) : IRequest<IEnumerable<CommissionDto>>;
public sealed record UpsertCommissionCommand(UpsertCommissionRequest Request) : IRequest<bool>;
public sealed record DeleteCommissionCommand(int Id) : IRequest<bool>;

public sealed class GetCommissionsHandler(IAgentRepository repo) : IRequestHandler<GetCommissionsQuery, IEnumerable<CommissionDto>>
{
    public async Task<IEnumerable<CommissionDto>> Handle(GetCommissionsQuery request, CancellationToken ct)
    {
        var result = await repo.GetCommissionsAsync(request.AgentId, ct);
        return result.Select(c => new CommissionDto
        {
            Id = c.Id, AgentId = c.AgentId, AgentCode = c.AgentCode,
            AirlineGroup = c.AirlineGroup, StartRegion = c.StartRegion, EndRegion = c.EndRegion,
            Currency = c.Currency, FeeAdtOneWay = c.FeeAdtOneWay, FeeChdOneWay = c.FeeChdOneWay,
            FeeInfOneWay = c.FeeInfOneWay, FeeAdtRoundTrip = c.FeeAdtRoundTrip,
            FeeChdRoundTrip = c.FeeChdRoundTrip, FeeInfRoundTrip = c.FeeInfRoundTrip,
            FeeByPercent = c.FeeByPercent, Commission = c.Commission,
            ComByPercent = c.ComByPercent, ComPercentOnBaseFare = c.ComPercentOnBaseFare
        });
    }
}

public sealed class UpsertCommissionHandler(IAgentRepository repo) : IRequestHandler<UpsertCommissionCommand, bool>
{
    public async Task<bool> Handle(UpsertCommissionCommand request, CancellationToken ct)
    {
        var domainReq = new Domain.Repositories.UpsertCommissionRequest
        {
            AgentId = request.Request.AgentId,
            AirlineGroup = request.Request.AirlineGroup,
            StartRegion = request.Request.StartRegion,
            EndRegion = request.Request.EndRegion,
            Currency = request.Request.Currency,
            FeeAdtOneWay = request.Request.FeeAdtOneWay,
            FeeChdOneWay = request.Request.FeeChdOneWay,
            FeeInfOneWay = request.Request.FeeInfOneWay,
            FeeAdtRoundTrip = request.Request.FeeAdtRoundTrip,
            FeeChdRoundTrip = request.Request.FeeChdRoundTrip,
            FeeInfRoundTrip = request.Request.FeeInfRoundTrip,
            FeeByPercent = request.Request.FeeByPercent,
            Commission = request.Request.Commission,
            ComByPercent = request.Request.ComByPercent,
            ComPercentOnBaseFare = request.Request.ComPercentOnBaseFare
        };
        return await repo.UpsertCommissionAsync(domainReq, ct);
    }
}

public sealed class DeleteCommissionHandler(IAgentRepository repo) : IRequestHandler<DeleteCommissionCommand, bool>
{
    public async Task<bool> Handle(DeleteCommissionCommand request, CancellationToken ct)
        => await repo.DeleteCommissionAsync(request.Id, ct);
}

public sealed class UpsertCommissionRequest
{
    public int AgentId { get; set; }
    public string AirlineGroup { get; set; } = default!;
    public string StartRegion { get; set; } = default!;
    public string EndRegion { get; set; } = default!;
    public string Currency { get; set; } = "VND";
    public decimal FeeAdtOneWay { get; set; }
    public decimal FeeChdOneWay { get; set; }
    public decimal FeeInfOneWay { get; set; }
    public decimal FeeAdtRoundTrip { get; set; }
    public decimal FeeChdRoundTrip { get; set; }
    public decimal FeeInfRoundTrip { get; set; }
    public decimal FeeByPercent { get; set; }
    public decimal Commission { get; set; }
    public bool ComByPercent { get; set; }
    public bool ComPercentOnBaseFare { get; set; }
}

public sealed class CommissionDto
{
    public int Id { get; init; }
    public int AgentId { get; init; }
    public string AgentCode { get; init; } = default!;
    public string AirlineGroup { get; init; } = default!;
    public string StartRegion { get; init; } = default!;
    public string EndRegion { get; init; } = default!;
    public string Currency { get; init; } = default!;
    public decimal FeeAdtOneWay { get; init; }
    public decimal FeeChdOneWay { get; init; }
    public decimal FeeInfOneWay { get; init; }
    public decimal FeeAdtRoundTrip { get; init; }
    public decimal FeeChdRoundTrip { get; init; }
    public decimal FeeInfRoundTrip { get; init; }
    public decimal FeeByPercent { get; init; }
    public decimal Commission { get; init; }
    public bool ComByPercent { get; init; }
    public bool ComPercentOnBaseFare { get; init; }
}
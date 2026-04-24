using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Pccs;

public sealed record GetPccsQuery(int Page = 1, int PageSize = 20) : IRequest<PccsResponse>;
public sealed record GetPccByIdQuery(string Pcc) : IRequest<PccDto?>;
public sealed record CreatePccCommand(string Pcc) : IRequest<bool>;
public sealed record DeletePccCommand(string Pcc) : IRequest<bool>;

public sealed record PccsResponse(int Total, int Page, int PageSize, IEnumerable<PccDto> Data);

public sealed class GetPccsHandler(IAgentRepository repo) : IRequestHandler<GetPccsQuery, PccsResponse>
{
    public async Task<PccsResponse> Handle(GetPccsQuery request, CancellationToken ct)
    {
        var query = repo.GetPccsQuery().Where(p => p.Active);
        var total = await Task.Run(() => query.Count(), ct);
        var list = query.OrderBy(p => p.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new PccsResponse(total, request.Page, request.PageSize, list.Select(e => new PccDto { Id = e.Id, Active = e.Active }));
    }
}

public sealed class GetPccByIdHandler(IAgentRepository repo) : IRequestHandler<GetPccByIdQuery, PccDto?>
{
    public async Task<PccDto?> Handle(GetPccByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetPccByIdAsync(request.Pcc, ct);
        return entity is null ? null : new PccDto { Id = entity.Id, Active = entity.Active };
    }
}

public sealed class CreatePccHandler(IAgentRepository repo) : IRequestHandler<CreatePccCommand, bool>
{
    public async Task<bool> Handle(CreatePccCommand request, CancellationToken ct)
    {
        var exists = await repo.GetPccByIdAsync(request.Pcc, ct);
        if (exists != null) return false;
        var entity = PccEntity.Create(request.Pcc);
        await repo.AddPccAsync(entity, ct);
        return true;
    }
}

public sealed class DeletePccHandler(IAgentRepository repo) : IRequestHandler<DeletePccCommand, bool>
{
    public async Task<bool> Handle(DeletePccCommand request, CancellationToken ct)
        => await repo.DeletePccAsync(request.Pcc, ct);
}
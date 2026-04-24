using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.Partners;

public sealed record GetPartnersQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<PartnerDto>>;
public sealed record GetPartnerByIdQuery(int Id) : IRequest<PartnerDto?>;
public sealed record CreatePartnerCommand(string Name) : IRequest<PartnerDto>;
public sealed record UpdatePartnerCommand(int Id, string Name) : IRequest<PartnerDto?>;
public sealed record DeletePartnerCommand(int Id) : IRequest<bool>;

public sealed class GetPartnersHandler(IAgentRepository repo) : IRequestHandler<GetPartnersQuery, PaginatedResult<PartnerDto>>
{
    public async Task<PaginatedResult<PartnerDto>> Handle(GetPartnersQuery request, CancellationToken ct)
    {
        var query = repo.GetPartnersQuery();
        var total = query.Count();
        var list = query.OrderBy(p => p.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new PartnerDto { Id = p.Id, Name = p.Name, Active = p.Active }).ToList();
        return new PaginatedResult<PartnerDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetPartnerByIdHandler(IAgentRepository repo) : IRequestHandler<GetPartnerByIdQuery, PartnerDto?>
{
    public async Task<PartnerDto?> Handle(GetPartnerByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetPartnerByIdAsync(request.Id, ct);
        return entity is null ? null : new PartnerDto { Id = entity.Id, Name = entity.Name, Active = entity.Active };
    }
}

public sealed class CreatePartnerHandler(IAgentRepository repo) : IRequestHandler<CreatePartnerCommand, PartnerDto>
{
    public async Task<PartnerDto> Handle(CreatePartnerCommand request, CancellationToken ct)
    {
        var entity = PartnerEntity.Create(request.Name);
        await repo.AddPartnerAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new PartnerDto { Id = entity.Id, Name = entity.Name, Active = entity.Active };
    }
}

public sealed class UpdatePartnerHandler(IAgentRepository repo) : IRequestHandler<UpdatePartnerCommand, PartnerDto?>
{
    public async Task<PartnerDto?> Handle(UpdatePartnerCommand request, CancellationToken ct)
    {
        var entity = await repo.GetPartnerByIdAsync(request.Id, ct);
        if (entity is null) return null;
        entity.Name = request.Name;
        await repo.SaveChangesAsync(ct);
        return new PartnerDto { Id = entity.Id, Name = entity.Name, Active = entity.Active };
    }
}

public sealed class DeletePartnerHandler(IAgentRepository repo) : IRequestHandler<DeletePartnerCommand, bool>
{
    public async Task<bool> Handle(DeletePartnerCommand request, CancellationToken ct)
    {
        var entity = await repo.GetPartnerByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.Active = false;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}
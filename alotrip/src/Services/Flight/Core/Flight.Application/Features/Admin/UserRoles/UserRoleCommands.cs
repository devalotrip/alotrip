using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.UserRoles;

public sealed record GetUserRolesQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<UserRoleDto>>;
public sealed record GetUserRoleByIdQuery(int Id) : IRequest<UserRoleDto?>;
public sealed record CreateUserRoleCommand(string Name, string? Description) : IRequest<UserRoleDto>;
public sealed record DeleteUserRoleCommand(int Id) : IRequest<bool>;

public sealed class GetUserRolesHandler(IUserRepository repo) : IRequestHandler<GetUserRolesQuery, PaginatedResult<UserRoleDto>>
{
    public async Task<PaginatedResult<UserRoleDto>> Handle(GetUserRolesQuery request, CancellationToken ct)
    {
        var query = repo.GetUserRolesQuery();
        var total = query.Count();
        var list = query.OrderBy(u => u.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(u => new UserRoleDto { Id = u.Id, Name = u.Name, Description = u.Description }).ToList();
        return new PaginatedResult<UserRoleDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetUserRoleByIdHandler(IUserRepository repo) : IRequestHandler<GetUserRoleByIdQuery, UserRoleDto?>
{
    public async Task<UserRoleDto?> Handle(GetUserRoleByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetUserRoleByIdAsync(request.Id, ct);
        return entity is null ? null : new UserRoleDto { Id = entity.Id, Name = entity.Name, Description = entity.Description };
    }
}

public sealed class CreateUserRoleHandler(IUserRepository repo) : IRequestHandler<CreateUserRoleCommand, UserRoleDto>
{
    public async Task<UserRoleDto> Handle(CreateUserRoleCommand request, CancellationToken ct)
    {
        var entity = UserRoleEntity.Create(request.Name, request.Description);
        await repo.AddUserRoleAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new UserRoleDto { Id = entity.Id, Name = entity.Name, Description = entity.Description };
    }
}

public sealed class DeleteUserRoleHandler(IUserRepository repo) : IRequestHandler<DeleteUserRoleCommand, bool>
{
    public async Task<bool> Handle(DeleteUserRoleCommand request, CancellationToken ct)
    {
        var entity = await repo.GetUserRoleByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.UserAccounts;

public sealed record GetUsersQuery(int Page = 1, int PageSize = 20, string? Email = null, bool? Active = null) : IRequest<PaginatedResult<UserAccountDto>>;
public sealed record GetUserByIdQuery(int Id) : IRequest<UserAccountDto?>;
public sealed record CreateUserCommand(int UserRoleId, string Email, string Password, string? Phone, string? FullName, bool? Gender, string? Address, bool Active) : IRequest<UserAccountDto>;
public sealed record UpdateUserCommand(int Id, int UserRoleId, string? Phone, string? FullName, bool? Gender, string? Address, bool Active, string? OTP) : IRequest<UserAccountDto?>;
public sealed record DeleteUserCommand(int Id) : IRequest<bool>;
public sealed record ChangeUserPasswordCommand(int Id, string NewPassword) : IRequest<bool>;

public sealed class GetUsersHandler(IUserRepository repo) : IRequestHandler<GetUsersQuery, PaginatedResult<UserAccountDto>>
{
    public async Task<PaginatedResult<UserAccountDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var query = repo.GetUserAccountsQuery();
        query = query.Where(u => u.Visible && u.UserRoleId != 1);
        if (!string.IsNullOrEmpty(request.Email)) query = query.Where(u => u.Email.Contains(request.Email));
        if (request.Active.HasValue) query = query.Where(u => u.Active == request.Active);
        var total = query.Count();
        var list = query.OrderByDescending(u => u.CreateDate).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(u => new UserAccountDto { Id = u.Id, UserRoleId = u.UserRoleId, Email = u.Email, Phone = u.Phone, FullName = u.FullName, Gender = u.Gender, Address = u.Address, Avatar = u.Avatar, CreateDate = u.CreateDate, LastLoginDate = u.LastLoginDate, IPLastLogin = u.IPLastLogin, Active = u.Active, Visible = u.Visible }).ToList();
        return new PaginatedResult<UserAccountDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetUserByIdHandler(IUserRepository repo) : IRequestHandler<GetUserByIdQuery, UserAccountDto?>
{
    public async Task<UserAccountDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetUserAccountByIdAsync(request.Id, ct);
        return entity is null ? null : new UserAccountDto { Id = entity.Id, UserRoleId = entity.UserRoleId, Email = entity.Email, Phone = entity.Phone, FullName = entity.FullName, Gender = entity.Gender, Address = entity.Address, Avatar = entity.Avatar, CreateDate = entity.CreateDate, LastLoginDate = entity.LastLoginDate, IPLastLogin = entity.IPLastLogin, Active = entity.Active, Visible = entity.Visible };
    }
}

public sealed class CreateUserHandler(IUserRepository repo) : IRequestHandler<CreateUserCommand, UserAccountDto>
{
    public async Task<UserAccountDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var exists = await repo.UserAccountExistsAsync(request.Email, ct);
        if (exists) throw new InvalidOperationException("Email already exists");
        var entity = UserAccountEntity.Create(request.UserRoleId, request.Email, request.Password, request.Phone, request.FullName, request.Gender, request.Address);
        entity.Active = request.Active;
        await repo.AddUserAccountAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new UserAccountDto { Id = entity.Id, UserRoleId = entity.UserRoleId, Email = entity.Email, Phone = entity.Phone, FullName = entity.FullName, Gender = entity.Gender, Address = entity.Address, Avatar = entity.Avatar, CreateDate = entity.CreateDate, LastLoginDate = entity.LastLoginDate, IPLastLogin = entity.IPLastLogin, Active = entity.Active, Visible = entity.Visible };
    }
}

public sealed class UpdateUserHandler(IUserRepository repo) : IRequestHandler<UpdateUserCommand, UserAccountDto?>
{
    public async Task<UserAccountDto?> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var entity = await repo.GetUserAccountByIdAsync(request.Id, ct);
        if (entity is null) return null;
        entity.UserRoleId = request.UserRoleId;
        entity.Phone = request.Phone;
        entity.FullName = request.FullName;
        entity.Gender = request.Gender;
        entity.Address = request.Address;
        entity.Active = request.Active;
        entity.OTP = request.OTP;
        await repo.SaveChangesAsync(ct);
        return new UserAccountDto { Id = entity.Id, UserRoleId = entity.UserRoleId, Email = entity.Email, Phone = entity.Phone, FullName = entity.FullName, Gender = entity.Gender, Address = entity.Address, Avatar = entity.Avatar, CreateDate = entity.CreateDate, LastLoginDate = entity.LastLoginDate, IPLastLogin = entity.IPLastLogin, Active = entity.Active, Visible = entity.Visible };
    }
}

public sealed class DeleteUserHandler(IUserRepository repo) : IRequestHandler<DeleteUserCommand, bool>
{
    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var entity = await repo.GetUserAccountByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.Visible = false;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>
/// Changes a user's password. Matches old UserAccountAPIDB.ChangePassword().
/// </summary>
public sealed class ChangeUserPasswordHandler(IUserRepository repo) : IRequestHandler<ChangeUserPasswordCommand, bool>
{
    public async Task<bool> Handle(ChangeUserPasswordCommand request, CancellationToken ct)
    {
        var entity = await repo.GetUserAccountByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.Password = request.NewPassword;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}
using Flight.Domain.Aggregates.Booking;

namespace Flight.Domain.Repositories;

public interface IUserRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);

    // UserAccount operations
    IQueryable<UserAccountEntity> GetUserAccountsQuery();
    Task<UserAccountEntity?> GetUserAccountByIdAsync(int id, CancellationToken ct = default);
    Task AddUserAccountAsync(UserAccountEntity entity, CancellationToken ct = default);
    Task<bool> UserAccountExistsAsync(string email, CancellationToken ct = default);

    // UserRole operations
    IQueryable<UserRoleEntity> GetUserRolesQuery();
    Task<UserRoleEntity?> GetUserRoleByIdAsync(int id, CancellationToken ct = default);
    Task AddUserRoleAsync(UserRoleEntity entity, CancellationToken ct = default);
}

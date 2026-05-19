using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flight.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so that <c>dotnet ef migrations add</c> can create the DbContext
/// without needing the full DI container to be running.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=flight_api;Username=postgres;Password=123456",
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(
            options,
            new Interceptors.AuditableEntityInterceptor(),
            new Interceptors.DispatchDomainEventsInterceptor(
                null!,
                NullLogger<Interceptors.DispatchDomainEventsInterceptor>.Instance));
    }
}

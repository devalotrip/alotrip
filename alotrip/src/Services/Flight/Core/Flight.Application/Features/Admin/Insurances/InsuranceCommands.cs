using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.Insurances;

public sealed record GetInsurancesQuery(int Page = 1, int PageSize = 20, Guid? BookingId = null) : IRequest<PaginatedResult<InsuranceDto>>;
public sealed record GetInsuranceByIdQuery(int Id) : IRequest<InsuranceDto?>;
public sealed record CreateInsuranceCommand(Guid BookingId, Guid PassengerId, string Provider, string PolicyNumber, string CoverageType, decimal Premium, string Currency, DateTime StartDate, DateTime EndDate, string? BeneficiaryName, string? BeneficiaryPhone) : IRequest<InsuranceDto>;
public sealed record DeleteInsuranceCommand(int Id) : IRequest<bool>;

public sealed class GetInsurancesHandler(IBookingAddonRepository repo) : IRequestHandler<GetInsurancesQuery, PaginatedResult<InsuranceDto>>
{
    public async Task<PaginatedResult<InsuranceDto>> Handle(GetInsurancesQuery request, CancellationToken ct)
    {
        var query = repo.GetInsurancesQuery();
        if (request.BookingId.HasValue) query = query.Where(i => i.BookingId == request.BookingId);
        var total = query.Count();
        var list = query.OrderByDescending(i => i.CreatedOnUtc).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(i => new InsuranceDto { Id = i.Id, BookingId = i.BookingId, PassengerId = i.PassengerId, Provider = i.Provider, PolicyNumber = i.PolicyNumber, CoverageType = i.CoverageType, Premium = i.Premium, Currency = i.Currency, StartDate = i.StartDate, EndDate = i.EndDate, BeneficiaryName = i.BeneficiaryName, BeneficiaryPhone = i.BeneficiaryPhone, Status = i.Status }).ToList();
        return new PaginatedResult<InsuranceDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetInsuranceByIdHandler(IBookingAddonRepository repo) : IRequestHandler<GetInsuranceByIdQuery, InsuranceDto?>
{
    public async Task<InsuranceDto?> Handle(GetInsuranceByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetInsuranceByIdAsync(request.Id, ct);
        return entity is null ? null : new InsuranceDto { Id = entity.Id, BookingId = entity.BookingId, PassengerId = entity.PassengerId, Provider = entity.Provider, PolicyNumber = entity.PolicyNumber, CoverageType = entity.CoverageType, Premium = entity.Premium, Currency = entity.Currency, StartDate = entity.StartDate, EndDate = entity.EndDate, BeneficiaryName = entity.BeneficiaryName, BeneficiaryPhone = entity.BeneficiaryPhone, Status = entity.Status };
    }
}

public sealed class CreateInsuranceHandler(IBookingAddonRepository repo) : IRequestHandler<CreateInsuranceCommand, InsuranceDto>
{
    public async Task<InsuranceDto> Handle(CreateInsuranceCommand request, CancellationToken ct)
    {
        var entity = InsuranceEntity.Create(request.BookingId, request.PassengerId, request.Provider, request.PolicyNumber, request.CoverageType, request.Premium, request.Currency, request.StartDate, request.EndDate, request.BeneficiaryName, request.BeneficiaryPhone);
        await repo.AddInsuranceAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new InsuranceDto { Id = entity.Id, BookingId = entity.BookingId, PassengerId = entity.PassengerId, Provider = entity.Provider, PolicyNumber = entity.PolicyNumber, CoverageType = entity.CoverageType, Premium = entity.Premium, Currency = entity.Currency, StartDate = entity.StartDate, EndDate = entity.EndDate, BeneficiaryName = entity.BeneficiaryName, BeneficiaryPhone = entity.BeneficiaryPhone, Status = entity.Status };
    }
}

public sealed class DeleteInsuranceHandler(IBookingAddonRepository repo) : IRequestHandler<DeleteInsuranceCommand, bool>
{
    public async Task<bool> Handle(DeleteInsuranceCommand request, CancellationToken ct)
    {
        var entity = await repo.GetInsuranceByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}
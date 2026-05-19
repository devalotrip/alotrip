using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.Invoices;

public sealed record GetInvoicesQuery(int Page = 1, int PageSize = 20, Guid? BookingId = null) : IRequest<PaginatedResult<InvoiceDto>>;
public sealed record GetInvoiceByIdQuery(int Id) : IRequest<InvoiceDto?>;
public sealed record CreateInvoiceCommand(Guid BookingId, string CompanyName, decimal TotalAmount, string Currency, string? Address, string? CityName, string? TaxCode, string? Receiver, string? ReceiverPhone, string? ReceiverEmail) : IRequest<InvoiceDto>;
public sealed record UpdateInvoiceCommand(int Id, string CompanyName, string? Address, string? CityName, string? TaxCode, string? Receiver, string? ReceiverPhone, string? ReceiverEmail, decimal TotalAmount, string Currency) : IRequest<InvoiceDto?>;
public sealed record DeleteInvoiceCommand(int Id) : IRequest<bool>;

public sealed class GetInvoicesHandler(IBookingAddonRepository repo) : IRequestHandler<GetInvoicesQuery, PaginatedResult<InvoiceDto>>
{
    public async Task<PaginatedResult<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        var query = repo.GetInvoicesQuery();
        if (request.BookingId.HasValue) query = query.Where(i => i.BookingId == request.BookingId);
        var total = query.Count();
        var list = query.OrderByDescending(i => i.InvoiceDate).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(i => new InvoiceDto { Id = i.Id, BookingId = i.BookingId, CompanyName = i.CompanyName, Address = i.Address, CityName = i.CityName, TaxCode = i.TaxCode, Receiver = i.Receiver, ReceiverPhone = i.ReceiverPhone, ReceiverEmail = i.ReceiverEmail, TotalAmount = i.TotalAmount, Currency = i.Currency, InvoiceDate = i.InvoiceDate, InvoiceNumber = i.InvoiceNumber }).ToList();
        return new PaginatedResult<InvoiceDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetInvoiceByIdHandler(IBookingAddonRepository repo) : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto?>
{
    public async Task<InvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetInvoiceByIdAsync(request.Id, ct);
        return entity is null ? null : new InvoiceDto { Id = entity.Id, BookingId = entity.BookingId, CompanyName = entity.CompanyName, Address = entity.Address, CityName = entity.CityName, TaxCode = entity.TaxCode, Receiver = entity.Receiver, ReceiverPhone = entity.ReceiverPhone, ReceiverEmail = entity.ReceiverEmail, TotalAmount = entity.TotalAmount, Currency = entity.Currency, InvoiceDate = entity.InvoiceDate, InvoiceNumber = entity.InvoiceNumber };
    }
}

public sealed class CreateInvoiceHandler(IBookingAddonRepository repo) : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        var entity = InvoiceEntity.Create(request.BookingId, request.CompanyName, request.TotalAmount, request.Currency, request.Address, request.CityName, request.TaxCode, request.Receiver, request.ReceiverPhone, request.ReceiverEmail);
        await repo.AddInvoiceAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new InvoiceDto { Id = entity.Id, BookingId = entity.BookingId, CompanyName = entity.CompanyName, Address = entity.Address, CityName = entity.CityName, TaxCode = entity.TaxCode, Receiver = entity.Receiver, ReceiverPhone = entity.ReceiverPhone, ReceiverEmail = entity.ReceiverEmail, TotalAmount = entity.TotalAmount, Currency = entity.Currency, InvoiceDate = entity.InvoiceDate, InvoiceNumber = entity.InvoiceNumber };
    }
}

public sealed class UpdateInvoiceHandler(IBookingAddonRepository repo) : IRequestHandler<UpdateInvoiceCommand, InvoiceDto?>
{
    public async Task<InvoiceDto?> Handle(UpdateInvoiceCommand request, CancellationToken ct)
    {
        var entity = await repo.GetInvoiceByIdAsync(request.Id, ct);
        if (entity is null) return null;
        entity.CompanyName = request.CompanyName;
        entity.Address = request.Address;
        entity.CityName = request.CityName;
        entity.TaxCode = request.TaxCode;
        entity.Receiver = request.Receiver;
        entity.ReceiverPhone = request.ReceiverPhone;
        entity.ReceiverEmail = request.ReceiverEmail;
        entity.TotalAmount = request.TotalAmount;
        entity.Currency = request.Currency;
        await repo.SaveChangesAsync(ct);
        return new InvoiceDto { Id = entity.Id, BookingId = entity.BookingId, CompanyName = entity.CompanyName, Address = entity.Address, CityName = entity.CityName, TaxCode = entity.TaxCode, Receiver = entity.Receiver, ReceiverPhone = entity.ReceiverPhone, ReceiverEmail = entity.ReceiverEmail, TotalAmount = entity.TotalAmount, Currency = entity.Currency, InvoiceDate = entity.InvoiceDate, InvoiceNumber = entity.InvoiceNumber };
    }
}

public sealed class DeleteInvoiceHandler(IBookingAddonRepository repo) : IRequestHandler<DeleteInvoiceCommand, bool>
{
    public async Task<bool> Handle(DeleteInvoiceCommand request, CancellationToken ct)
    {
        var entity = await repo.GetInvoiceByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.DeletedAt = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}
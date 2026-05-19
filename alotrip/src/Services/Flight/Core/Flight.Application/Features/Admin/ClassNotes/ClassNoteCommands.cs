using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Airlines;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using System.Linq;

namespace Flight.Application.Features.Admin.ClassNotes;

public sealed record GetClassNotesQuery(int Page = 1, int PageSize = 20, string? AirlineCode = null) : IRequest<PaginatedResult<ClassAndNoteDto>>;
public sealed record GetClassNoteByIdQuery(int Id) : IRequest<ClassAndNoteDto?>;
public sealed record CreateClassNoteCommand(string AirlineCode, string Class, string ShowClass, bool NonRefundable, string? StartAirportCode, string? EndAirportCode) : IRequest<ClassAndNoteDto>;
public sealed record DeleteClassNoteCommand(int Id) : IRequest<bool>;

public sealed class GetClassNotesHandler(IReferenceDataRepository repo) : IRequestHandler<GetClassNotesQuery, PaginatedResult<ClassAndNoteDto>>
{
    public async Task<PaginatedResult<ClassAndNoteDto>> Handle(GetClassNotesQuery request, CancellationToken ct)
    {
        var query = repo.GetClassNotesQuery();
        if (!string.IsNullOrEmpty(request.AirlineCode)) query = query.Where(c => c.AirlineCode == request.AirlineCode);
        var total = query.Count();
        var list = query.OrderBy(c => c.AirlineCode).ThenBy(c => c.Class).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(c => new ClassAndNoteDto { Id = c.Id, AirlineCode = c.AirlineCode, Class = c.Class, ShowClass = c.ShowClass, NonRefundable = c.NonRefundable, Visible = c.Visible, StartAirportCode = c.StartAirportCode, EndAirportCode = c.EndAirportCode }).ToList();
        return new PaginatedResult<ClassAndNoteDto>(total, request.Page, request.PageSize, list);
    }
}

public sealed class GetClassNoteByIdHandler(IReferenceDataRepository repo) : IRequestHandler<GetClassNoteByIdQuery, ClassAndNoteDto?>
{
    public async Task<ClassAndNoteDto?> Handle(GetClassNoteByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetClassNoteByIdAsync(request.Id, ct);
        return entity is null ? null : new ClassAndNoteDto { Id = entity.Id, AirlineCode = entity.AirlineCode, Class = entity.Class, ShowClass = entity.ShowClass, NonRefundable = entity.NonRefundable, Visible = entity.Visible, StartAirportCode = entity.StartAirportCode, EndAirportCode = entity.EndAirportCode };
    }
}

public sealed class CreateClassNoteHandler(IReferenceDataRepository repo) : IRequestHandler<CreateClassNoteCommand, ClassAndNoteDto>
{
    public async Task<ClassAndNoteDto> Handle(CreateClassNoteCommand request, CancellationToken ct)
    {
        var entity = ClassAndNoteEntity.Create(request.AirlineCode, request.Class, request.ShowClass, request.NonRefundable, request.StartAirportCode, request.EndAirportCode);
        await repo.AddClassNoteAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new ClassAndNoteDto { Id = entity.Id, AirlineCode = entity.AirlineCode, Class = entity.Class, ShowClass = entity.ShowClass, NonRefundable = entity.NonRefundable, Visible = entity.Visible, StartAirportCode = entity.StartAirportCode, EndAirportCode = entity.EndAirportCode };
    }
}

public sealed class DeleteClassNoteHandler(IReferenceDataRepository repo) : IRequestHandler<DeleteClassNoteCommand, bool>
{
    public async Task<bool> Handle(DeleteClassNoteCommand request, CancellationToken ct)
    {
        var entity = await repo.GetClassNoteByIdAsync(request.Id, ct);
        if (entity is null) return false;
        entity.Visible = false;
        await repo.SaveChangesAsync(ct);
        return true;
    }
}
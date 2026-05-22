using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.ClassNotes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.ClassNotes;

public sealed class ClassNoteAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/class-notes")
            .WithTags("Admin - Class And Notes");
		//.RequireAuthorization("AdminOnly");

		group.MapGet("/", GetClassAndNotes);
        group.MapGet("/{id:int}", GetClassAndNoteById);
        group.MapPost("/", CreateClassAndNote);
        group.MapDelete("/{id:int}", DeleteClassAndNote);
    }

    private static async Task<IResult> GetClassAndNotes(ISender sender, string? airlineCode = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetClassNotesQuery(page, pageSize, airlineCode), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetClassAndNoteById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetClassNoteByIdQuery(id), ct);
        return item is null ? Results.NotFound() : Results.Ok(new ApiResponse<ClassAndNoteDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateClassAndNote(CreateClassAndNoteRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateClassNoteCommand(req.AirlineCode, req.Class, req.ShowClass ?? string.Empty, req.NonRefundable, req.StartAirportCode, req.EndAirportCode), ct);
        return Results.Created($"/api/admin/class-notes/{created.Id}", new ApiResponse<ClassAndNoteDto>(true, "Created", created));
    }

    private static async Task<IResult> DeleteClassAndNote(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteClassNoteCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}
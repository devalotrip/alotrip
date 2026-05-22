using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Geo;
using MediatR;
using Microsoft.AspNetCore.Http;
using Shared.BuildingBlocks.Authentication.Extensions;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin;

public sealed class GeoAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/geo")
            .WithTags("Admin Geo");
		//.RequireAuthorization("AdminOnly");

		// Continents
		group.MapGet("/continents", GetContinents);
        group.MapGet("/continents/{code}", GetContinentByCode);
        group.MapPost("/continents", CreateContinent);
        group.MapPut("/continents/{code}", UpdateContinent);
        group.MapDelete("/continents/{code}", DeleteContinent);

        // Countries
        group.MapGet("/countries", GetCountries);
        group.MapGet("/countries/{code}", GetCountryByCode);
        group.MapPost("/countries", CreateCountry);
        group.MapPut("/countries/{code}", UpdateCountry);
        group.MapDelete("/countries/{code}", DeleteCountry);

        // Cities
        group.MapGet("/cities", GetCities);
        group.MapGet("/cities/{code}", GetCityByCode);
        group.MapPost("/cities", CreateCity);
        group.MapPut("/cities/{code}", UpdateCity);
        group.MapDelete("/cities/{code}", DeleteCity);

        // Airports
        group.MapGet("/airports", GetAirports);
        group.MapGet("/airports/{code}", GetAirportByCode);
        group.MapPost("/airports", CreateAirport);
        group.MapPut("/airports/{code}", UpdateAirport);
        group.MapDelete("/airports/{code}", DeleteAirport);
    }

    // === Continents ===

    private static async Task<IResult> GetContinents(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetContinentsQuery(), ct);
        return Results.Ok(new ApiResponse<IEnumerable<GeoContinentDto>>(true, "OK", result));
    }

    private static async Task<IResult> GetContinentByCode(string code, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetContinentByCodeQuery(code), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Continent not found", null))
            : Results.Ok(new ApiResponse<GeoContinentDto>(true, "OK", result));
    }

    private static async Task<IResult> CreateContinent(GeoContinentDto dto, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var result = await sender.Send(new CreateContinentCommand(dto, userName), ct);
        return Results.Created($"/api/admin/geo/continents/{result.Code}", new ApiResponse<GeoContinentDto>(true, "Created", result));
    }

    private static async Task<IResult> UpdateContinent(string code, GeoContinentDto dto, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var result = await sender.Send(new UpdateContinentCommand(code, dto, userName), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Continent not found", null))
            : Results.Ok(new ApiResponse<GeoContinentDto>(true, "Updated", result));
    }

    private static async Task<IResult> DeleteContinent(string code, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var success = await sender.Send(new DeleteContinentCommand(code, userName), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Continent not found", null));
    }

    // === Countries ===

    private static async Task<IResult> GetCountries(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCountriesQuery(), ct);
        return Results.Ok(new ApiResponse<IEnumerable<GeoCountryDto>>(true, "OK", result));
    }

    private static async Task<IResult> GetCountryByCode(string code, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCountryByCodeQuery(code), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Country not found", null))
            : Results.Ok(new ApiResponse<GeoCountryDto>(true, "OK", result));
    }

    private static async Task<IResult> CreateCountry(GeoCountryDto dto, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var result = await sender.Send(new CreateCountryCommand(dto, userName), ct);
        return Results.Created($"/api/admin/geo/countries/{result.Code}", new ApiResponse<GeoCountryDto>(true, "Created", result));
    }

    private static async Task<IResult> UpdateCountry(string code, GeoCountryDto dto, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var result = await sender.Send(new UpdateCountryCommand(code, dto, userName), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Country not found", null))
            : Results.Ok(new ApiResponse<GeoCountryDto>(true, "Updated", result));
    }

    private static async Task<IResult> DeleteCountry(string code, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var success = await sender.Send(new DeleteCountryCommand(code, userName), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Country not found", null));
    }

    // === Cities ===

    private static async Task<IResult> GetCities(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCitiesQuery(), ct);
        return Results.Ok(new ApiResponse<IEnumerable<GeoCityDto>>(true, "OK", result));
    }

    private static async Task<IResult> GetCityByCode(string code, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCityByCodeQuery(code), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "City not found", null))
            : Results.Ok(new ApiResponse<GeoCityDto>(true, "OK", result));
    }

    private static async Task<IResult> CreateCity(GeoCityDto dto, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var result = await sender.Send(new CreateCityCommand(dto, userName), ct);
        return Results.Created($"/api/admin/geo/cities/{result.Code}", new ApiResponse<GeoCityDto>(true, "Created", result));
    }

    private static async Task<IResult> UpdateCity(string code, GeoCityDto dto, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var result = await sender.Send(new UpdateCityCommand(code, dto, userName), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "City not found", null))
            : Results.Ok(new ApiResponse<GeoCityDto>(true, "Updated", result));
    }

    private static async Task<IResult> DeleteCity(string code, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var success = await sender.Send(new DeleteCityCommand(code, userName), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "City not found", null));
    }

    // === Airports ===

    private static async Task<IResult> GetAirports(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAirportsQuery(), ct);
        return Results.Ok(new ApiResponse<IEnumerable<GeoAirportDto>>(true, "OK", result));
    }

    private static async Task<IResult> GetAirportByCode(string code, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAirportByCodeQuery(code), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Airport not found", null))
            : Results.Ok(new ApiResponse<GeoAirportDto>(true, "OK", result));
    }

    private static async Task<IResult> CreateAirport(GeoAirportDto dto, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var result = await sender.Send(new CreateAirportCommand(dto, userName), ct);
        return Results.Created($"/api/admin/geo/airports/{result.Code}", new ApiResponse<GeoAirportDto>(true, "Created", result));
    }

    private static async Task<IResult> UpdateAirport(string code, GeoAirportDto dto, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var result = await sender.Send(new UpdateAirportCommand(code, dto, userName), ct);
        return result is null
            ? Results.NotFound(new ApiResponse<object>(false, "Airport not found", null))
            : Results.Ok(new ApiResponse<GeoAirportDto>(true, "Updated", result));
    }

    private static async Task<IResult> DeleteAirport(string code, ISender sender, IHttpContextAccessor ctx, CancellationToken ct)
    {
        var userName = ctx.GetCurrentUser().UserName;
        var success = await sender.Send(new DeleteAirportCommand(code, userName), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Airport not found", null));
    }
}

# ── Stage 1: Restore + Build ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

# Copy Directory.Build.props and Directory.Packages.props first (for CPM cache)
COPY src/Directory.Build.props src/
COPY src/Directory.Packages.props src/

# Copy all csproj files to restore in a separate layer (better cache utilisation)
COPY src/Shared/BuildingBlocks/Shared.BuildingBlocks/Shared.BuildingBlocks.csproj \
     src/Shared/BuildingBlocks/Shared.BuildingBlocks/
COPY src/Shared/Common/Shared.Common/Shared.Common.csproj \
     src/Shared/Common/Shared.Common/
COPY src/Services/Flight/Core/Flight.Domain/Flight.Domain.csproj \
     src/Services/Flight/Core/Flight.Domain/
COPY src/Services/Flight/Core/Flight.Application/Flight.Application.csproj \
     src/Services/Flight/Core/Flight.Application/
COPY src/Services/Flight/Core/Flight.Infrastructure/Flight.Infrastructure.csproj \
     src/Services/Flight/Core/Flight.Infrastructure/
COPY src/Services/Flight/Api/Flight.Api/Flight.Api.csproj \
     src/Services/Flight/Api/Flight.Api/

# Restore NuGet packages
RUN dotnet restore src/Services/Flight/Api/Flight.Api/Flight.Api.csproj

# Copy the remaining sources and publish
COPY src/ src/

RUN dotnet publish src/Services/Flight/Api/Flight.Api/Flight.Api.csproj \
    --configuration Release \
    --output /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

# ASP.NET Core listens on 8080 by default in .NET 8 images
EXPOSE 8080

ENTRYPOINT ["dotnet", "Flight.Api.dll"]

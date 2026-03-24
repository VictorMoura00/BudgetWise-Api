FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy .csproj files first — Docker caches the restore layer until a .csproj changes
COPY src/BudgetWise.Api/BudgetWise.Api.csproj                       src/BudgetWise.Api/
COPY src/BudgetWise.Application/BudgetWise.Application.csproj       src/BudgetWise.Application/
COPY src/BudgetWise.Domain/BudgetWise.Domain.csproj                 src/BudgetWise.Domain/
COPY src/BudgetWise.Infrastructure/BudgetWise.Infrastructure.csproj src/BudgetWise.Infrastructure/
COPY tests/BudgetWise.UnitTests/BudgetWise.UnitTests.csproj         tests/BudgetWise.UnitTests/
RUN dotnet restore src/BudgetWise.Api/BudgetWise.Api.csproj
RUN dotnet restore tests/BudgetWise.UnitTests/BudgetWise.UnitTests.csproj

# Copy sources and compile
COPY src/ src/
COPY tests/BudgetWise.UnitTests/ tests/BudgetWise.UnitTests/
RUN dotnet build src/BudgetWise.Api/BudgetWise.Api.csproj \
        -c Release --no-restore
RUN dotnet build tests/BudgetWise.UnitTests/BudgetWise.UnitTests.csproj \
        -c Release --no-restore

# Unit-test gate — the image cannot be published if any unit test fails.
# Integration tests (Testcontainers) are not run here; they require a live
# Docker daemon and are executed separately in CI against the running stack.
FROM build AS test
WORKDIR /src
RUN dotnet test tests/BudgetWise.UnitTests/BudgetWise.UnitTests.csproj \
        -c Release \
        --no-build \
        --logger "console;verbosity=normal"

# Publish inherits FROM test — only reachable when all unit tests pass
FROM test AS publish
WORKDIR /src/src/BudgetWise.Api
RUN dotnet publish -c Release --no-build -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BudgetWise.Api.dll"]

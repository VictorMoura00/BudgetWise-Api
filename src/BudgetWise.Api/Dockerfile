FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os .csproj e restaura dependências (cache de layers)
COPY src/BudgetWise.Api/BudgetWise.Api.csproj src/BudgetWise.Api/
COPY src/BudgetWise.Application/BudgetWise.Application.csproj src/BudgetWise.Application/
COPY src/BudgetWise.Domain/BudgetWise.Domain.csproj src/BudgetWise.Domain/
COPY src/BudgetWise.Infrastructure/BudgetWise.Infrastructure.csproj src/BudgetWise.Infrastructure/
RUN dotnet restore src/BudgetWise.Api/BudgetWise.Api.csproj

# Copia o restante e faz o build
COPY src/ src/
WORKDIR /src/src/BudgetWise.Api
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BudgetWise.Api.dll"]
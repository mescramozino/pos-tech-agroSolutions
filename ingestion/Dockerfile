FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Ingestion.Api/Ingestion.Api.csproj Ingestion.Api/
COPY Ingestion.Application/Ingestion.Application.csproj Ingestion.Application/
COPY Ingestion.Infrastructure/Ingestion.Infrastructure.csproj Ingestion.Infrastructure/
RUN dotnet restore Ingestion.Api/Ingestion.Api.csproj

COPY . .
RUN dotnet publish Ingestion.Api/Ingestion.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ingestion.Api.dll"]

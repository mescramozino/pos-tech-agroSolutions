FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Identity.Api/Identity.Api.csproj Identity.Api/
COPY Identity.Application/Identity.Application.csproj Identity.Application/
COPY Identity.Domain/Identity.Domain.csproj Identity.Domain/
COPY Identity.Infrastructure/Identity.Infrastructure.csproj Identity.Infrastructure/
RUN dotnet restore Identity.Api/Identity.Api.csproj

COPY . .
RUN dotnet publish Identity.Api/Identity.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]

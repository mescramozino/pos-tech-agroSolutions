FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Properties.Api/Properties.Api.csproj Properties.Api/
COPY Properties.Application/Properties.Application.csproj Properties.Application/
COPY Properties.Domain/Properties.Domain.csproj Properties.Domain/
COPY Properties.Infrastructure/Properties.Infrastructure.csproj Properties.Infrastructure/
RUN dotnet restore Properties.Api/Properties.Api.csproj

COPY . .
RUN dotnet publish Properties.Api/Properties.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Properties.Api.dll"]

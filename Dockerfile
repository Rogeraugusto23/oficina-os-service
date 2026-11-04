FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/OsService.Domain/OsService.Domain.csproj src/OsService.Domain/
COPY src/OsService.Application/OsService.Application.csproj src/OsService.Application/
COPY src/OsService.Infrastructure/OsService.Infrastructure.csproj src/OsService.Infrastructure/
COPY src/OsService.API/OsService.API.csproj src/OsService.API/
RUN dotnet restore src/OsService.API/OsService.API.csproj

COPY src/ src/
RUN dotnet publish src/OsService.API/OsService.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OsService.API.dll"]

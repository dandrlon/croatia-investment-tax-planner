# Build stage: restore and publish the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached until dependencies change.
COPY global.json Directory.Build.props ./
COPY src/TaxPlanner.Core/TaxPlanner.Core.csproj src/TaxPlanner.Core/
COPY src/TaxPlanner.Infrastructure/TaxPlanner.Infrastructure.csproj src/TaxPlanner.Infrastructure/
COPY src/TaxPlanner.Api/TaxPlanner.Api.csproj src/TaxPlanner.Api/
RUN dotnet restore src/TaxPlanner.Api/TaxPlanner.Api.csproj

COPY src/ src/
RUN dotnet publish src/TaxPlanner.Api/TaxPlanner.Api.csproj -c Release -o /app --no-restore

# Runtime stage: only the ASP.NET Core runtime and the published app.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TaxPlanner.Api.dll"]

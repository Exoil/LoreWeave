# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project files and restore dependencies
COPY . .
RUN dotnet restore

# Publish the application
RUN dotnet publish -c Release -o /app/out

# Use the official .NET runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV Neo4j__ConnectionString=localhost:7687
ENV Neo4j__Password=YourStrong@Passw0rd
ENV Neo4j__UserName=neo4j
EXPOSE 8080

COPY --from=build /app/out .

# Run as the non-root user the aspnet image ships with (uid 1654) instead of root.
USER $APP_UID

ENTRYPOINT ["dotnet", "LoreWeave.Api.dll"]


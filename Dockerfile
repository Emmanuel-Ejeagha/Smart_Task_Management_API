# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY SmartTaskManagement.sln .
COPY src/SmartTaskManagement.Domain/*.csproj ./src/SmartTaskManagement.Domain/
COPY src/SmartTaskManagement.Application/*.csproj ./src/SmartTaskManagement.Application/
COPY src/SmartTaskManagement.Infrastructure/*.csproj ./src/SmartTaskManagement.Infrastructure/
COPY src/SmartTaskManagement.API/*.csproj ./src/SmartTaskManagement.API/

# Copy test projects (required for restore)
COPY tests/SmartTaskManagement.Domain.UnitTests/*.csproj ./tests/SmartTaskManagement.Domain.UnitTests/
COPY tests/SmartTaskManagement.Application.UnitTests/*.csproj ./tests/SmartTaskManagement.Application.UnitTests/
COPY tests/SmartTaskManagement.Infrastructure.IntegrationTests/*.csproj ./tests/SmartTaskManagement.Infrastructure.IntegrationTests/
COPY tests/SmartTaskManagement.API.IntegrationTests/*.csproj ./tests/SmartTaskManagement.API.IntegrationTests/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/src/SmartTaskManagement.API
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SmartTaskManagement.API.dll"]
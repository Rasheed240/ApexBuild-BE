# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ApexBuild.sln .
COPY ApexBuild.Api/ApexBuild.Api.csproj ApexBuild.Api/
COPY ApexBuild.Application/ApexBuild.Application.csproj ApexBuild.Application/
COPY ApexBuild.Domain/ApexBuild.Domain.csproj ApexBuild.Domain/
COPY ApexBuild.Infrastructure/ApexBuild.Infrastructure.csproj ApexBuild.Infrastructure/
COPY ApexBuild.Contracts/ApexBuild.Contracts.csproj ApexBuild.Contracts/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build the application
WORKDIR /src/ApexBuild.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chmod 777 /app/logs

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "ApexBuild.Api.dll"]

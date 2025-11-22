# ════════════════════════════════════════════════════════════════════════════
# HRMS Fortune 500 - Production Dockerfile
# Multi-stage build for .NET 9 API with optimized image size
# ════════════════════════════════════════════════════════════════════════════

# ───────────────────────────────────────────────────────────────────────────
# BUILD STAGE
# ───────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (cached layer)
COPY ["src/HRMS.API/HRMS.API.csproj", "HRMS.API/"]
COPY ["src/HRMS.Application/HRMS.Application.csproj", "HRMS.Application/"]
COPY ["src/HRMS.Infrastructure/HRMS.Infrastructure.csproj", "HRMS.Infrastructure/"]
COPY ["src/HRMS.Core/HRMS.Core.csproj", "HRMS.Core/"]
COPY ["src/HRMS.BackgroundJobs/HRMS.BackgroundJobs.csproj", "HRMS.BackgroundJobs/"]

RUN dotnet restore "HRMS.API/HRMS.API.csproj"

# Copy all source code
COPY src/ .

# Build and publish in Release mode
WORKDIR /src/HRMS.API
RUN dotnet build "HRMS.API.csproj" -c Release -o /app/build
RUN dotnet publish "HRMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ───────────────────────────────────────────────────────────────────────────
# RUNTIME STAGE
# ───────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Build arguments
ARG BUILD_DATE
ARG VCS_REF
LABEL org.opencontainers.image.created="${BUILD_DATE}" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.title="HRMS API" \
      org.opencontainers.image.description="Fortune 500-grade HRMS Multi-Tenant API" \
      org.opencontainers.image.vendor="HRMS Platform" \
      org.opencontainers.image.version="1.0.0"

# Create non-root user for security
RUN groupadd -r hrms && useradd -r -g hrms hrms

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create directories for logs and temp files
RUN mkdir -p /app/logs /app/temp && \
    chown -R hrms:hrms /app

# Switch to non-root user
USER hrms

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "HRMS.API.dll"]

# ════════════════════════════════════════════════════════════════════════════
# BUILD INSTRUCTIONS
# ════════════════════════════════════════════════════════════════════════════
#
# Build the image:
#   docker build -t hrms-api:latest \
#     --build-arg BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ") \
#     --build-arg VCS_REF=$(git rev-parse --short HEAD) \
#     -f Dockerfile .
#
# Run locally:
#   docker run -p 8080:8080 \
#     -e ConnectionStrings__DefaultConnection="Host=localhost;Database=hrms_master;..." \
#     -e JwtSettings__Secret="your-secret-key" \
#     hrms-api:latest
#
# Push to GCR:
#   docker tag hrms-api:latest gcr.io/PROJECT_ID/hrms-api:latest
#   docker push gcr.io/PROJECT_ID/hrms-api:latest
#
# ════════════════════════════════════════════════════════════════════════════

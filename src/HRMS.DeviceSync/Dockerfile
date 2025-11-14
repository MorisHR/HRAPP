# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["HRMS.DeviceSync.csproj", "./"]
RUN dotnet restore "HRMS.DeviceSync.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "HRMS.DeviceSync.csproj" -c Release -o /app/build
RUN dotnet publish "HRMS.DeviceSync.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Install Wine for running Windows COM DLLs (zkemkeeper.dll)
# Note: This is experimental - Windows host is recommended for production
RUN apt-get update && apt-get install -y \
    wine \
    wine32 \
    wine64 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs && chmod 777 /app/logs

# Copy published app
COPY --from=build /app/publish .

# Create non-root user
RUN useradd -m -u 1000 -s /bin/bash hrmsync && \
    chown -R hrmsync:hrmsync /app
USER hrmsync

ENTRYPOINT ["dotnet", "HRMS.DeviceSync.dll"]

#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.EntityFrameworkCore, 9.0.0"
#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 9.0.0"
#load "src/HRMS.Infrastructure/Data/TenantDbContext.cs"

using Microsoft.EntityFrameworkCore;
using Npgsql;

var connectionString = "Host=localhost;Database=hrms_master;Username=postgres;Password=postgres";
var schemaName = "tenant_siraaj";

Console.WriteLine($"Applying migrations to schema: {schemaName}");

var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
optionsBuilder.UseNpgsql(connectionString)
    .ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

await using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

// Apply all pending migrations
await tenantContext.Database.MigrateAsync();

Console.WriteLine($"✓ Migrations applied successfully to {schemaName}");

# PostgreSQL 18 Setup Script for DevBrain
# Run as Administrator

# Set error action
$ErrorActionPreference = "Stop"

Write-Host "=== DevBrain PostgreSQL 18 Setup ===" -ForegroundColor Green
Write-Host ""

# Step 1: Update pg_hba.conf
Write-Host "Step 1: Updating pg_hba.conf to allow trust authentication..." -ForegroundColor Cyan
$pgHbaPath = "C:\Program Files\PostgreSQL\18\data\pg_hba.conf"

if (-not (Test-Path $pgHbaPath)) {
    Write-Host "ERROR: pg_hba.conf not found at $pgHbaPath" -ForegroundColor Red
    exit 1
}

$content = Get-Content $pgHbaPath
$modified = $content -replace 'host\s+all\s+all\s+127\.0\.0\.1/32\s+scram-sha-256', 'host    all             all             127.0.0.1/32            trust'
$modified | Set-Content $pgHbaPath
Write-Host "✓ pg_hba.conf updated" -ForegroundColor Green

# Step 2: Restart PostgreSQL service
Write-Host ""
Write-Host "Step 2: Restarting PostgreSQL service..." -ForegroundColor Cyan
try {
    Stop-Service postgresql-x64-18 -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Start-Service postgresql-x64-18
    Start-Sleep -Seconds 3
    Write-Host "✓ PostgreSQL restarted" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Could not restart PostgreSQL service" -ForegroundColor Red
    Write-Host $_.Exception.Message
    Write-Host "Try manually restarting the service in Services.msc"
    exit 1
}

# Step 3: Execute SQL setup script
Write-Host ""
Write-Host "Step 3: Creating devbrain user and database..." -ForegroundColor Cyan

$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$sqlScript = @"
-- Create devbrain user with password
CREATE USER devbrain WITH PASSWORD 'devbrain_secure_password';

-- Create database
CREATE DATABASE devbrain_local OWNER devbrain;

-- Grant connection permission
GRANT CONNECT ON DATABASE devbrain_local TO devbrain;

-- Connect to new database
\c devbrain_local

-- Create schema
CREATE SCHEMA IF NOT EXISTS public;

-- Grant schema permissions
ALTER SCHEMA public OWNER TO devbrain;
GRANT USAGE ON SCHEMA public TO devbrain;
GRANT CREATE ON SCHEMA public TO devbrain;

-- Set default privileges for new objects
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO devbrain;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO devbrain;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO devbrain;

-- Verify creation
SELECT 'User created successfully' as status;
SELECT datname FROM pg_database WHERE datname = 'devbrain_local';
"@

# Execute SQL
& $psqlPath -U postgres -h 127.0.0.1 -p 5432 -c $sqlScript

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Database and user created successfully" -ForegroundColor Green
} else {
    Write-Host "ERROR: Failed to create database/user" -ForegroundColor Red
    exit 1
}

# Step 4: Set up User Secrets for connection string
Write-Host ""
Write-Host "Step 4: Configuring connection string via User Secrets..." -ForegroundColor Cyan

cd c:\dev\devbrain-trainer
$connectionString = "Host=localhost;Port=5432;Database=devbrain_local;Username=devbrain;Password=devbrain_secure_password"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ User Secrets configured" -ForegroundColor Green
} else {
    Write-Host "WARNING: Could not set User Secrets. You can do this manually:" -ForegroundColor Yellow
    Write-Host "dotnet user-secrets set `"ConnectionStrings:DefaultConnection`" `"$connectionString`"" -ForegroundColor Gray
}

# Step 5: Summary
Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Create EF Core migration:"
Write-Host "   dotnet ef migrations add InitialCreate --project src/DevBrain.Infrastructure" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Apply migration to PostgreSQL:"
Write-Host "   dotnet ef database update --project src/DevBrain.Infrastructure" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Verify tests still pass:"
Write-Host "   dotnet test" -ForegroundColor Gray
Write-Host ""
Write-Host "Connection String (saved in User Secrets):" -ForegroundColor Cyan
Write-Host $connectionString -ForegroundColor Gray

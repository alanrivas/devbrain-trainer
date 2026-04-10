# RUN AS ADMINISTRATOR
# This script configures PostgreSQL for local development with trust authentication

Write-Host "=== Configuring PostgreSQL 18 for Development ===" -ForegroundColor Green

# Step 1: Update pg_hba.conf
Write-Host "`n[1/3] Updating pg_hba.conf..." -ForegroundColor Cyan
$pgHbaPath = "C:\Program Files\PostgreSQL\18\data\pg_hba.conf"

if (-not (Test-Path $pgHbaPath)) {
    Write-Host "ERROR: pg_hba.conf not found at $pgHbaPath" -ForegroundColor Red
    exit 1
}

# Create backup
Copy-Item $pgHbaPath "$pgHbaPath.backup" -Force
Write-Host "✓ Backed up pg_hba.conf"

# Update authentication method
$content = Get-Content $pgHbaPath
$modified = $content -replace 'host\s+all\s+all\s+127\.0\.0\.1/32\s+\w+', 'host    all             all             127.0.0.1/32            trust'
$modified | Set-Content $pgHbaPath
Write-Host "✓ Changed 127.0.0.1/32 authentication to 'trust'"

# Step 2: Restart PostgreSQL
Write-Host "`n[2/3] Restarting PostgreSQL 18..." -ForegroundColor Cyan
try {
    Restart-Service postgresql-x64-18 -Force
    Start-Sleep -Seconds 3
    Write-Host "✓ PostgreSQL restarted"
} catch {
    Write-Host "ERROR: Could not restart PostgreSQL" -ForegroundColor Red
    exit 1
}

# Step 3: Verify connection
Write-Host "`n[3/3] Verifying connection..." -ForegroundColor Cyan
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
& $psqlPath -U devbrain -h 127.0.0.1 -d devbrain_local -c "SELECT 'DevBrain connection successful!' as status;" 2>&1 | ForEach-Object { Write-Host "  $_" }

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Setup complete! PostgreSQL is ready for development." -ForegroundColor Green
} else {
    Write-Host "`n✗ Connection failed. Check PostgreSQL logs." -ForegroundColor Red
}

Write-Host "`nYou can now run:" -ForegroundColor Cyan
Write-Host "  dotnet-ef database update --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api" -ForegroundColor Gray

# PostgreSQL 18: Enable trust authentication for localhost (development only)
# Run as Administrator

$pgPath = "C:\Program Files\PostgreSQL\18"
$pgHbaPath = "$pgPath\data\pg_hba.conf"
$pgDataPath = "$pgPath\data"

Write-Host "=== PostgreSQL 18 Trust Authentication Setup ===" -ForegroundColor Green

# Check if file exists
if (-not (Test-Path $pgHbaPath)) {
    Write-Host "ERROR: pg_hba.conf not found at $pgHbaPath" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Found pg_hba.conf" -ForegroundColor Green

# Backup original
$backupPath = "$pgHbaPath.backup"
if (-not (Test-Path $backupPath)) {
    Copy-Item $pgHbaPath $backupPath
    Write-Host "✓ Backup created at $backupPath" -ForegroundColor Green
}

# Read content
$content = Get-Content $pgHbaPath -Raw

# Replace scram-sha-256 with trust for IPv4 local
$original = $content
$content = $content -replace '(^TYPE\s+DATABASE.*)', '$1'
$content = $content -replace '(127\.0\.0\.1/32\s+\w+\s+\w+\s+)scram-sha-256', '${1}trust'

# Write back
Set-Content $pgHbaPath $content
Write-Host "✓ pg_hba.conf updated: scram-sha-256 → trust (127.0.0.1)" -ForegroundColor Green

# Reload PostgreSQL config (without restart)
Write-Host "`nReloading PostgreSQL configuration..." -ForegroundColor Cyan

# Method 1: Reload via pg_ctl
$pgCtlPath = "$pgPath\bin\pg_ctl.exe"
if (Test-Path $pgCtlPath) {
    & $pgCtlPath reload -D $pgDataPath 2>&1 | Out-String | Write-Host
    Write-Host "✓ Configuration reloaded via pg_ctl" -ForegroundColor Green
} else {
    # Method 2: Restart service
    Write-Host "pg_ctl not found, restarting PostgreSQL service..." -ForegroundColor Yellow
    Restart-Service -Name "postgresql-x64-18" -Force
    Write-Host "✓ PostgreSQL 18 service restarted" -ForegroundColor Green
}

# Verify
Write-Host "`nVerifying changes..." -ForegroundColor Cyan
$configCheck = Get-Content $pgHbaPath | Select-String "127.0.0.1" | Select-Object -First 1
Write-Host "pg_hba.conf entry: $configCheck" -ForegroundColor White

Write-Host "`n✅ Trust authentication enabled for localhost!" -ForegroundColor Green
Write-Host "Now you can run: dotnet-ef database update" -ForegroundColor Cyan

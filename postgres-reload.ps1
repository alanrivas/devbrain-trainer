# Run as Administrator
# This reloads PostgreSQL configuration after pg_hba.conf changes

$pgCtlPath = "C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe"
$dataPath = "C:\Program Files\PostgreSQL\18\data"

try {
    Write-Host "Reloading PostgreSQL configuration..." -ForegroundColor Cyan
    & $pgCtlPath -D $dataPath reload | Write-Host
    Write-Host "PostgreSQL configuration reloaded successfully!" -ForegroundColor Green
} catch {
    Write-Host "Error: Could not reload PostgreSQL" -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit 1
}

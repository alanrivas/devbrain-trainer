#Requires -Version 5.1
<#
.SYNOPSIS
    Levanta el stack local completo de DevBrain Trainer.
    Guarda los PIDs en .devbrain.pids para que stop-local.ps1 los mate.
#>

$PIDS_FILE = ".devbrain.pids"
$BACKEND_LOG = "$env:TEMP\devbrain-backend.log"
$FRONTEND_LOG = "$env:TEMP\devbrain-frontend.log"

function Write-Info  { param($m) Write-Host "  $m" -ForegroundColor Cyan }
function Write-Ok    { param($m) Write-Host "  $m" -ForegroundColor Green }
function Write-Warn  { param($m) Write-Host "  $m" -ForegroundColor Yellow }
function Write-Fail  { param($m) Write-Host "  ERROR: $m" -ForegroundColor Red; exit 1 }

function Wait-ForUrl {
    param([string]$Url, [string]$Label, [int]$Retries = 20)
    for ($i = 1; $i -le $Retries; $i++) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            if ($r.StatusCode -lt 500) { return $true }
        } catch {}
        Write-Host "    esperando $Label... ($i/$Retries)" -NoNewline -ForegroundColor DarkGray
        Write-Host "`r" -NoNewline
        Start-Sleep -Seconds 1
    }
    return $false
}

Write-Host ""
Write-Host "  ╔══════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║     DevBrain Trainer — Local Stack       ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── 0. Verificar stack previo ───────────────────────────────────────────────
if (Test-Path $PIDS_FILE) {
    Write-Warn "Ya existe un stack levantado (.devbrain.pids). Ejecuta .\stop-local.ps1 primero."
    exit 1
}

# ── 1. Prerequisitos ────────────────────────────────────────────────────────
Write-Info "Verificando prerequisitos..."
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Write-Fail ".NET SDK no encontrado" }
if (-not (Get-Command node   -ErrorAction SilentlyContinue)) { Write-Fail "Node.js no encontrado" }
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { Write-Fail "Docker no encontrado" }
Write-Ok "dotnet $(dotnet --version) | node $(node --version)"

# ── 2. Docker Desktop ───────────────────────────────────────────────────────
Write-Info "Verificando Docker daemon..."
$dockerOk = (docker ps 2>&1) -notmatch "error|failed"
if (-not $dockerOk) {
    Write-Warn "Docker no corre. Iniciando Docker Desktop..."
    Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe" -ErrorAction SilentlyContinue
    for ($i = 1; $i -le 30; $i++) {
        Start-Sleep -Seconds 2
        $dockerOk = (docker ps 2>&1) -notmatch "error|failed"
        if ($dockerOk) { break }
        Write-Host "    esperando Docker... ($i/30)`r" -NoNewline -ForegroundColor DarkGray
    }
    Write-Host ""
    if (-not $dockerOk) { Write-Fail "Docker no respondió después de 60s" }
}
Write-Ok "Docker daemon activo"

# ── 3. Contenedores ─────────────────────────────────────────────────────────
Write-Info "Levantando PostgreSQL + Redis..."
docker-compose up -d 2>&1 | Where-Object { $_ -match "Started|Running|healthy|error" } | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

for ($i = 1; $i -le 20; $i++) {
    $ready = (docker exec devbrain-trainer-db-1 pg_isready -U devbrain 2>&1) -match "accepting"
    if ($ready) { break }
    Write-Host "    esperando PostgreSQL... ($i/20)`r" -NoNewline -ForegroundColor DarkGray
    Start-Sleep -Seconds 1
}
Write-Host ""
Write-Ok "PostgreSQL :5433 | Redis :6379"

# ── 4. Migraciones ──────────────────────────────────────────────────────────
Write-Info "Aplicando migraciones EF Core..."
$efOut = dotnet ef database update `
    --project src/DevBrain.Infrastructure `
    --startup-project src/DevBrain.Api 2>&1
if ($LASTEXITCODE -ne 0) { Write-Fail "Migraciones fallaron: $efOut" }
Write-Ok "Base de datos actualizada"

# ── 5. Backend ──────────────────────────────────────────────────────────────
Write-Info "Iniciando backend (ASP.NET Core)..."
$backend = Start-Process dotnet `
    -ArgumentList "run --project src/DevBrain.Api" `
    -WorkingDirectory (Get-Location) `
    -RedirectStandardOutput $BACKEND_LOG `
    -RedirectStandardError  "$BACKEND_LOG.err" `
    -NoNewWindow -PassThru

if (-not (Wait-ForUrl "http://localhost:5118/health" "backend" 20)) {
    Stop-Process -Id $backend.Id -Force -ErrorAction SilentlyContinue
    Write-Fail "Backend no respondio. Log: $BACKEND_LOG"
}
Write-Ok "Backend listo en http://localhost:5118 (PID $($backend.Id))"

# ── 6. Frontend ─────────────────────────────────────────────────────────────
Write-Info "Iniciando frontend (Next.js)..."
$frontend = Start-Process npm `
    -ArgumentList "run dev" `
    -WorkingDirectory (Join-Path (Get-Location) "frontend") `
    -RedirectStandardOutput $FRONTEND_LOG `
    -RedirectStandardError  "$FRONTEND_LOG.err" `
    -NoNewWindow -PassThru

if (-not (Wait-ForUrl "http://localhost:3000" "frontend" 20)) {
    Stop-Process -Id $backend.Id  -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $frontend.Id -Force -ErrorAction SilentlyContinue
    Write-Fail "Frontend no respondio. Log: $FRONTEND_LOG"
}
Write-Ok "Frontend listo en http://localhost:3000 (PID $($frontend.Id))"

# ── 7. Guardar PIDs ─────────────────────────────────────────────────────────
@"
BACKEND_PID=$($backend.Id)
FRONTEND_PID=$($frontend.Id)
"@ | Set-Content $PIDS_FILE -Encoding UTF8

# ── 8. Resumen ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "  ╔══════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "  ║       Stack local levantado  ✓           ║" -ForegroundColor Green
Write-Host "  ╚══════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "  Frontend   " -NoNewline; Write-Host "http://localhost:3000"         -ForegroundColor Cyan
Write-Host "  Backend    " -NoNewline; Write-Host "http://localhost:5118"         -ForegroundColor Cyan
Write-Host "  API Docs   " -NoNewline; Write-Host "http://localhost:5118/scalar/" -ForegroundColor Cyan
Write-Host "  Health     " -NoNewline; Write-Host "http://localhost:5118/health"  -ForegroundColor Cyan
Write-Host ""
Write-Host "  Credenciales: admin@devbrain.local / Admin123!" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Logs:  $BACKEND_LOG"
Write-Host "         $FRONTEND_LOG"
Write-Host ""
Write-Host "  Para detener:  " -NoNewline; Write-Host ".\stop-local.ps1" -ForegroundColor Cyan
Write-Host ""

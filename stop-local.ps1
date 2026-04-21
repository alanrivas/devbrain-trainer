#Requires -Version 5.1
<#
.SYNOPSIS
    Detiene el stack local de DevBrain Trainer (backend, frontend y Docker).
#>

$PIDS_FILE = ".devbrain.pids"

function Write-Info { param($m) Write-Host "  $m" -ForegroundColor Cyan }
function Write-Ok   { param($m) Write-Host "  $m" -ForegroundColor Green }
function Write-Warn { param($m) Write-Host "  $m" -ForegroundColor Yellow }

function Stop-Tree {
    param([int]$Pid)
    # Mata el proceso y todos sus hijos (dotnet y node lanzan subprocesos)
    taskkill /F /T /PID $Pid 2>&1 | Out-Null
}

function Kill-Port {
    param([int]$Port)
    $found = netstat -ano | Select-String ":$Port\s.*LISTENING" | ForEach-Object {
        ($_ -split '\s+')[-1]
    } | Select-Object -First 1
    if ($found) {
        taskkill /F /T /PID $found 2>&1 | Out-Null
        return $true
    }
    return $false
}

Write-Host ""
Write-Host "  ╔══════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║    DevBrain Trainer — Stopping Stack     ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── 1. Backend + Frontend ───────────────────────────────────────────────────
if (Test-Path $PIDS_FILE) {
    $pids = @{}
    Get-Content $PIDS_FILE | ForEach-Object {
        if ($_ -match '^(\w+)=(\d+)$') { $pids[$Matches[1]] = [int]$Matches[2] }
    }

    if ($pids.ContainsKey("BACKEND_PID")) {
        $id = $pids["BACKEND_PID"]
        Write-Info "Deteniendo backend (PID $id)..."
        Stop-Tree $id
        Write-Ok "Backend detenido"
    }

    if ($pids.ContainsKey("FRONTEND_PID")) {
        $id = $pids["FRONTEND_PID"]
        Write-Info "Deteniendo frontend (PID $id)..."
        Stop-Tree $id
        Write-Ok "Frontend detenido"
    }

    Remove-Item $PIDS_FILE -Force
} else {
    Write-Warn ".devbrain.pids no encontrado — buscando por puerto..."

    $killed = $false
    foreach ($port in @(5118, 3000)) {
        if (Kill-Port $port) {
            Write-Ok "Proceso en :$port detenido"
            $killed = $true
        }
    }
    if (-not $killed) { Write-Warn "No se encontraron procesos en :5118 ni :3000" }
}

# ── 2. Docker ───────────────────────────────────────────────────────────────
Write-Info "Bajando contenedores Docker..."
$dockerOk = (docker ps 2>&1) -notmatch "error|failed"
if ($dockerOk) {
    docker-compose down 2>&1 | Where-Object { $_ -match "Stopping|Stopped|Removing|Removed" } | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    Write-Ok "PostgreSQL + Redis detenidos"
} else {
    Write-Warn "Docker no está corriendo, saltando docker-compose down"
}

# ── 3. Limpiar logs ─────────────────────────────────────────────────────────
Remove-Item "$env:TEMP\devbrain-*.log*" -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "  ╔══════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "  ║       Stack local detenido  ✓            ║" -ForegroundColor Green
Write-Host "  ╚══════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

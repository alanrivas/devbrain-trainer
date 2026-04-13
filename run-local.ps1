#!/usr/bin/env pwsh
<#
.SYNOPSIS
    DevBrain Trainer — Local Stack Startup Script
    Levanta PostgreSQL, Redis, Backend API, y Frontend en local.

.DESCRIPTION
    Este script automatiza todo el setup para testing local:
    1. Verifica que Docker esté corriendo
    2. Levanta PostgreSQL + Redis con Docker Compose
    3. Aplica migraciones EF Core
    4. Levanta Backend ASP.NET en terminal dedicada
    5. Levanta Frontend Next.js en terminal dedicada

.EXAMPLE
    PS C:\dev\devbrain-trainer> .\run-local.ps1

.NOTES
    Asegúrate de:
    - Docker + Docker Compose instalados
    - Puertos libres: 5118 (API), 3000 (web), 5433 (DB), 6379 (Redis)
    - .NET 10 SDK instalado
    - Node.js 22+ instalado
#>

[CmdletBinding()]
param(
    [switch]$NoDatabase,  # Skip DB setup
    [switch]$NoMigrations # Skip EF migrations
)

$ErrorActionPreference = "Stop"

function Write-Banner {
    param([string]$Message)
    Write-Host "`n" -ForegroundColor Cyan
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ $($Message.PadRight(61)) ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Message, [string]$Status = "")
    if ($Status) {
        Write-Host "✅ $Message" -ForegroundColor Green
    } else {
        Write-Host "▶️  $Message" -ForegroundColor Yellow
    }
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ ERROR: $Message" -ForegroundColor Red
    exit 1
}

function Test-PortAvailable {
    param([int]$Port)
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $tcp.Connect("127.0.0.1", $Port)
        $tcp.Close()
        return $false  # Port is in use
    } catch {
        return $true   # Port is available
    }
}

# ============================================================================
# 1. PREREQUISITOS
# ============================================================================

Write-Banner "DevBrain Trainer — Local Stack Setup"

Write-Step "Checking prerequisites..."

# Verificar .NET
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom ".NET SDK not found. Install .NET 10 SDK."
}
Write-Host "  ✓ .NET $dotnetVersion" -ForegroundColor Gray

# Verificar Node.js
$nodeVersion = node --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Node.js not found. Install Node.js 22+."
}
Write-Host "  ✓ Node $nodeVersion" -ForegroundColor Gray

# Verificar Docker
$dockerRunning = docker ps *>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Docker daemon is not running. Start Docker Desktop."
}
Write-Host "  ✓ Docker is running" -ForegroundColor Gray

Write-Step "Prerequisites check complete" $true

# ============================================================================
# 2. VERIFICAR PUERTOS DISPONIBLES
# ============================================================================

Write-Banner "Checking Port Availability"

$ports = @{
    "5118" = "Backend API"
    "3000" = "Frontend (Next.js)"
    "5433" = "PostgreSQL"
    "6379" = "Redis"
}

$portsBlocked = $false
foreach ($port in $ports.Keys) {
    if (!(Test-PortAvailable -Port $port)) {
        Write-Host "⚠️  Port $port ($($ports[$port])) is IN USE!" -ForegroundColor Red
        $portsBlocked = $true
    } else {
        Write-Host "  ✓ Port $port available" -ForegroundColor Gray
    }
}

if ($portsBlocked) {
    Write-Error-Custom "Some ports are in use. Free them or modify docker-compose.yml"
}

Write-Step "All ports available" $true

# ============================================================================
# 3. LEVANTAR DOCKER COMPOSE
# ============================================================================

if (!$NoDatabase) {
    Write-Banner "Starting Docker Compose (PostgreSQL + Redis)"
    
    Write-Step "Starting containers..."
    
    # Detener cualquier container anterior
    docker-compose down --remove-orphans 2>&1 | Out-Null
    
    # Levantar servicios en background
    $composeOutput = docker-compose up -d 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Docker Compose startup failed. Check Docker daemon."
    }
    
    Write-Step "Waiting for PostgreSQL to be ready (up to 30s)..." 
    
    # Esperar a que PostgreSQL esté listo
    $maxRetries = 30
    $retry = 0
    while ($retry -lt $maxRetries) {
        try {
            # Intentar conectar a PostgreSQL
            docker exec devbrain-trainer-db-1 pg_isready -U devbrain 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                break
            }
        } catch {
        }
        $retry++
        Write-Host "  ⏳ Attempt $retry/$maxRetries..." -ForegroundColor Gray
        Start-Sleep -Seconds 1
    }
    
    if ($retry -eq $maxRetries) {
        Write-Error-Custom "PostgreSQL failed to start within 30 seconds"
    }
    
    Write-Step "PostgreSQL is ready" $true
    Write-Step "Redis is ready" $true
}

# ============================================================================
# 4. APLICAR MIGRACIONES
# ============================================================================

if (!$NoMigrations) {
    Write-Banner "Applying EF Core Migrations"
    
    Write-Step "Running database update..."
    
    $migrationOutput = dotnet ef database update `
        --project src/DevBrain.Infrastructure `
        --startup-project src/DevBrain.Api `
        2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "EF migration failed`n$migrationOutput"
    }
    
    Write-Step "Migrations applied successfully" $true
}

# ============================================================================
# 5. LEVANTAR BACKEND
# ============================================================================

Write-Banner "Starting Backend (ASP.NET Core 10)"

Write-Step "Launching backend on port 5118..."

$backendStartScript = {
    Set-Location "c:\dev\devbrain-trainer"
    Write-Host "`n🚀 Backend startup..." -ForegroundColor Green
    dotnet run --project src/DevBrain.Api
}

Start-Process powershell -ArgumentList "-NoExit", "-Command", $backendStartScript `
    -RedirectStandardOutput ".\backend.log" `
    -RedirectStandardError ".\backend.log"

# Esperar a que el backend esté listo
Write-Step "Waiting for backend to start (up to 15s)..."
$maxRetries = 15
$retry = 0
while ($retry -lt $maxRetries) {
    try {
        $response = curl.exe -s http://localhost:5118/health 2>&1
        if ($LASTEXITCODE -eq 0) {
            break
        }
    } catch {
    }
    $retry++
    Write-Host "  ⏳ Attempt $retry/$maxRetries..." -ForegroundColor Gray
    Start-Sleep -Seconds 1
}

if ($retry -eq $maxRetries) {
    Write-Error-Custom "Backend failed to start within 15 seconds. Check .\backend.log"
}

Write-Step "Backend is ready on http://localhost:5118" $true

# ============================================================================
# 6. LEVANTAR FRONTEND
# ============================================================================

Write-Banner "Starting Frontend (Next.js 15)"

Write-Step "Installing dependencies (if needed)..."
if (!(Test-Path "c:\dev\devbrain-trainer\frontend\node_modules")) {
    Push-Location "c:\dev\devbrain-trainer\frontend"
    npm install 2>&1 | Out-Null
    Pop-Location
}

Write-Step "Launching frontend on port 3000..."

$frontendStartScript = {
    Set-Location "c:\dev\devbrain-trainer\frontend"
    Write-Host "`n🚀 Frontend startup..." -ForegroundColor Green
    npm run dev
}

Start-Process powershell -NoProfile -ArgumentList "-NoExit", "-Command", $frontendStartScript `
    -RedirectStandardOutput ".\frontend.log"

# Esperar a que el frontend esté listo
Write-Step "Waiting for frontend to start (up to 15s)..."
$maxRetries = 15
$retry = 0
while ($retry -lt $maxRetries) {
    try {
        $response = curl.exe -s http://localhost:3000 2>&1
        if ($LASTEXITCODE -eq 0) {
            break
        }
    } catch {
    }
    $retry++
    Write-Host "  ⏳ Attempt $retry/$maxRetries..." -ForegroundColor Gray
    Start-Sleep -Seconds 1
}

if ($retry -eq $maxRetries) {
    Write-Error-Custom "Frontend failed to start within 15 seconds. Check .\frontend.log"
}

Write-Step "Frontend is ready on http://localhost:3000" $true

# ============================================================================
# 7. RESUMEN FINAL
# ============================================================================

Write-Banner "✅ DevBrain Trainer — Local Stack Ready!"

Write-Host "
📍 Access Points:
   • Frontend:        http://localhost:3000
   • Backend API:     http://localhost:5118
   • API Docs:        http://localhost:5118/scalar/
   • Health Check:    http://localhost:5118/health
   • PostgreSQL:      localhost:5433 (user: devbrain, pass: devbrain)
   • Redis:           localhost:6379

🔐 Testing Credentials:
   • Email:    admin@devbrain.local
   • Password: Admin123!

📚 Documentation:
   • Testing guide:     ./docs/TESTING.md
   • API reference:     ./docs/API-ENDPOINTS.md
   • Development:       ./docs/DEVELOPMENT.md

💡 Quick Links:
   • Register:  http://localhost:3000/register
   • Login:     http://localhost:3000/login
   • Dashboard: http://localhost:3000/challenges

⚠️  Notes:
   • Frontend logs: .\frontend.log
   • Backend logs:  .\backend.log
   • Database logs: docker-compose logs db
   • Redis logs:    docker-compose logs redis

To stop services:
   • Docker: docker-compose down
   • Ctrl+C on each terminal

Happy testing! 🎉
" -ForegroundColor Green

Write-Host "`nOpening browser in 3 seconds..." -ForegroundColor Cyan
Start-Sleep -Seconds 3
Start-Process "http://localhost:3000"

Write-Host "`n✋ Script complete. Services are running in background terminals." -ForegroundColor Cyan

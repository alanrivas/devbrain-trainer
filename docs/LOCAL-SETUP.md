# 🚀 Stack Local — Live & Ready

## Estado Actual (21 Abril 2026)

✅ **TODAS LAS DEPENDENCIAS CORRIENDO EN LOCAL:**

| Servicio | URL | Status | Puerto |
|----------|-----|--------|--------|
| **Backend API** | http://localhost:5118 | ✅ Running | 5118 |
| **Frontend Web** | http://localhost:3000 | ✅ Running | 3000 |
| **PostgreSQL** | localhost:5433 | ✅ Running (Docker) | 5433 |
| **Redis** | localhost:6379 | ✅ Running (Docker) | 6379 |
| **API Docs** | http://localhost:5118/scalar/ | ✅ Available | — |
| **Health Check** | http://localhost:5118/health | ✅ OK | — |

---

## ⚡ Levantar / Bajar el Stack

```powershell
# Levantar todo (Docker + Backend + Frontend)
.\start-local.ps1

# Bajar todo
.\stop-local.ps1
```

Los scripts detectan Docker Desktop automáticamente, aplican migraciones EF y guardan los PIDs en `.devbrain.pids`.

---

## 🎯 Quick Start — Testing

### 1. En el navegador (http://localhost:3000)

#### Opción A: Registrar nuevo usuario

1. Click **"Sign Up"** (esquina superior derecha)
2. Completa:
   - Display Name: `Test Dev`
   - Email: `dev@example.com`
   - Password: `MyPass123!`
   - Confirm: `MyPass123!`
3. Click **"Sign Up"**
4. ✅ Serás redirigido a http://localhost:3000/challenges automáticamente

#### Opción B: Login con usuario pre-seeded

1. Click **"Sign In"**
2. Credenciales (si la BD fue inicializada):
   - **Email:** `admin@devbrain.local`
   - **Password:** `Admin123!`
3. Click **"Sign In"**
4. ✅ Acceso a challenges

### 2. Testing challenges

```
1. En http://localhost:3000/challenges
2. Verás una lista de desafíos (seeded en PostgreSQL)
3. Cada challenge muestra:
   - Título, dificultad (Easy/Medium/Hard) y categoría
   - Botón "Attempt →" → abre el challenge con timer y AttemptForm
4. Atajos de teclado en el challenge:
   - Ctrl+Enter — enviar respuesta
   - ArrowLeft / ArrowRight — navegar entre challenges
   - Escape — volver al listado
   - R — reiniciar formulario (cuando hay resultado)
```

### 3. View Data (Backend)

```bash
# Ver todos los usuarios (sin auth)  
curl http://localhost:5118/api/v1/auth/register -X POST \
  -H "Content-Type: application/json" \
  -d '{"email":"test@ex.com","password":"Test123!","displayName":"New"}' | jq .

# Login para obtener JWT
JWT=$(curl -s -X POST http://localhost:5118/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@devbrain.local","password":"Admin123!"}' | jq -r '.token')

# Ver challenges
curl -H "Authorization: Bearer $JWT" \
  http://localhost:5118/api/v1/challenges | jq .

# Ver stats del usuario
curl -H "Authorization: Bearer $JWT" \
  http://localhost:5118/api/v1/users/me/stats | jq .
```

---

## 📊 Datos de Testing

### Pre-seeded Users (en BD)

| Email | Password | Status |
|-------|----------|--------|
| `admin@devbrain.local` | `Admin123!` | ✅ Ready (si seed corrió) |
| `testuser@devbrain.local` | `TestUser123!` | ✅ Ready (si seed corrió) |
| `pro@devbrain.local` | `Pro123!` | ✅ Ready (si seed corrió) |

*Nota: Si la BD fue inicializada con seed. Si no, simplemente regístrate en la app (opción más simple).*

### Pre-seeded Challenges (en BD)

**10 challenges** con categorías:
- SQL Injection (3 challenges: Easy, Medium, Hard)
- C# Async (3 challenges: Easy, Medium, Hard)
- Docker (2 challenges: Medium, Hard)
- Architecture (2 challenges: Medium, Hard)

---

## 🔧 Configuración Actual

### Backend (`src/DevBrain.Api/`)

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=devbrain;Username=devbrain;Password=devbrain;SSL Mode=Disable"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

- ✅ Conecta a PostgreSQL en `localhost:5433`
- ⚠️ Redis no disponible → usando NoOpStreakService (streaks no persistidos en local)
- ✅ JWT auth activo
- ✅ Serilog logging estructurado a console + file (`logs/devbrain-*.txt`)

### Frontend (`frontend/`)

```bash
# .env.local
NEXT_PUBLIC_API_URL=http://localhost:5118/api/v1
```

- ✅ Next.js 15 en puerto 3000
- ✅ Conecta a backend en `localhost:5118`
- ✅ Auth context global con JWT management
- ✅ Tailwind CSS styling

### Docker Compose (`docker-compose.yml`)

```yaml
services:
  db:       # PostgreSQL 17 en puerto 5433
  redis:    # Redis 7 en puerto 6379
```

- ✅ Auto-health checks
- ✅ Volúmenes persistentes para PostgreSQL

---

## 📚 Documentation

| Doc | Propósito |
|-----|-----------|
| [`docs/TESTING.md`](./docs/TESTING.md) | Guía completa de testing (flujos happy path, API, cURL, troubleshooting) |
| [`docs/TEST-USERS.md`](./docs/TEST-USERS.md) | Usuarios precreados, credenciales, test scenarios completos |
| [`docs/API-ENDPOINTS.md`](./docs/API-ENDPOINTS.md) | Referencia de endpoints (métodos, auth, ejemplos) |
| [`postman/devbrain-trainer.localhost.postman_collection.json`](./postman/devbrain-trainer.localhost.postman_collection.json) | Colección Postman con todas las requests pre-configuradas |

---

## 🧪 Tests Reportados

**Status:** ✅ All 399 tests passing

```
Domain.Tests:           ✅  69/69
Infrastructure.Tests:   ✅  71/71
Api.Tests:              ✅ 104/104
Integration.Tests:      ✅  10/10
─────────────────────────
Backend Total:          ✅ 254/254

Frontend.Tests:         ✅ 145/145
─────────────────────────
GRAND TOTAL:            ✅ 399/399
```

Para correr tests:
```bash
dotnet test -c Release  # Todos
dotnet test -c Release tests/DevBrain.Domain.Tests/  # Solo Domain
```

---

## 🚨 Troubleshooting

### ❌ "Connection refused" al arrancar backend

**Causa:** PostgreSQL o Redis no están corriendo  
**Fix:**
```bash
docker-compose ps
docker-compose up -d   # Si no están corriendo
```

### ❌ Frontend muestra "Cannot reach API"

**Causa:** Backend no está corriendo o puerto incorrecto  
**Fix:**
```bash
# Verifica que backend está en 5118
curl http://localhost:5118/health

# Si no funciona, verifica puerto
lsof -i :5118  # (o en PowerShell: netstat -ano | findstr :5118)
```

### ❌ "Database connection failed"

**Causa:** Connection string incorrecta en  `appsettings.Development.json`  
**Fix:** Verifica:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=devbrain;Username=devbrain;Password=devbrain;SSL Mode=Disable"
  }
}
```

### ❌ "Port 5433 already in use"

**Causa:** Otro servicio usando el puerto  
**Fix:**
```bash
# Detener el contenedor anterior
docker-compose down --remove-orphans

# Volver a levantar
docker-compose up -d
```

---

## ⚠️ Known Limitations (Local Setup)

1. **Redis / Streaks:** No persistidos (usando NoOpStreakService)
   - Streaks siempre retornan 0 en local
   - En producción (Redis Cloud): Funciona correcto

2. **Application Insights:** Logging va a console + archivo local
   - No se envía telemetría a Azure en local
   - En producción: Application Insights activo

3. **Database:** In-memory para tests, PostgreSQL para local dev
   - Tests usan TestContainers (isolated)
   - Local dev usa el mismo PostgreSQL que integration tests

---

## 🎉 ¡Todo listo para testing!

**Próximos Pasos:**
1. ✅ Phases 4.1–4.9 completadas (Frontend completo)
2. ▶️ Phase 4.10 — Por definir (candidatos: leaderboard, filtros en historial, hint visual de atajos)
3. ⏳ Phase 5 — Post-Frontend Testing (Benchmarks, Contract Tests)

**Acceder:**
- http://localhost:3000 — Frontend
- http://localhost:5118/scalar — API Docs
- `localhost:5433` — PostgreSQL (DBeaver / psql)
- `localhost:6379` — Redis (redis-cli)

**Documentación Completa:** Ver [`docs/`](./docs/) folder

---

*Updated: 2026-04-21 | Frontend: Next.js 16.2.3 | Backend: .NET 10 | DB: PostgreSQL 17 + Redis 7*

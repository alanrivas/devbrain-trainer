# 🧪 Testing Guide — DevBrain Trainer

## 📋 Tabla de contenidos
- [Requisitos previos](#requisitos-previos)
- [Setup local](#setup-local)
- [Levantar el stack](#levantar-el-stack)
- [Usuario Admin (Testing)](#usuario-admin-testing)
- [Flujos de prueba](#flujos-de-prueba)
- [API Endpoints](#api-endpoints)
- [Troubleshooting](#troubleshooting)

---

## Requisitos previos

Asegúrate de tener instalado:
- **.NET 10** SDK → `dotnet --version`
- **Node.js 22+** → `node --version`
- **Docker + Docker Compose** → `docker --version` y `docker-compose --version`
- **PostgreSQL 17** (en container)
- **Redis 7** (en container)

---

## Setup local

### 1. Instalar dependencias del frontend

```bash
cd c:\dev\devbrain-trainer\frontend
npm install
```

✅ Esperado: Sin errores, `node_modules/` creado

### 2. Verificar puertos disponibles

Asegúrate de que estos puertos están libres:
- `5118` — Backend API
- `3000` — Frontend (Next.js)
- `5433` — PostgreSQL
- `6379` — Redis

Si algo está usando esos puertos, mata los procesos o cambia en la configuración.

---

## Levantar el stack

### Option A: Levantar todo automáticamente (RECOMENDADO)

#### En Windows PowerShell (from `c:\dev\devbrain-trainer`):

```powershell
# 1. Levantar Docker Compose (PostgreSQL + Redis)
docker-compose up -d

# Espera ~3 segundos a que PostgreSQL esté ready
Start-Sleep -Seconds 3

# 2. Aplicar migraciones y seed inicial
dotnet ef database update --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api

# 3. Crear usuario admin en la BD (ver abajo para credenciales)
# Ej: admin@devbrain.local / Admin123!

# 4. En Terminal 1: Levantar backend
dotnet run --project src/DevBrain.Api

# Backend estará en http://localhost:5118/

# 5. En Terminal 2: Levantar frontend
cd frontend
npm run dev

# Frontend estará en http://localhost:3000/
```

✅ **Ambos servicios deberían estar corriendo sin errores**

---

### Option B: Paso a paso (para debugging)

#### Terminal 1: Docker Compose

```bash
cd c:\dev\devbrain-trainer
docker-compose up

# Esperado:
# ✅ postgres_1 | "database system is ready to accept connections"
# ✅ redis-1   | "Ready to accept connections"
```

#### Terminal 2: Backend

```bash
cd c:\dev\devbrain-trainer

# Aplicar migraciones
dotnet ef database update --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api

# Levantar backend
dotnet run --project src/DevBrain.Api

# Esperado output:
# ✅ "🚀 Starting DevBrain API - Environment: Development"
# ✅ "info: Microsoft.EntityFrameworkCore.Database.Connection[20000]"
# ✅ "Scalar UI:  http://localhost:5118/scalar/"
```

#### Terminal 3: Frontend

```bash
cd c:\dev\devbrain-trainer\frontend
npm run dev

# Esperado output:
# ✅ "▲ Next.js 15.0.0"
# ✅ "- ready on http://localhost:3000"
```

---

## Usuario Admin (Testing)

### Credenciales pre-creadas

**Email:** `admin@devbrain.local`  
**Password:** `Admin123!`  
**Role:** Admin (para futura features)

### Crear el usuario admin en la BD

Si el usuario admin **no existe** automáticamente (por falta de seed), créalo ejecutando:

```powershell
# En tu terminal, en c:\dev\devbrain-trainer\
$ConnectionString = "Host=localhost;Port=5433;Database=devbrain;Username=devbrain;Password=devbrain"

# Genera el hash de "Admin123!" con PBKDF2 (mismo que usa el backend)
$Password = "Admin123!"
$PasswordBytes = [System.Text.Encoding]::UTF8.GetBytes($Password)
$Rfc2898 = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Password, [System.Convert]::FromBase64String("somesalt"), 10000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
# 👇 Usa una herramienta online o construye el hash correctamente

# Alternative: Usa psql directamente para insertar
psql -h localhost -p 5433 -U devbrain -d devbrain -c "
INSERT INTO \"Users\" (\"Id\", \"Email\", \"DisplayName\", \"PasswordHash\", \"EloRating\", \"CreatedAt\", \"UpdatedAt\")
VALUES (
    'e5a8e9b1-1234-5678-9abc-def012345678'::uuid,
    'admin@devbrain.local',
    'Admin User',
    '[PBKDF2_HASH_HERE]',
    1200,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;
"
```

**MEJOR OPCIÓN**: Simplemente regístrate en la app:

```
1. Abre http://localhost:3000
2. Click "Sign Up"
3. Completa: Email, Password, Display Name
4. Click "Sign Up"
5. Eres redirigido a /challenges (ya autenticado ✅)
```

---

## Flujos de prueba

### 🟢 Flujo Happy Path (Recomendado)

#### 1. **Testing de Registro**

```
1. Navega a http://localhost:3000/register
2. Completa:
   - Display Name: "Test User"
   - Email: "test@example.com"
   - Password: "Test123!"
   - Confirm Password: "Test123!"
3. Click "Sign Up"

✅ Esperado: 
   - JWT guardado en localStorage
   - Redirect a http://localhost:3000/challenges
   - Header muestra tu email
```

#### 2. **Testing de Login**

```
1. Click "Logout" en header
2. Navega a http://localhost:3000/login
3. Completa:
   - Email: "test@example.com"
   - Password: "Test123!"
4. Click "Sign In"

✅ Esperado:
   - JWT guardado en localStorage
   - Redirect a http://localhost:3000/challenges
   - Header muestra tu email + Logout button
```

#### 3. **Testing de Challenges**

```
1. En http://localhost:3000/challenges
2. Deberías ver una lista de desafíos (seeded en la BD)
3. Cada challenge muestra:
   - Titulo
   - Dificultad (Easy/Medium/Hard)
   - Categoria
   - Button "Attempt →"

✅ Esperado: 3+ challenges visibles
```

#### 4. **Testing de Auth Protección**

```
1. Abre DevTools (F12) → Application/Storage → Cookies
2. Busca el JWT en localStorage
3. Hace logout (Click "Logout")
4. Intenta acceder a http://localhost:3000/challenges
5. Eres redirigido a http://localhost:3000/login

✅ Esperado: Routes autenticadas están protegidas
```

---

### 🔵 Testing de API (Postman / cURL)

#### Obtener JWT (Login)

```bash
curl -X POST http://localhost:5118/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'

# Response:
{
  "token": "eyJhbGc...",
  "user": {
    "id": "...",
    "email": "test@example.com",
    "displayName": "Test User"
  }
}
```

#### Usar JWT para endpoints protegidos

```bash
JWT="eyJhbGc..." # Reemplaza con tu token

curl -X GET http://localhost:5118/api/v1/challenges \
  -H "Authorization: Bearer $JWT"

# Response: { "items": [...], "page": 1, "pageSize": 10, "total": 5 }
```

#### Crear un Attempt

```bash
curl -X POST http://localhost:5118/api/v1/challenges/{CHALLENGE_ID}/attempt \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "userAnswer": "YES",
    "elapsedSeconds": 15
  }'

# Response: { "attemptId": "...", "isCorrect": true, "newEloRating": 1250, ... }
```

#### Obtener Stats del usuario

```bash
curl -X GET http://localhost:5118/api/v1/users/me/stats \
  -H "Authorization: Bearer $JWT"

# Response:
{
  "totalAttempts": 5,
  "correctAttempts": 3,
  "accuracy": 0.6,
  "currentElo": 1250,
  ...
}
```

---

## API Endpoints

| Endpoint | Method | Auth | Descripción |
|----------|--------|------|-------------|
| `/api/v1/auth/register` | POST | ❌ | Registrar usuario (email, password, displayName) |
| `/api/v1/auth/login` | POST | ❌ | Login (email, password) → JWT |
| `/api/v1/challenges` | GET | ✅ | Listar challenges (con paginación) |
| `/api/v1/challenges/{id}` | GET | ✅ | Detalle de un challenge |
| `/api/v1/challenges/{id}/attempt` | POST | ✅ | Enviar respuesta a un challenge |
| `/api/v1/users/me/stats` | GET | ✅ | Estadísticas del usuario |
| `/api/v1/users/me/badges` | GET | ✅ | Badges del usuario |
| `/scalar/` | GET | ❌ | UI Interactiva de API (Swagger-like) |
| `/health` | GET | ❌ | Health check |

---

### 📊 Bases de datos

#### PostgreSQL

- **Host:** localhost
- **Port:** 5433
- **Database:** devbrain
- **User:** devbrain
- **Password:** devbrain

Conectarse con DBeaver / psql:
```bash
psql -h localhost -p 5433 -U devbrain -d devbrain
```

#### Redis

- **Host:** localhost
- **Port:** 6379

Ver streaks con Redis CLI:
```bash
redis-cli -p 6379
> KEYS "streak:*"
> GET "streak:user-id-here"
```

---

## Postman Collection

Se incluye una colección de Postman en `postman/devbrain-trainer.localhost.postman_collection.json`.

Pasos para usarla:

1. Abre Postman
2. `File` → `Import` → `postman/devbrain-trainer.localhost.postman_collection.json`
3. Selecciona el environment: `devbrain-trainer.localhost.postman_environment.json`
4. ¡A testear! (La colección tiene todas las llamadas pre-configuradas)

---

## Troubleshooting

### ❌ Docker error: "Port 5433 already in use"

```bash
# Mata el servicio en ese puerto
netstat -ano | findstr :5433
taskkill /PID <PID> /F

# O cambia el puerto en docker-compose.yml y en appsettings.json
```

### ❌ "Connection refused" desde backend a PostgreSQL

```bash
# Verifica que PostgreSQL esté corriendo
docker-compose ps

# Si no está corriendo:
docker-compose up -d postgres redis
```

### ❌ Frontend no conecta con backend (CORS error)

```bash
# Verifica que el backend esté corriendo en http://localhost:5118
curl http://localhost:5118/health

# Si error, cheq: ASPNETCORE_URLS en Program.cs
# Debe escuchar en http://localhost:5118
```

### ❌ "JWT expired" error

Los JWTs expiran cada 24 horas. Si lleva > 24h:
1. Haz logout
2. Login nuevamente
3. Nuevo JWT se genera y guarda en localStorage

### ❌ Migraciones falladas

```bash
# Reset la BD (⚠️ Borra TODO)
dotnet ef database drop --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api -f

# Aplicar migraciones nuevamente
dotnet ef database update --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api
```

### ❌ "Cannot access frontend on 3000"

```bash
# Verifica que npm run dev esté corriendo
# Node.js debe estar en el PATH

# Reinstala dependencias
cd frontend
rm -r node_modules
npm install
npm run dev
```

---

## 🚀 Endpoints útiles

### Dashboard / UI

- **Frontend Home:** http://localhost:3000
- **Frontend Login:** http://localhost:3000/login
- **Frontend Register:** http://localhost:3000/register
- **Frontend Challenges:** http://localhost:3000/challenges (requiere auth)

### API

- **Scalar UI (Swagger):** http://localhost:5118/scalar/
- **Health Check:** http://localhost:5118/health
- **API Base:** http://localhost:5118/api/v1

---

## 💡 Tips para testing

1. **Usa DevTools de Firefox/Chrome** para inspeccionar localStorage y network requests
2. **Postman** es ideal para testear endpoints sin UI
3. **Test containers** cumplen migraciones automáticamente — no necesitas migrar manualmente si usas tests
4. **Redis CLI** para debuggear streaks en tiempo real
5. **Application Insights** en Production logs — Application Insights telemetría se integra automáticamente

---

**¿Algo no funciona?** Revisa los logs:
- Backend: `logs/devbrain-*.txt` (rolling daily)
- Frontend: Abre DevTools (F12) → Console
- Docker: `docker-compose logs api`, `docker-compose logs db`, etc.

**Happy testing! 🎉**

# Configuración Local de PostgreSQL 18 para DevBrain Trainer

## Estado Actual
✅ Npgsql.EntityFrameworkCore.PostgreSQL instalado
✅ appsettings.json con connection string template
✅ appsettings.Development.json configurado
✅ Program.cs ya soporta PostgreSQL con Npgsql

## Pasos Pendientes

### 1. Conectarse a PostgreSQL como admin

Abre PowerShell como Administrador y ejecuta:

```powershell
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$env:PGPASSWORD = "admin"  # Reemplaza 'admin' con tu contraseña real
& $psqlPath -U postgres -h 127.0.0.1 -p 5432
```

### 2. Crear usuario `devbrain` con permisos limitados

Una vez conectado a psql (verás el prompt `postgres=#`), ejecuta:

```sql
-- Crear usuario devbrain
CREATE USER devbrain WITH PASSWORD 'devbrain_secure_password';

-- Crear base de datos
CREATE DATABASE devbrain_local OWNER devbrain;

-- Permitir al usuario conectarse
\c devbrain_local

-- Conceder permisos sobre el schema public
GRANT USAGE ON SCHEMA public TO devbrain;
GRANT CREATE ON SCHEMA public TO devbrain;

-- Permisos default para nuevas tablas (DML permitido, DDL permitido, pero NO CREATE ROLE)
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO devbrain;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO devbrain;

-- Salir
\q
```

### 3. Actualizar connection string en appsettings

Edita `src/DevBrain.Api/appsettings.json` y `src/DevBrain.Api/appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=devbrain_local;Username=devbrain;Password=devbrain_secure_password"
}
```

**Nota**: Reemplaza `devbrain_secure_password` con la contraseña que elegiste en el paso 2.

### 4. Crear y aplicar migrations

```powershell
cd c:\dev\devbrain-trainer

# Cambiar UseInMemoryDatabase a false en appsettings.Development.json
(Get-Content src/DevBrain.Api/appsettings.Development.json) -replace '"UseInMemoryDatabase": true', '"UseInMemoryDatabase": false' | Set-Content src/DevBrain.Api/appsettings.Development.json

# Crear initial migration
dotnet ef migrations add InitialCreate --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api

# Aplicar migration a PostgreSQL
dotnet ef database update --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api
```

### 5. Verificar que los tests sigan pasando

```powershell
# Cambiar UseInMemoryDatabase a true (los tests usarán in-memory)
(Get-Content src/DevBrain.Api/appsettings.Development.json) -replace '"UseInMemoryDatabase": false', '"UseInMemoryDatabase": true' | Set-Content src/DevBrain.Api/appsettings.Development.json

# Ejecutar tests
dotnet test

# Esperado: 108/108 tests passing
```

## Connection String Examples

### Local Development (PostgreSQL)
```
Host=localhost;Port=5432;Database=devbrain_local;Username=devbrain;Password=devbrain_secure_password
```

### Production (Railway) - Será configurado después
```
Will be added in deploy step
```

## Troubleshooting

### Error: "FATAL: password authentication failed"
- Verifica que la contraseña sea correcta en `$env:PGPASSWORD`
- Verifica que PostgreSQL 18 esté corriendo: `Get-Service postgresql-x64-18`

### Error: "database does not exist"
- Confirma que ejecutaste `CREATE DATABASE devbrain_local` correctamente
- Verifica que el usuario `devbrain` tiene permisos: `\du` en psql

### Error: "permission denied"
- El usuario `devbrain` debe tener `USAGE` y `CREATE` en schema public
- Verifica permisos: `\dn` en psql

## estructura PostgreSQL esperada

```
Database: devbrain_local
Owner: devbrain
Schema: public
Tables (se crearán con migrations):
  - Challenges
  - Attempts
  - Users
```

---

**Próximo paso**: Contacta y sigue los pasos 1-5 arriba para completar la configuración.

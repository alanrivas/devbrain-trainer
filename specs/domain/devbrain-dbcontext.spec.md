# Spec: DevBrain DbContext

**Tipo**: Infraestructura (Entity Framework Core)  
**Ubicación**: `DevBrain.Infrastructure`  
**Versión**: 1.0  

---

## Qué es

`DevBrainDbContext` es el punto de acceso a la base de datos PostgreSQL mediante Entity Framework Core 10. Configura las tablas (`Users`, `Challenges`, `Attempts`), relaciones, índices, y proporciona migraciones iniciales.

Es la base que permite implementar los repositorios (`EFChallengeRepository`, `EFAttemptRepository`) en los siguientes pasos.

---

## Estructura de DbContext

### DbSets

| DbSet | Tipo | Tabla | Descripción |
|-------|------|-------|-------------|
| `Users` | `DbSet<User>` | `users` | Perfiles de usuario desde Supabase Auth |
| `Challenges` | `DbSet<Challenge>` | `challenges` | Problemas técnicos disponibles |
| `Attempts` | `DbSet<Attempt>` | `attempts` | Intentos de respuesta de usuarios |

---

## Configuración de tablas

### Tabla `users`

| Columna | Tipo | Propiedades | Notas |
|---------|------|-------------|-------|
| `id` | `varchar(36)` | PK, no nulo | SupabaseId (UUID como string) |
| `email` | `varchar(255)` | no nulo, UNIQUE | Email del usuario |
| `display_name` | `varchar(50)` | no nulo | Nombre mostrado en la app |
| `created_at` | `timestamp with tz` | no nulo | Fecha de creación (UTC) |

**Índices**: UNIQUE en `email`

---

### Tabla `challenges`

| Columna | Tipo | Propiedades | Notas |
|---------|------|-------------|-------|
| `id` | `uuid` | PK, default `gen_uuid_v4()`, no nulo | Identificador del desafío |
| `title` | `varchar(100)` | no nulo | Título del problema |
| `description` | `text` | no nulo | Enunciado completo |
| `category` | `smallint` | no nulo | Enum: 0=SQL, 1=CodeLogic, 2=Architecture, 3=DevOps, 4=WorkingMemory |
| `difficulty` | `smallint` | no nulo | Enum: 0=Easy, 1=Medium, 2=Hard |
| `correct_answer` | `text` | no nulo | Respuesta correcta (caso-insensible) |
| `time_limit_secs` | `int` | no nulo | Segundos permitidos (30-300) |
| `created_at` | `timestamp with tz` | no nulo | Fecha de creación (UTC) |

**Índices**: sobre `category`, `difficulty` (para filtrados rápidos en GET /challenges)

---

### Tabla `attempts`

| Columna | Tipo | Propiedades | Notas |
|---------|------|-------------|-------|
| `id` | `uuid` | PK, default `gen_uuid_v4()`, no nulo | Identificador del intento |
| `user_id` | `varchar(36)` | FK → `users.id`, no nulo | SupabaseId del usuario |
| `challenge_id` | `uuid` | FK → `challenges.id`, no nulo | ID del desafío respondido |
| `submitted_answer` | `text` | no nulo | Respuesta enviada por el usuario |
| `is_correct` | `boolean` | no nulo | Resultado (true = correcto) |
| `time_taken_secs` | `int` | no nulo | Segundos que tardó en responder |
| `created_at` | `timestamp with tz` | no nulo | Fecha del intento (UTC) |

**Índices**:
- FK sobre `user_id` (para GetByUserAsync)
- Compuesto sobre `(user_id, created_at DESC)` (para GetLastByUserAsync, streaks)
- Compuesto sobre `(user_id, challenge_id, is_correct)` (para estadísticas por categoría)

---

## Relaciones

- `Attempt` → `User`: N a 1 (muchos intentos por usuario)
- `Attempt` → `Challenge`: N a 1 (muchos intentos por desafío)
- `User` ↔ `Challenge`: sin relación directa

**Cascada de borrado**: ON DELETE CASCADE en attempts si se borra un usuario o challenge (para mantener integridad referencial).

---

## Seed Data

Al crear la migración inicial, se insertan datos base para que el MVP sea testeable:

### Seed de Challenges (al menos 10 problemas)

Se definen en el código y se ejecutan en `OnModelCreating` o en una seed data separada:

**Ejemplos**:
- SQL: "Escribe un SELECT que devuelva el nombre de usuarios con más de 10 intentos completados"
- C#: "Refactoriza este código para aplicar DRY"
- Docker: "¿Cuál es el comando para listar todos los contenedores?"
- Arquitectura: "¿Qué patrón resuelve este escenario?"
- Memoria: "Traza esta ejecución y dime el valor final de X"

**Propiedades de cada seed**:
- Título único
- Categoría y dificultad variadas (mix de Easy, Medium, Hard)
- Respuesta correcta única y válida para validación

---

## Invariantes de integridad

1. Toda fila en `attempts` debe referenciar un `user_id` y `challenge_id` válidos
2. No puede haber `user_id` nulo, `challenge_id` nulo o `submitted_answer` nulo
3. `is_correct` es determinado por comparar `submitted_answer` con `Challenge.IsCorrectAnswer()`
4. `time_taken_secs` siempre está entre 0 y `time_limit_secs` (validado en la entidad)
5. Las fechas se almacenan en UTC (tipo `timestamp with tz`)

---

## Qué NO es esta spec

- No define cómo conectarse a PostgreSQL (eso depende de `appsettings.json` y configuración en `Program.cs`)
- No implementa los repositorios (`EFChallengeRepository`, `EFAttemptRepository`) — esos vienen en specs posteriores
- No incluye migraciones futuras (nuevos campos, nuevas tablas — se hacen en specs posteriores)

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| DbContext se crea correctamente | OK — contexto disponible |
| Se crean las tablas `users`, `challenges`, `attempts` | OK — esquema presente |
| Se insertan ~10 challenges seed | OK — datos iniciales presentes |
| FK `attempts.user_id` → `users.id` existe | OK — relación configurada |
| FK `attempts.challenge_id` → `challenges.id` existe | OK — relación configurada |
| Insertar un `Attempt` con `user_id` inválido | PK/FK Error (integridad de BD) |
| Insertar un `Attempt` con `challenge_id` inválido | PK/FK Error (integridad de BD) |
| Consultar challenges por categoría con índice | OK — query rápida |
| Consultar último attempt de un usuario | OK — retorna el más reciente |
| Índice compuesto `(user_id, created_at)` existe | OK — índice presente |

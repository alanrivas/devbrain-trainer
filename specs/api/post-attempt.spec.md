# Spec: POST /challenges/{id}/attempt

**Tipo**: Endpoint de API (con autenticación)  
**Ubicación**: `DevBrain.Api`  
**Versión**: 1.0  

---

## Qué es

Endpoint POST que permite a un usuario autenticado enviar su respuesta a un challenge.
- Valida que el usuario está logueado (JWT de Supabase)
- Crea un `Attempt` (intento de resolver)
- Devuelve si la respuesta es correcta o no
- Persiste el intento en DB para histórico y estadísticas

---

## Ruta y método

```
POST /api/v1/challenges/{id}/attempt
```

---

## Path Parameters

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | `Guid` | ID del challenge a resolver |

---

## Encabezados requeridos

| Encabezado | Valor | Descripción |
|------------|-------|-------------|
| `Authorization` | `Bearer {jwt}` | JWT válido de Supabase Auth |
| `Content-Type` | `application/json` | (implícito en POST con body JSON) |

---

## Body de solicitud

```json
{
  "userAnswer": "SELECT * FROM users",
  "elapsedSeconds": 45
}
```

### Campos

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `userAnswer` | `string` | Sí | No vacío, se trimea antes de comparar con correctAnswer |
| `elapsedSeconds` | `int` | Sí | >= 0, <= challenge.TimeLimitSecs (si exceeds, devolver warning pero permite save) |

---

## Respuesta exitosa (201 Created)

```json
{
  "attemptId": "550e8400-e29b-41d4-a716-446655440000",
  "challengeId": "660e8400-e29b-41d4-a716-446655440001",
  "userId": "user_abc123xyz",
  "userAnswer": "SELECT * FROM users",
  "isCorrect": true,
  "correctAnswer": "SELECT * FROM users",
  "elapsedSeconds": 45,
  "challengeTitle": "SQL SELECT Basics",
  "occurredAt": "2026-04-09T14:30:45.123Z"
}
```

### Campos de respuesta

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `attemptId` | `Guid` | ID generado del intento |
| `challengeId` | `Guid` | ID del challenge resuelto |
| `userId` | `string` | ID del usuario (SupabaseId extractado del JWT) |
| `userAnswer` | `string` | Respuesta enviada (trimmed) |
| `isCorrect` | `bool` | `true` si userAnswer === correctAnswer (case-insensitive) |
| `correctAnswer` | `string` | Respuesta correcta del challenge (siempre se devuelve para aprendizaje) |
| `elapsedSeconds` | `int` | Segundos transcurridos resolviendo |
| `challengeTitle` | `string` | Título del challenge (para contexto en frontend) |
| `occurredAt` | `ISO 8601 DateTime` | Timestamp del servidor cuando se guardó |

---

## Errores posibles

### 400 Bad Request — Validación fallida

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "userAnswer": ["Field is required and cannot be empty"]
  }
}
```

**Causas**:
- `userAnswer` está vacío o solo whitespace
- `elapsedSeconds` < 0
- `elapsedSeconds` > 3600 (1 hora máximo permitido)
- Body JSON inválido (parse error)

### 401 Unauthorized — Usuario no autenticado

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Missing or invalid authorization token"
}
```

**Causas**:
- Header `Authorization` faltante
- Token expirado o inválido
- Token no es de Supabase (formato incorrecto)

### 404 Not Found — Challenge no existe

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Challenge with ID '660e8400-e29b-41d4-a716-446655440999' not found"
}
```

**Causas**:
- Challenge ID en ruta no existe en DB

### 422 Unprocessable Entity — Usuario sin permisos

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Unprocessable Entity",
  "status": 422,
  "detail": "User profile not found or not initialized"
}
```

**Causas**:
- JWT válido pero usuario no existe en tabla `Users`
- Usuario fue borrado después de loguearse

---

## Comportamientos

### Creación de intento

1. Extraer `userId` del JWT (claim `sub` de Supabase)
2. Verificar que usuario existe en DB
3. Buscar challenge por ID (404 si no existe)
4. Crear `Attempt` con:
   - `challengeId` (del path param)
   - `userId` (del JWT)
   - `userAnswer` (trimado del body)
   - `elapsedSeconds` (del body)
   - `isCorrect` = `challenge.IsCorrectAnswer(userAnswer)`
   - `occurredAt` = `DateTime.UtcNow`
5. Guardar vía `attemptRepository.AddAsync()`
6. Devolver Attempt serializado (201 Created)

### Comparación de respuesta (isCorrect)

- Usa `Challenge.IsCorrectAnswer(userAnswer)` del dominio
- Comparación **case-insensitive**
- Se trimean espacios antes de comparar
- No se normalizan saltos de línea (acepta como está)

### Tiempos excedidos

- Si `elapsedSeconds` > `challenge.TimeLimitSecs`, registra pero **permite guardar**
- Devuelve warning implícitamente (frontend puede mostrar "Tiempo excedido")
- Se contabiliza igual en streak / ELO

---

## Invariantes

1. Todo `Attempt` guardado corresponde a un `Challenge` y `User` existentes
2. `isCorrect` siempre refleja la realidad (nunca se guarda incorrectamente)
3. `userId` en Attempt siempre coincide con el del JWT (no acepta otro userId en body)
4. `occurredAt` es siempre `DateTime.UtcNow` en el servidor (no acepta timestamp del cliente)

---

## Qué NO es este endpoint

- No actualiza ELO/rating (eso es responsabilidad de un servicio de gamificación posterior)
- No crea los challenges (eso es seed data en DbContext o admin endpoint)
- No verifica acceso a challenges categorizado por rol/nivel (todos pueden intentar todos)
- No gestiona "bloqueos" de intentos (same user, same challenge, same day) — se acepta re-intentar

---

## Casos de prueba esperados (13 Tests)

| Escenario | Entrada | Esperado |
|-----------|---------|----------|
| Respuesta correcta | Challenge con correctAnswer="YES", userAnswer="YES" | `isCorrect: true`, 201 Created |
| Respuesta incorrecta | correctAnswer="SELECT *", userAnswer="SELECT COUNT(*)" | `isCorrect: false`, 201 Created |
| Case-insensitive | correctAnswer="SQL", userAnswer="sql" | `isCorrect: true` |
| Espacios trimados | correctAnswer="test", userAnswer="  test  " | `isCorrect: true` |
| Respuesta vacía | userAnswer="" | 400 Bad Request |
| elapsedSeconds negativo | elapsedSeconds=-5 | 400 Bad Request |
| elapsedSeconds muy grande | elapsedSeconds=3601 | 400 Bad Request |
| Body JSON inválido | (JSON parse error) | 400 Bad Request |
| Sin Authorization header | (falta header) | 401 Unauthorized |
| Token expirado/inválido | (token corrupto) | 401 Unauthorized |
| Challenge no existe | ID="00000000-0000-0000-0000-000000000000" | 404 Not Found |
| Usuario no existe en DB | (JWT válido pero user no seedeado) | 422 Unprocessable Entity |
| Tiempo dentro de límite | elapsedSeconds < timeLimitSecs | 201 Created con success |

---

## Dependencias

- ✅ `Challenge` entidad (dominio)
- ✅ `Attempt` entidad (dominio)
- ✅ `User` entidad (dominio)
- ✅ `IChallengeRepository.GetByIdAsync()`
- ✅ `IAttemptRepository.AddAsync()`
- ⏳ JWT middleware de Supabase (para extraer userId)
- ⏳ User repository (para verificar usuario existe)

---

## Notas de implementación

- Header `Content-Type` se valida automáticamente en ASP.NET Core
- JWT parsing se hace en middleware (fase de autenticación)
- `DateTime.UtcNow` en servidor, no confiamos en timestamp del cliente
- Manejo de zona horaria: todo en UTC

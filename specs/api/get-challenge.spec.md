# Spec: GET /challenges/{id}

**Tipo**: Endpoint de API (sin autenticación)  
**Ubicación**: `DevBrain.Api`  
**Versión**: 1.0  

---

## Qué es

Endpoint GET que retorna los detalles completos de un `Challenge` específico.
Permite al usuario/frontend ver el enunciado, categoría, dificultad y tiempo límite antes de intentar resolverlo.
No requiere autenticación — cualquiera puede ver el detalle de un challenge.

---

## Ruta y método

```
GET /api/v1/challenges/{id}
```

**Path Parameters**:
- `id` (UUID/Guid) — identificador único del challenge

---

## Respuesta exitosa (200 OK)

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "SQL: Select Top N Records",
  "description": "Write a query that returns top 5 users by attempt count from the users table ordered by attempts DESC",
  "category": "Sql",
  "difficulty": "Easy",
  "timeLimitSecs": 60
}
```

**Esquema**:
- `id` (UUID): identificador único del challenge
- `title` (string): título corto del problema
- `description` (string): enunciado completo con instrucciones
- `category` (string): categoría (e.g., `Sql`, `CodeLogic`, `Architecture`, `DevOps`, `WorkingMemory`)
- `difficulty` (string): nivel de dificultad (`Easy`, `Medium`, `Hard`)
- `timeLimitSecs` (int): tiempo máximo permitido en segundos

**Notas**:
- **NO incluir** `correctAnswer` en la respuesta (eso es secreto del servidor)
- **NO incluir** `createdAt` o metadata de auditoría (innecesaria para el usuario)
- Usar el mismo DTO `ChallengeResponseDto` que en GET /challenges

---

## Respuesta si el ID no existe (404 Not Found)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Challenge not found"
}
```

---

## Respuesta si el ID no es un GUID válido (400 Bad Request)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid challenge ID format. Must be a valid GUID."
}
```

---

## Escenarios de test esperados

| Escenario | Status | Resultado |
|-----------|--------|-----------|
| GET /api/v1/challenges/{existing-valid-id} | 200 | Retorna challenge completo |
| GET /api/v1/challenges/{non-existent-id} | 404 | Error "Challenge not found" |
| GET /api/v1/challenges/{invalid-guid-format} | 400 | Error de formato inválido |
| GET /api/v1/challenges/ (sin id) | 404 | Ruta no encuentra handler (no es responsabilidad de este endpoint) |
| Respuesta no incluye `correctAnswer` | 200 | Campo ausente en response |
| Respuesta no incluye `createdAt` | 200 | Campo ausente en response |
| Response es igual a item dentro de lista GET /challenges | 200 | Consistencia DTO |
| ID es case-insensitive (GUID válido con mayúsculas) | 200 | Retorna same challenge |

---

## Comportamiento esperado

### Búsqueda exitosa
- El endpoint valida que el `id` sea un GUID válido (formato correcto)
- Si válido, consulta `IChallengeRepository.GetByIdAsync(id)`
- Si existe, mapea a `ChallengeResponseDto` y retorna 200
- Si no existe, retorna 404 sin exponer mensajes internos

### Validación de entrada
- Si el `id` no cumple formato GUID (ej: `abc123`, vacío, null), rechaza con 400
- ASP.NET model binding valida automáticamente — no aceptar strings inválidos

### Seguridad
- **No exponer** `correctAnswer` nunca
- **No permitir** acceso a challenges en estado "borrador" (si existen en el futuro)
- La respuesta es pública (sin autenticación)

---

## Dependencias existentes

- `IChallengeRepository.GetByIdAsync(id)` — ya existe e implementada en `EFChallengeRepository`
- `ChallengeMapper.ToResponseDto(challenge)` — ya existe
- ASP.NET Core model binding — para parsing automático de GUID

---

## DTOs reutilizados

### ChallengeResponseDto

```csharp
public sealed record ChallengeResponseDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Difficulty,
    int TimeLimitSecs
);
```

*(Este DTO ya existe y se usa en GET /challenges)*

---

## Notas de implementación

- **Ruta**: registrar con `app.MapGet("/api/v1/challenges/{id}", GetChallenge)` en `ChallengeEndpoints.cs`
- **Handler**: método estático `GetChallenge(Guid id, IChallengeRepository repo)` con validación de resultado
- **Mapeo**: usar `challenge.ToResponseDto()` existente
- **Errores**: retornar `Results.NotFound()` o `Results.BadRequest()` según corresponda
- **Tests**: 4-5 test cases (happy path, not found, invalid guid, etc.)

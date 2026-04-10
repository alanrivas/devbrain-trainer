# Spec: GET /users/me/stats

**Tipo**: Endpoint de API (requiere autenticación JWT)  
**Ubicación**: `DevBrain.Api`  
**Versión**: 1.0  

---

## Qué es

Endpoint GET que retorna las estadísticas del usuario autenticado: intentos totales, aciertos, tasa de precisión, streak actual, rating ELO y fecha del último intento.

El usuario se identifica automáticamente por el JWT en el header `Authorization: Bearer <token>`. No acepta un `userId` como parámetro — solo retorna los datos del caller.

---

## Ruta y método

```
GET /api/v1/users/me/stats
```

**Requiere**: `Authorization: Bearer <jwt>` (401 si ausente o inválido)

---

## Respuesta exitosa (200 OK)

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "displayName": "Alan",
  "totalAttempts": 42,
  "correctAttempts": 30,
  "accuracyRate": 0.714,
  "currentStreak": 0,
  "eloRating": 1000,
  "lastAttemptAt": "2026-04-09T10:00:00Z"
}
```

**Esquema**:
- `userId` (UUID): ID del usuario autenticado
- `displayName` (string): nombre visible del usuario
- `totalAttempts` (int): total de attempts registrados por este usuario
- `correctAttempts` (int): cantidad de attempts con `IsCorrect = true`
- `accuracyRate` (float, 0.0–1.0): `correctAttempts / totalAttempts`, o `0.0` si `totalAttempts == 0`
- `currentStreak` (int): días consecutivos con al menos un attempt — **placeholder: siempre 0** hasta implementar Redis (Fase F)
- `eloRating` (int): rating ELO global — **placeholder: siempre 1000** hasta implementar Fase F
- `lastAttemptAt` (DateTime? / null): fecha UTC del último attempt, o `null` si nunca intentó

---

## Respuesta sin autenticación (401 Unauthorized)

El middleware JWT rechaza la request antes de llegar al handler. No requiere lógica adicional.

---

## Respuesta si el usuario del token no existe en la DB (404 Not Found)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "User not found"
}
```

Escenario de baja probabilidad (token válido pero usuario eliminado), pero debe manejarse defensivamente.

---

## Escenarios de test esperados

| Escenario | Status | Resultado |
|-----------|--------|-----------|
| Sin token | 401 | Middleware lo rechaza |
| Token válido, sin attempts | 200 | totalAttempts=0, correctAttempts=0, accuracyRate=0.0, lastAttemptAt=null |
| Token válido, con attempts mixtos | 200 | Conteos correctos, accuracyRate calculado |
| accuracyRate con todos correctos | 200 | accuracyRate = 1.0 |
| accuracyRate con ninguno correcto | 200 | accuracyRate = 0.0 |
| Response contiene userId del JWT | 200 | userId coincide con claim `sub` |
| Response contiene displayName del usuario | 200 | displayName del User en DB |
| currentStreak siempre 0 (placeholder) | 200 | currentStreak = 0 |
| eloRating siempre 1000 (placeholder) | 200 | eloRating = 1000 |
| lastAttemptAt refleja el attempt más reciente | 200 | OccurredAt del último attempt |

---

## Comportamiento esperado

### Happy path
1. Extraer `userId` del claim `sub` del JWT
2. `IUserRepository.GetByIdAsync(userId)` para obtener `displayName`
3. Si usuario no existe → 404
4. `IAttemptRepository.GetByUserAsync(userId)` → lista de attempts del usuario
5. `IAttemptRepository.CountCorrectByUserAsync(userId)` → cantidad de aciertos
6. `IAttemptRepository.GetLastByUserAsync(userId)` → fecha del último intento
7. Calcular `accuracyRate = totalAttempts > 0 ? (float)correctAttempts / totalAttempts : 0.0f`
8. Retornar 200 con `UserStatsResponseDto`

### Placeholders (Fase F)
- `currentStreak = 0` — Redis no conectado aún
- `eloRating = 1000` — cálculo ELO no implementado aún
- Estos campos están en el contrato de la API para no romper el frontend cuando se implemente Fase F

---

## DTOs nuevos

### UserStatsResponseDto

```csharp
public sealed record UserStatsResponseDto(
    Guid UserId,
    string DisplayName,
    int TotalAttempts,
    int CorrectAttempts,
    float AccuracyRate,
    int CurrentStreak,
    int EloRating,
    DateTimeOffset? LastAttemptAt
);
```

**Ubicación**: `src/DevBrain.Api/DTOs/UserStatsResponseDto.cs`

---

## Dependencias existentes

- `IAttemptRepository.GetByUserAsync(Guid userId)` — lista todos los attempts del usuario
- `IAttemptRepository.CountCorrectByUserAsync(Guid userId)` — cuenta aciertos
- `IAttemptRepository.GetLastByUserAsync(Guid userId)` — último attempt
- `IUserRepository.GetByIdAsync(Guid id)` — datos del usuario (displayName)
- JWT middleware ya configurado — userId disponible via `HttpContext.User`

---

## Notas de implementación

- **Archivo nuevo**: `src/DevBrain.Api/Endpoints/UserEndpoints.cs`
- **Registro**: `app.MapUserEndpoints()` en `Program.cs`
- **Handler**: `GetUserStats(HttpContext httpContext, IUserRepository userRepo, IAttemptRepository attemptRepo)`
- **Protección**: `.RequireAuthorization()` en el endpoint
- **Tests**: `tests/DevBrain.Api.Tests/GetUserStatsTests.cs` (10 test cases)

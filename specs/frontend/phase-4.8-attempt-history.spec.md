# Spec: Phase 4.8 — Attempt History

**Tipo**: Full-stack Feature (nuevo endpoint API + página frontend)  
**Ubicación**:
- `src/DevBrain.Api/Endpoints/UserEndpoints.cs` — nuevo endpoint `GET /api/v1/users/me/attempts`
- `src/DevBrain.Api/DTOs/AttemptHistoryItemDto.cs` — nuevo DTO
- `src/DevBrain.Infrastructure/Persistence/EFAttemptRepository.cs` — incluir `Challenge` en la query
- `tests/DevBrain.Api.Tests/GetUserAttemptsTests.cs` — tests del endpoint
- `frontend/src/app/history/page.tsx` — nueva página `/history`
- `frontend/src/app/history/page.test.tsx` — tests de la página  
**Versión**: 1.0

---

## Qué es

Phase 4.8 permite al usuario ver el historial completo de sus intentos pasados. La página `/history` muestra cada intento con el título del challenge, si fue correcto o incorrecto, el tiempo empleado y la fecha.

Esta fase requiere un nuevo endpoint en el backend que no existe actualmente. El repositorio `EFAttemptRepository.GetByUserAsync` ya retorna los intentos ordenados por fecha descendente, pero sin incluir la navegación `Challenge` (no tiene `.Include()`). Se debe ajustar para cargar el título del challenge.

---

## Parte 1 — Backend: `GET /api/v1/users/me/attempts`

### Contrato

```
GET /api/v1/users/me/attempts
Authorization: Bearer {token}

200 OK
[
  {
    "attemptId": "guid",
    "challengeId": "guid",
    "challengeTitle": "SQL Challenge",
    "isCorrect": true,
    "elapsedSecs": 42,
    "occurredAt": "2026-04-15T10:00:00Z"
  },
  ...
]

401 Unauthorized  — sin token o token inválido
```

### DTO nuevo: `AttemptHistoryItemDto`

```csharp
public record AttemptHistoryItemDto(
    Guid AttemptId,
    Guid ChallengeId,
    string ChallengeTitle,
    bool IsCorrect,
    int ElapsedSecs,
    DateTimeOffset OccurredAt
);
```

### Cambio en `EFAttemptRepository`

`GetByUserAsync` debe incluir la navegación `Challenge` para que `ChallengeTitle` esté disponible:

```csharp
// Agregar .Include(a => a.Challenge) a la query de GetByUserAsync
```

No se necesita cambiar la firma de la interfaz ni el método — solo la implementación EF.

### Comportamiento del endpoint

- Requiere JWT válido → 401 si ausente o inválido
- Lee el `userId` del claim `NameIdentifier`
- Llama a `attemptRepository.GetByUserAsync(userId)` (ya ordenado por `OccurredAt` desc)
- Mapea cada `Attempt` a `AttemptHistoryItemDto` usando `attempt.Challenge?.Title ?? "Unknown"`
- Si el usuario no tiene intentos, retorna `[]` (lista vacía, 200 OK — **no** 404)
- Los intentos se devuelven **solo del usuario autenticado** — nunca de otros usuarios

### Escenarios de test — `GetUserAttemptsTests.cs` (~5)

| Escenario | Resultado |
|-----------|-----------|
| Sin token | 401 Unauthorized |
| Usuario sin intentos | 200 OK con lista vacía `[]` |
| Usuario con intentos | 200 OK con cada item incluyendo `challengeTitle` correcto |
| Orden cronológico | El intento más reciente aparece primero |
| Aislamiento de usuario | Solo retorna intentos del usuario autenticado (no de otros) |

---

## Parte 2 — Frontend: página `/history`

### Comportamiento

- Si el usuario no tiene token, redirige a `/login`
- Si el backend devuelve 401, llama `clearAuth()` y redirige a `/login`
- Mientras carga: muestra texto que coincida con `/loading history/i`
- Una vez cargado, muestra una lista de intentos

Cada fila de la lista muestra:
| Campo | Fuente | Formato |
|---|---|---|
| Título del challenge | `challengeTitle` | Link a `/challenges/{challengeId}` |
| Resultado | `isCorrect` | "Correct" o "Incorrect" |
| Tiempo | `elapsedSecs` | `{n}s` |
| Fecha | `occurredAt` | fecha local legible |

- Si la lista está vacía: muestra texto que coincida con `/no attempts yet/i`
- Error genérico (5xx): muestra `role="alert"` con texto `/failed to load/i`

### Navegación hacia `/history`

- La página `/stats` incluye un link a `/history` con texto que coincida con `/view history/i`

### Escenarios de test — `history/page.test.tsx` (~8)

| Escenario | Resultado |
|-----------|-----------|
| No hay token | Redirige a `/login` |
| Petición pendiente | Muestra `/loading history/i` |
| Lista con intentos | Muestra `challengeTitle` de cada intento |
| Intento correcto | Muestra texto "Correct" |
| Intento incorrecto | Muestra texto "Incorrect" |
| Lista vacía | Muestra `/no attempts yet/i` |
| 401 del backend | Llama `clearAuth()` + redirige a `/login` |
| 5xx del backend | Muestra `role="alert"` con `/failed to load/i` |

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `src/DevBrain.Api/DTOs/AttemptHistoryItemDto.cs` | Nuevo DTO |
| `src/DevBrain.Api/Endpoints/UserEndpoints.cs` | Nuevo endpoint `GET /me/attempts` |
| `src/DevBrain.Infrastructure/Persistence/EFAttemptRepository.cs` | Agregar `.Include(a => a.Challenge)` en `GetByUserAsync` |
| `tests/DevBrain.Api.Tests/GetUserAttemptsTests.cs` | 5 tests del nuevo endpoint |
| `frontend/src/app/history/page.tsx` | Nueva página |
| `frontend/src/app/history/page.test.tsx` | 8 tests de la página |
| `frontend/src/app/stats/page.tsx` | Agregar link "View history" |
| `frontend/src/app/stats/page.test.tsx` | 1 test: link visible en stats |

---

## Recuento de tests nuevos

| Suite | Tests nuevos |
|-------|-------------|
| `DevBrain.Api.Tests` | +5 (GetUserAttemptsTests) |
| `Frontend.Tests` | +9 (history/page x8 + stats link x1) |
| **Total nuevos** | **14** |
| **Grand total esperado** | **376 + 14 = 390** |

---

## Qué NO es esta fase

- No pagina el historial (devuelve todos los intentos del usuario, sin límite por ahora)
- No permite filtrar por challenge, fecha o resultado
- No muestra la respuesta del usuario ni la respuesta correcta en el historial
- No modifica las reglas de negocio de `Attempt`
- No toca el endpoint de submit (`POST /challenges/{id}/attempt`)

---

## Criterios de éxito

- `GET /api/v1/users/me/attempts` devuelve los intentos del usuario con título del challenge
- La página `/history` renderiza correctamente el historial
- El link "View history" en `/stats` navega a `/history`
- 249/249 tests de backend siguen en verde
- 14 tests nuevos en verde
- Grand total: **390/390 ✅**

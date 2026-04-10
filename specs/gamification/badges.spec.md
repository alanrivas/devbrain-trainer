# Spec: Sistema de Badges (Logros)

**Tipo**: Regla de gamificación — entidad de dominio + servicio de evaluación  
**Ubicación**: `DevBrain.Domain`  
**Versión**: 1.0

---

## Qué es

Un badge es un logro único que se otorga al usuario cuando cumple una condición específica durante su actividad en la app (attempts, streak, ELO). Cada badge se otorga **una sola vez** — si ya fue ganado, no se vuelve a otorgar.

El sistema de badges tiene dos piezas de dominio: la entidad `UserBadge` (registro de que un usuario ganó un badge) y el servicio `BadgeAwardService` (lógica pura que evalúa qué badges nuevos corresponden dado el contexto de un attempt).

---

## Enum: `BadgeType`

**Ubicación**: `src/DevBrain.Domain/Enums/BadgeType.cs`

| Valor | Nombre | Condición de otorgamiento |
|-------|--------|--------------------------|
| `FirstBlood` | First Blood | Primera respuesta correcta del usuario |
| `OnFire` | On Fire | Streak ≥ 3 días consecutivos |
| `WeekWarrior` | Week Warrior | Streak ≥ 7 días consecutivos |
| `RisingStar` | Rising Star | ELO alcanza ≥ 1200 |
| `SharpMind` | Sharp Mind | ELO alcanza ≥ 1500 |
| `Centurion` | Centurion | 100 attempts totales acumulados |
| `Perfectionist` | Perfectionist | 10 respuestas correctas consecutivas |
| `Brave` | Brave | Primera respuesta correcta en dificultad `Hard` |

---

## Entidad: `UserBadge`

**Ubicación**: `src/DevBrain.Domain/Entities/UserBadge.cs`

### Propiedades

| Propiedad | Tipo | Reglas |
|-----------|------|--------|
| `Id` | `Guid` | Generado al crear, inmutable |
| `UserId` | `Guid` | Requerido, inmutable — referencia al usuario |
| `Type` | `BadgeType` | Requerido, inmutable — tipo de badge otorgado |
| `EarnedAt` | `DateTimeOffset` | UTC del momento en que se otorgó, inmutable |

### Creación

- Factory method estático: `UserBadge.Create(Guid userId, BadgeType type)`
- Genera `Id = Guid.NewGuid()`
- Setea `EarnedAt = DateTimeOffset.UtcNow`
- Lanza `DomainException` si `userId == Guid.Empty`

### Invariantes

1. `UserId` nunca puede ser `Guid.Empty`
2. Una vez creado, ninguna propiedad es mutable
3. La validez del enum `BadgeType` está garantizada por el compilador (no se valida en runtime)

---

## Interfaz: `IBadgeRepository`

**Ubicación**: `src/DevBrain.Domain/Interfaces/IBadgeRepository.cs`

```csharp
namespace DevBrain.Domain.Interfaces;

public interface IBadgeRepository
{
    Task AddAsync(UserBadge badge);
    Task<IReadOnlyList<UserBadge>> GetByUserAsync(Guid userId);
    Task<bool> HasBadgeAsync(Guid userId, BadgeType type);
}
```

- `AddAsync`: persiste un nuevo `UserBadge`. No valida duplicados — esa responsabilidad es del servicio.
- `GetByUserAsync`: retorna todos los badges del usuario, ordenados por `EarnedAt` ascendente.
- `HasBadgeAsync`: retorna `true` si el usuario ya tiene ese badge.

---

## Record: `BadgeAwardContext`

**Ubicación**: `src/DevBrain.Domain/Services/BadgeAwardContext.cs`

Encapsula el estado observable después de un attempt, necesario para evaluar las condiciones de cada badge.

```csharp
namespace DevBrain.Domain.Services;

public record BadgeAwardContext(
    bool IsCorrect,
    Difficulty Difficulty,
    int TotalAttempts,
    int ConsecutiveCorrect,
    int CurrentStreak,
    int NewEloRating);
```

| Campo | Descripción |
|-------|-------------|
| `IsCorrect` | Si el attempt actual fue correcto |
| `Difficulty` | Dificultad del challenge intentado |
| `TotalAttempts` | Total de attempts del usuario **incluyendo el actual** |
| `ConsecutiveCorrect` | Cantidad de respuestas correctas seguidas al cierre del attempt actual. 0 si el actual fue incorrecto. |
| `CurrentStreak` | Streak después de registrar el attempt actual (valor retornado por `IStreakService.RecordAttemptAsync`) |
| `NewEloRating` | ELO del usuario después del attempt actual |

---

## Servicio: `BadgeAwardService`

**Tipo**: Servicio de dominio — lógica pura, sin estado, sin dependencias externas  
**Ubicación**: `src/DevBrain.Domain/Services/BadgeAwardService.cs`

### Interfaz

```csharp
namespace DevBrain.Domain.Services;

public interface IBadgeAwardService
{
    IReadOnlyList<BadgeType> EvaluateNewBadges(
        BadgeAwardContext context,
        IReadOnlyList<BadgeType> alreadyEarned);
}
```

### Lógica de evaluación

Por cada `BadgeType`, evalúa la condición. Si se cumple **y** el badge **no está** en `alreadyEarned`, lo incluye en el resultado. El resultado es la lista de badges nuevos a otorgar (puede ser vacía).

| BadgeType | Condición |
|-----------|-----------|
| `FirstBlood` | `context.IsCorrect == true` |
| `OnFire` | `context.CurrentStreak >= 3` |
| `WeekWarrior` | `context.CurrentStreak >= 7` |
| `RisingStar` | `context.NewEloRating >= 1200` |
| `SharpMind` | `context.NewEloRating >= 1500` |
| `Centurion` | `context.TotalAttempts >= 100` |
| `Perfectionist` | `context.ConsecutiveCorrect >= 10` |
| `Brave` | `context.IsCorrect == true && context.Difficulty == Difficulty.Hard` |

**Regla crítica**: si `alreadyEarned` contiene el badge, no se incluye en el resultado aunque se cumpla la condición.

### Registro en DI

Sin estado ni dependencias externas — se registra como `Singleton`:

```csharp
builder.Services.AddSingleton<IBadgeAwardService, BadgeAwardService>();
```

---

## Integración con `AttemptService`

`AttemptService` (en `DevBrain.Api.Services`) es quien orquesta el sistema. **Esta spec no modifica `AttemptService` directamente** — eso es responsabilidad de la spec de infraestructura (`ef-badge-repository.spec.md`) que agrega `IBadgeRepository` como dependencia y cierra el ciclo.

**Responsabilidades de `AttemptService` para badges (a implementar en la siguiente spec):**

1. Tras calcular `newEloRating` y `newStreak`, obtener `GetByUserAsync(userId)` para la lista de badges ya ganados.
2. Obtener `TotalAttempts` = `CountCorrectByUserAsync` + attempts incorrectos — **nota**: se agrega `CountAllByUserAsync(Guid userId)` a `IAttemptRepository` (ver abajo).
3. Calcular `ConsecutiveCorrect` desde `GetByUserAsync(userId)` ordenado por `OccurredAt` desc, contando desde el inicio mientras `IsCorrect == true`. Si el attempt actual fue incorrecto, `ConsecutiveCorrect = 0`.
4. Construir `BadgeAwardContext` y llamar `BadgeAwardService.EvaluateNewBadges`.
5. Para cada nuevo badge, crear `UserBadge.Create(userId, badgeType)` y persistirlo con `IBadgeRepository.AddAsync`.
6. Incluir los badges nuevos en el `AttemptResult`.

---

## Extensión requerida: `IAttemptRepository`

Se agrega un método para obtener el total de attempts del usuario (correctos + incorrectos):

```csharp
Task<int> CountAllByUserAsync(Guid userId);
```

**Ubicación**: `src/DevBrain.Domain/Interfaces/IAttemptRepository.cs`  
**Implementación**: `EFAttemptRepository` — `_db.Attempts.CountAsync(a => a.UserId == userId)`

---

## Tests

Los tests de `BadgeAwardService` son **unit tests puros** — sin DB, sin Redis, sin HTTP.

**Ubicación**: `tests/DevBrain.Domain.Tests/BadgeAwardServiceTests.cs`

### UserBadge.Create — escenarios

| Escenario | Resultado esperado |
|-----------|-------------------|
| userId válido, tipo válido | `UserBadge` creado con Id != Empty, EarnedAt ≈ UtcNow |
| userId == Guid.Empty | `DomainException` |
| EarnedAt es UTC | `EarnedAt.Offset == TimeSpan.Zero` |

### BadgeAwardService.EvaluateNewBadges — escenarios

**FirstBlood:**

| Escenario | Resultado |
|-----------|-----------|
| IsCorrect=true, alreadyEarned=[] | incluye `FirstBlood` |
| IsCorrect=false, alreadyEarned=[] | no incluye `FirstBlood` |
| IsCorrect=true, alreadyEarned=[FirstBlood] | no incluye `FirstBlood` (ya ganado) |

**OnFire / WeekWarrior:**

| Escenario | Resultado |
|-----------|-----------|
| streak=2 | ninguno de los dos |
| streak=3, alreadyEarned=[] | incluye `OnFire` |
| streak=7, alreadyEarned=[] | incluye `OnFire` y `WeekWarrior` |
| streak=7, alreadyEarned=[OnFire, WeekWarrior] | ninguno (ya ganados) |
| streak=7, alreadyEarned=[OnFire] | incluye solo `WeekWarrior` |

**RisingStar / SharpMind:**

| Escenario | Resultado |
|-----------|-----------|
| ELO=1199 | ninguno |
| ELO=1200, alreadyEarned=[] | incluye `RisingStar` |
| ELO=1500, alreadyEarned=[] | incluye `RisingStar` y `SharpMind` |
| ELO=1500, alreadyEarned=[RisingStar] | incluye solo `SharpMind` |

**Centurion:**

| Escenario | Resultado |
|-----------|-----------|
| TotalAttempts=99 | no incluye `Centurion` |
| TotalAttempts=100, alreadyEarned=[] | incluye `Centurion` |
| TotalAttempts=150, alreadyEarned=[Centurion] | no incluye `Centurion` |

**Perfectionist:**

| Escenario | Resultado |
|-----------|-----------|
| ConsecutiveCorrect=9 | no incluye `Perfectionist` |
| ConsecutiveCorrect=10, alreadyEarned=[] | incluye `Perfectionist` |
| ConsecutiveCorrect=15, alreadyEarned=[Perfectionist] | no incluye `Perfectionist` |

**Brave:**

| Escenario | Resultado |
|-----------|-----------|
| IsCorrect=true, Difficulty=Hard, alreadyEarned=[] | incluye `Brave` |
| IsCorrect=false, Difficulty=Hard, alreadyEarned=[] | no incluye `Brave` |
| IsCorrect=true, Difficulty=Medium, alreadyEarned=[] | no incluye `Brave` |
| IsCorrect=true, Difficulty=Hard, alreadyEarned=[Brave] | no incluye `Brave` |

**Múltiples badges simultáneos:**

| Escenario | Resultado |
|-----------|-----------|
| IsCorrect=true, streak=3, ELO=1200, TotalAttempts=1, ConsecutiveCorrect=1, Difficulty=Easy, alreadyEarned=[] | incluye `FirstBlood`, `OnFire`, `RisingStar` |
| Todos los badges ya ganados, condiciones todas cumplidas | lista vacía |

---

## Notas de implementación

- `BadgeAwardService` vive en `DevBrain.Domain` — cero dependencias externas, testeable en aislamiento
- `UserBadge` es una entidad sellada (`sealed`) con constructor privado y factory method `Create`
- Los valores de `BadgeType` deben ser persistidos en DB como `string` (no `int`) para facilitar migraciones futuras — esto se configura en `DevBrainDbContext` en la siguiente spec
- La próxima spec es `specs/infrastructure/ef-badge-repository.spec.md`, que cubre: tabla `UserBadges`, `EFBadgeRepository`, migración EF Core, actualización de `AttemptService`, y el endpoint `GET /users/me/badges`

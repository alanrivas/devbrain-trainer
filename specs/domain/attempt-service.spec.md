# Spec: AttemptService

**Tipo**: Servicio de aplicación (orquestador)  
**Ubicación**: `DevBrain.Api.Services`  
**Versión**: 1.0  

---

## Qué es

Servicio que centraliza toda la lógica de negocio al enviar un attempt:

1. Valida que el challenge existe
2. Crea y persiste el `Attempt`
3. Calcula el nuevo ELO del usuario con `IEloRatingService`
4. Actualiza el ELO del usuario en DB
5. Registra el streak del día con `IStreakService`
6. Retorna un resultado enriquecido con ELO y streak actualizados

El endpoint `POST /api/v1/challenges/{id}/attempt` delega toda la lógica a este servicio.

---

## Prerequisito — cambios en `User` y `IUserRepository`

El servicio necesita leer y actualizar el ELO del usuario. Actualmente `User` no tiene `EloRating`.

### Cambio 1 — `User` entity

Agregar `EloRating` con valor inicial 1000:

```csharp
public int EloRating { get; private set; } = 1000;

public void UpdateEloRating(int newRating)
{
    if (newRating < 100)
        throw new DomainException("ELO rating cannot be lower than 100.");
    EloRating = newRating;
}
```

Ubicación: `src/DevBrain.Domain/Entities/User.cs`

### Cambio 2 — `IUserRepository`

Agregar método de actualización:

```csharp
Task UpdateAsync(User user, CancellationToken cancellationToken = default);
```

Ubicación: `src/DevBrain.Domain/Interfaces/IUserRepository.cs`

### Cambio 3 — `EFUserRepository`

Implementar `UpdateAsync`:

```csharp
public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
{
    _context.Users.Update(user);
    await _context.SaveChangesAsync(cancellationToken);
}
```

### Cambio 4 — Migración EF Core

Agregar columna `EloRating` (int, default 1000) a la tabla `Users`:

```bash
dotnet ef migrations add AddEloRatingToUser --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api
dotnet ef database update --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api
```

---

## Interfaz del servicio

```csharp
namespace DevBrain.Api.Services;

public interface IAttemptService
{
    Task<AttemptResult> SubmitAsync(Guid challengeId, Guid userId, string userAnswer, int elapsedSecs);
}

public sealed record AttemptResult(
    Guid AttemptId,
    Guid ChallengeId,
    Guid UserId,
    string UserAnswer,
    bool IsCorrect,
    string CorrectAnswer,
    int ElapsedSeconds,
    string ChallengeTitle,
    DateTimeOffset OccurredAt,
    int NewEloRating,
    int NewStreak
);
```

Ubicación: `src/DevBrain.Api/Services/IAttemptService.cs`

---

## Implementación

**Clase**: `AttemptService`  
**Ubicación**: `src/DevBrain.Api/Services/AttemptService.cs`

### Flujo de `SubmitAsync`

```
1. challenge = await challengeRepository.GetByIdAsync(challengeId)
   → si null: throw ApplicationException("Challenge not found")

2. user = await userRepository.GetByIdAsync(userId)
   → si null: throw ApplicationException("User not found")

3. attempt = Attempt.Create(challengeId, userId, userAnswer, elapsedSecs, challenge)

4. await attemptRepository.AddAsync(attempt)

5. newEloRating = eloService.Calculate(
       user.EloRating, challenge.Difficulty, challenge.TimeLimitSecs,
       attempt.IsCorrect, elapsedSecs)

6. user.UpdateEloRating(newEloRating)
   await userRepository.UpdateAsync(user)

7. newStreak = await streakService.RecordAttemptAsync(userId, attempt.OccurredAt)

8. return new AttemptResult(...)
```

### Dependencias del constructor

```csharp
public AttemptService(
    IChallengeRepository challengeRepository,
    IAttemptRepository attemptRepository,
    IUserRepository userRepository,
    IEloRatingService eloRatingService,
    IStreakService streakService
)
```

---

## Cambios en el endpoint `POST /challenges/{id}/attempt`

El handler `PostAttempt` en `ChallengeEndpoints.cs` se simplifica: solo extrae los datos del request y delega a `IAttemptService`.

```csharp
private static async Task<IResult> PostAttempt(
    Guid id,
    CreateAttemptRequestDto request,
    IAttemptService attemptService,
    HttpContext httpContext
)
{
    // validaciones de request (userAnswer, elapsedSeconds) — igual que antes

    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var result = await attemptService.SubmitAsync(id, userId, request.UserAnswer.Trim(), request.ElapsedSeconds);

    var responseDto = new AttemptResponseDto(
        result.AttemptId, result.ChallengeId, result.UserId,
        result.UserAnswer, result.IsCorrect, result.CorrectAnswer,
        result.ElapsedSeconds, result.ChallengeTitle, result.OccurredAt.DateTime,
        result.NewEloRating, result.NewStreak
    );

    return Results.Created($"/api/v1/challenges/{id}/attempt/{result.AttemptId}", responseDto);
}
```

---

## Cambios en `AttemptResponseDto`

Agregar los dos campos nuevos:

```csharp
public record AttemptResponseDto(
    Guid AttemptId,
    Guid ChallengeId,
    Guid UserId,
    string UserAnswer,
    bool IsCorrect,
    string CorrectAnswer,
    int ElapsedSeconds,
    string ChallengeTitle,
    DateTime OccurredAt,
    int NewEloRating,    // nuevo
    int NewStreak        // nuevo
);
```

---

## Cambios en `GET /users/me/stats`

Reemplazar el placeholder de ELO con el valor real del usuario:

```csharp
// Antes:
EloRating: 1000,  // placeholder

// Después:
EloRating: user.EloRating,  // valor real de la DB
```

Y el streak real desde Redis:

```csharp
// Antes:
CurrentStreak: 0,  // placeholder

// Después:
CurrentStreak: await streakService.GetStreakAsync(userId),
```

---

## Registro en DI

```csharp
builder.Services.AddScoped<IAttemptService, AttemptService>();
```

En `Program.cs`. El `IEloRatingService` se registra como Singleton (stateless):

```csharp
builder.Services.AddSingleton<IEloRatingService, EloRatingService>();
```

---

## Escenarios de test (Api.Tests)

Tests en `tests/DevBrain.Api.Tests/PostAttemptEndpointTests.cs` — los existentes deben seguir pasando. Agregar:

| Escenario | Status | Resultado |
|-----------|--------|-----------|
| Correct answer → response incluye NewEloRating > 1000 | 201 | ELO subió |
| Incorrect answer → response incluye NewEloRating < 1000 | 201 | ELO bajó (o = 1000 si floor) |
| Correct answer → NewStreak = 1 (primer attempt del test) | 201 | Streak iniciado |
| GET /users/me/stats tras attempt correcto → eloRating real | 200 | ELO actualizado |
| GET /users/me/stats tras attempt → currentStreak > 0 | 200 | Streak real |

---

## Notas de implementación

- `IAttemptService` vive en `DevBrain.Api.Services` — es un servicio de aplicación, no de dominio
- `IStreakService` está en Infrastructure — el Api lo consume via DI (referencia cruzada válida porque Api ya referencia Infrastructure)
- Los tests de `PostAttemptEndpointTests` que verifican `NewEloRating` y `NewStreak` necesitan Redis corriendo (`localhost:6379`)
- El `CustomWebApplicationFactory` necesita registrar `IStreakService` y `IConnectionMultiplexer` para tests
- `IEloRatingService` (Domain) necesita agregarse como `using` en `AttemptService`

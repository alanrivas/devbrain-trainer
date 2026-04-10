# Spec: EFBadgeRepository + integración completa del sistema de badges

**Tipo**: Infraestructura + integración en AttemptService + endpoint API  
**Ubicación**: `DevBrain.Infrastructure` + `DevBrain.Api`  
**Versión**: 1.0  
**Depende de**: `specs/gamification/badges.spec.md` (ya implementada)

---

## Qué cubre esta spec

Cierra el ciclo del sistema de badges implementando las piezas que faltaban tras la spec de dominio:

1. Configuración de la tabla `user_badges` en `DevBrainDbContext`
2. `EFBadgeRepository` — implementación de `IBadgeRepository` con EF Core
3. Migración EF Core: `AddUserBadgesTable`
4. Actualización de `AttemptService` — evalúa y persiste badges tras cada attempt
5. Actualización de `AttemptResult` y `AttemptResponseDto` — incluye `NewBadges`
6. Endpoint `GET /api/v1/users/me/badges` — lista de badges del usuario autenticado
7. Registro en DI de `IBadgeRepository`, `IBadgeAwardService`

---

## 1 — DevBrainDbContext: tabla `user_badges`

### DbSet

```csharp
public DbSet<UserBadge> UserBadges => Set<UserBadge>();
```

### Configuración en `OnModelCreating`

```csharp
modelBuilder.Entity<UserBadge>()
    .ToTable("user_badges")
    .HasKey(b => b.Id);

modelBuilder.Entity<UserBadge>()
    .Property(b => b.Id)
    .ValueGeneratedNever();

modelBuilder.Entity<UserBadge>()
    .Property(b => b.UserId)
    .IsRequired();

modelBuilder.Entity<UserBadge>()
    .Property(b => b.Type)
    .HasConversion<string>()  // persiste "FirstBlood", "OnFire", etc.
    .HasMaxLength(50)
    .IsRequired();

modelBuilder.Entity<UserBadge>()
    .Property(b => b.EarnedAt)
    .IsRequired();

// FK sin navigation property en UserBadge
modelBuilder.Entity<UserBadge>()
    .HasOne<User>()
    .WithMany()
    .HasForeignKey(b => b.UserId)
    .OnDelete(DeleteBehavior.Cascade);

// Índice compuesto para lookup por usuario + tipo (unicidad lógica)
modelBuilder.Entity<UserBadge>()
    .HasIndex(b => b.UserId);

modelBuilder.Entity<UserBadge>()
    .HasIndex(new[] { nameof(UserBadge.UserId), nameof(UserBadge.Type) })
    .IsUnique();
```

**Nota sobre unicidad**: el índice único `(UserId, Type)` garantiza a nivel de DB que un usuario no puede tener el mismo badge dos veces. `BadgeAwardService` ya lo previene en dominio; la DB es la segunda línea de defensa.

---

## 2 — EFBadgeRepository

**Clase**: `EFBadgeRepository`  
**Ubicación**: `src/DevBrain.Infrastructure/Persistence/EFBadgeRepository.cs`  
**Implementa**: `IBadgeRepository`

```csharp
namespace DevBrain.Infrastructure.Persistence;

public sealed class EFBadgeRepository : IBadgeRepository
{
    private readonly DevBrainDbContext _context;

    public EFBadgeRepository(DevBrainDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserBadge badge)
    {
        _context.UserBadges.Add(badge);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<UserBadge>> GetByUserAsync(Guid userId)
    {
        return await _context.UserBadges
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.EarnedAt)
            .ToListAsync();
    }

    public async Task<bool> HasBadgeAsync(Guid userId, BadgeType type)
    {
        return await _context.UserBadges
            .AnyAsync(b => b.UserId == userId && b.Type == type);
    }
}
```

---

## 3 — Migración EF Core

**Nombre**: `AddUserBadgesTable`  
**Generar con**:

```bash
dotnet ef migrations add AddUserBadgesTable --project src/DevBrain.Infrastructure --startup-project src/DevBrain.Api
```

La migración crea la tabla `user_badges` con las columnas: `Id`, `UserId`, `Type` (string), `EarnedAt`, FK a `users`.

**No aplicar a producción en esta spec** — la spec solo define la migración. La aplicación a Neon es manual/CI.

---

## 4 — AttemptService: integración de badges

### Cambios a `IAttemptService.cs`

`AttemptResult` añade `NewBadges`:

```csharp
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
    int NewStreak,
    IReadOnlyList<string> NewBadges   // nombres de badges recién otorgados, ej: ["FirstBlood"]
);
```

### Cambios a `AttemptService.cs`

Se agregan dos dependencias nuevas: `IBadgeRepository` e `IBadgeAwardService`.

**Lógica adicional al final de `SubmitAsync`**, después de `RecordAttemptAsync`:

```
1. allAttempts = await _attemptRepository.GetByUserAsync(userId)
   // retorna desc por OccurredAt — el attempt recién añadido es el primero

2. totalAttempts = allAttempts.Count

3. consecutiveCorrect:
   if (!attempt.IsCorrect):
       consecutiveCorrect = 0
   else:
       count = 0
       foreach a in allAttempts:   // desc por OccurredAt
           if a.IsCorrect: count++
           else: break
       consecutiveCorrect = count

4. alreadyEarned = (await _badgeRepository.GetByUserAsync(userId))
                       .Select(b => b.Type)
                       .ToList()

5. context = new BadgeAwardContext(
       IsCorrect: attempt.IsCorrect,
       Difficulty: challenge.Difficulty,
       TotalAttempts: totalAttempts,
       ConsecutiveCorrect: consecutiveCorrect,
       CurrentStreak: newStreak,
       NewEloRating: newEloRating)

6. newBadgeTypes = _badgeAwardService.EvaluateNewBadges(context, alreadyEarned)

7. foreach badgeType in newBadgeTypes:
       await _badgeRepository.AddAsync(UserBadge.Create(userId, badgeType))

8. return new AttemptResult(..., NewBadges: newBadgeTypes.Select(b => b.ToString()).ToList())
```

---

## 5 — AttemptResponseDto y endpoint

### Cambio a `AttemptResponseDto`

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
    int NewEloRating,
    int NewStreak,
    string[] NewBadges   // nuevo campo
);
```

### Cambio en `ChallengeEndpoints.PostAttempt`

Agregar `NewBadges: result.NewBadges.ToArray()` al construir `AttemptResponseDto`.

---

## 6 — Endpoint: GET /api/v1/users/me/badges

**Ruta**: `GET /api/v1/users/me/badges`  
**Auth**: `RequireAuthorization()` — Bearer JWT  
**Handler**: en `UserEndpoints.MapUserEndpoints`

### Response 200 OK

```json
[
  {
    "type": "FirstBlood",
    "earnedAt": "2026-04-10T12:00:00Z"
  }
]
```

Array vacío `[]` si el usuario no tiene badges.

### DTO de respuesta

```csharp
// src/DevBrain.Api/DTOs/UserBadgeResponseDto.cs
public record UserBadgeResponseDto(string Type, DateTimeOffset EarnedAt);
```

### Lógica del handler

```
1. Extraer userId del JWT (Claims.NameIdentifier)
2. Verificar que el usuario existe en DB — 404 si no
3. badges = await _badgeRepository.GetByUserAsync(userId)
4. return Results.Ok(badges.Select(b => new UserBadgeResponseDto(b.Type.ToString(), b.EarnedAt)))
```

### Status codes

| Status | Condición |
|--------|-----------|
| 200 | Usuario existe (lista puede ser vacía) |
| 401 | Sin Bearer token |
| 404 | JWT válido pero userId no existe en DB |

---

## 7 — Registro en DI (`Program.cs`)

```csharp
builder.Services.AddScoped<IBadgeRepository, EFBadgeRepository>();
builder.Services.AddSingleton<IBadgeAwardService, BadgeAwardService>();
```

---

## 8 — Tests

### EFBadgeRepositoryTests (DevBrain.Infrastructure.Tests)

**Ubicación**: `tests/DevBrain.Infrastructure.Tests/EFBadgeRepositoryTests.cs`  
**Patrón**: in-memory DB (`UseInMemoryDatabase`), igual que `EFAttemptRepositoryTests`

| Escenario | Resultado esperado |
|-----------|-------------------|
| `AddAsync` con badge válido | badge persiste, recuperable |
| `GetByUserAsync` sin badges | retorna lista vacía |
| `GetByUserAsync` con dos badges | retorna ambos, ordenados por EarnedAt ASC |
| `GetByUserAsync` con dos usuarios | cada usuario solo ve sus propios badges |
| `HasBadgeAsync` badge no ganado | `false` |
| `HasBadgeAsync` badge ganado | `true` |

**Setup helper**: cada test crea su propio DbContext in-memory con `Guid.NewGuid()` como nombre, crea un `User` en la DB antes de insertar `UserBadge` (por la FK).

### GetUserBadgesTests (DevBrain.Api.Tests)

**Ubicación**: `tests/DevBrain.Api.Tests/GetUserBadgesTests.cs`  
**Patrón**: `CustomWebApplicationFactory` + `IAsyncLifetime`, igual que los otros tests de API

| Escenario | Resultado esperado |
|-----------|-------------------|
| Sin Bearer token | 401 |
| Usuario autenticado sin badges | 200 con `[]` |
| Usuario autenticado con 2 badges en DB | 200 con array de 2 elementos, campo `type` correcto |
| Campo `earnedAt` en respuesta | formato ISO 8601 UTC |

### POST /attempt — campo NewBadges (PostAttemptEndpointTests)

Agregar 2 tests al archivo existente `PostAttemptEndpointTests.cs`:

| Escenario | Resultado esperado |
|-----------|-------------------|
| Primera respuesta correcta | `newBadges` contiene `"FirstBlood"` |
| Respuesta incorrecta | `newBadges` es array vacío `[]` |

---

## Notas de implementación

- `EFBadgeRepository` usa `UseInMemoryDatabase` en tests (sin Redis, sin Postgres real) — mismo patrón que EFAttemptRepository
- `BadgeType` se almacena como `string` en DB — facilita queries legibles y migraciones futuras sin cambios de datos
- El índice único `(UserId, Type)` en DB es la segunda garantía de no-duplicados; `BadgeAwardService` es la primera
- `AttemptService` llama `GetByUserAsync` una sola vez para obtener todos los attempts y contar consecutivos — no hay N+1
- La migración solo se genera localmente y se aplica a Neon manualmente (como las anteriores)

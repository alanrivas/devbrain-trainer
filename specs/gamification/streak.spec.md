# Spec: StreakService

**Tipo**: Servicio de infraestructura (depende de Redis)  
**Ubicación**: `DevBrain.Infrastructure`  
**Versión**: 1.0  

---

## Qué es

Servicio que mantiene el streak diario de un usuario — cuántos días consecutivos ha hecho al menos un attempt.

El streak se incrementa cuando el usuario hace un attempt en un día distinto al último que registró. Si pasa más de un día sin attempts, el streak se rompe y vuelve a 1.

---

## Reglas de negocio

1. **Primer attempt del día**: si el usuario no tiene streak o es el primer attempt del día UTC, incrementa el contador.
2. **Mismo día**: si ya hizo un attempt hoy (UTC), el streak no cambia.
3. **Streak roto**: si pasó más de 1 día desde el último attempt, el streak vuelve a 1.
4. **Unidad de tiempo**: día en UTC. No hay zona horaria del usuario.

---

## Claves Redis

```
streak:{userId}:count      →  int (contador de días consecutivos)
streak:{userId}:last_date  →  string "yyyy-MM-dd" (último día UTC con attempt)
```

Ambas claves con **TTL de 48 horas** — si el usuario no hace nada en 2 días, las claves expiran y el streak se pierde naturalmente.

---

## Interfaz del servicio

```csharp
namespace DevBrain.Infrastructure.Services;

public interface IStreakService
{
    /// <summary>
    /// Registra un attempt y actualiza el streak. Retorna el nuevo valor del streak.
    /// </summary>
    Task<int> RecordAttemptAsync(Guid userId, DateTimeOffset occurredAt);

    /// <summary>
    /// Retorna el streak actual del usuario. 0 si no tiene streak activo.
    /// </summary>
    Task<int> GetStreakAsync(Guid userId);
}
```

**Ubicación**: `src/DevBrain.Infrastructure/Services/IStreakService.cs`

---

## Implementación

**Clase**: `RedisStreakService`  
**Ubicación**: `src/DevBrain.Infrastructure/Services/RedisStreakService.cs`

### Lógica de `RecordAttemptAsync`

```
today = occurredAt.UtcDateTime date ("yyyy-MM-dd")
lastDate = GET streak:{userId}:last_date
count = GET streak:{userId}:count ?? 0

if lastDate == null:
    count = 1                          // primer attempt ever
elif today == lastDate:
    return count                       // mismo día, no cambia
elif today == lastDate + 1 día:
    count += 1                         // día consecutivo
else:
    count = 1                          // streak roto

SET streak:{userId}:last_date = today  (TTL 48h)
SET streak:{userId}:count = count      (TTL 48h)
return count
```

### Lógica de `GetStreakAsync`

```
count = GET streak:{userId}:count ?? 0
return count
```

---

## Registro en DI

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379")
);
builder.Services.AddScoped<IStreakService, RedisStreakService>();
```

En `Program.cs`.

---

## Tests

Los tests de `StreakService` son de integración contra Redis real (no mocks). Se ejecutan contra `localhost:6379`.

**Ubicación**: `tests/DevBrain.Infrastructure.Tests/RedisStreakServiceTests.cs`

Cada test usa un `userId` único (`Guid.NewGuid()`) para evitar colisiones entre tests paralelos.

### Escenarios

| Escenario | Resultado esperado |
|-----------|-------------------|
| GetStreak sin attempts previos | 0 |
| Primer attempt | streak = 1 |
| Segundo attempt el mismo día | streak sigue en 1 |
| Attempt al día siguiente | streak = 2 |
| Attempt tras 2 días de pausa | streak = 1 (reset) |
| Attempt tras 5 días de pausa | streak = 1 (reset) |
| 3 días consecutivos | streak = 3 |
| GetStreak después de RecordAttempt | coincide con valor retornado |

---

## Notas de implementación

- Usar `IDatabase` de `StackExchange.Redis` (`multiplexer.GetDatabase()`)
- Los tests limpian sus propias claves con `KeyDeleteAsync` en `DisposeAsync`
- El `CustomWebApplicationFactory` no necesita cambios — los tests de streak son en `Infrastructure.Tests`, no en `Api.Tests`
- `IStreakService` vive en `Infrastructure` (no en `Domain`) porque depende de Redis, una tecnología externa

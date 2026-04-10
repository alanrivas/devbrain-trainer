# Spec: EloRatingService

**Tipo**: Servicio de dominio (lógica pura, sin dependencias externas)  
**Ubicación**: `DevBrain.Domain`  
**Versión**: 1.0  

---

## Qué es

Servicio de cálculo de rating ELO adaptado para aprendizaje individual.

En el ELO clásico (ajedrez), dos jugadores compiten entre sí. Aquí el "oponente" es el **challenge**: un challenge correcto equivale a ganarle al challenge, uno incorrecto equivale a perder. La dificultad del challenge y el tiempo empleado influyen en cuántos puntos se ganan o pierden.

El rating es **por usuario** y **global** (una sola cifra, no por categoría — eso es Fase G).

---

## Fórmula ELO adaptada

### Constantes

| Constante | Valor | Descripción |
|-----------|-------|-------------|
| `K_BASE` | 32 | Factor K estándar (máximo cambio posible por attempt) |
| `CHALLENGE_RATING_EASY` | 800 | Rating ficticio del challenge Easy |
| `CHALLENGE_RATING_MEDIUM` | 1200 | Rating ficticio del challenge Medium |
| `CHALLENGE_RATING_HARD` | 1600 | Rating ficticio del challenge Hard |
| `INITIAL_RATING` | 1000 | Rating inicial de todo usuario nuevo |

### Paso 1 — Probabilidad esperada de ganar

```
expected = 1 / (1 + 10^((challengeRating - userRating) / 400))
```

Donde `challengeRating` depende de la dificultad del challenge (`CHALLENGE_RATING_EASY/MEDIUM/HARD`).

### Paso 2 — Score del intento

- Si `isCorrect = true`: `score = 1.0`
- Si `isCorrect = false`: `score = 0.0`

### Paso 3 — Modificador por tiempo

Penaliza/bonifica según qué tan rápido resolvió el challenge en relación al límite de tiempo:

```
timeRatio = elapsedSecs / timeLimitSecs          // clampado a [0.0, 1.0]
timeModifier = 1.0 + (1.0 - timeRatio) * 0.25   // rango: [1.0, 1.25]
```

- Resolvió en 0% del tiempo → modifier = 1.25 (bonus máximo)
- Resolvió exactamente en el límite → modifier = 1.0 (sin bonus)
- Solo aplica si `isCorrect = true`; si es incorrecto, `timeModifier = 1.0`

### Paso 4 — Delta ELO

```
delta = K_BASE * (score - expected) * timeModifier
```

Redondear `delta` a entero con `Math.Round(delta)` (round half-away-from-zero → `MidpointRounding.AwayFromZero`).

### Paso 5 — Nuevo rating

```
newRating = userRating + delta
```

**Floor**: el rating nunca puede bajar de 100. `newRating = Math.Max(100, newRating)`.

---

## Interfaz del servicio

```csharp
namespace DevBrain.Domain.Services;

public interface IEloRatingService
{
    /// <summary>
    /// Calcula el nuevo rating ELO del usuario tras un attempt.
    /// </summary>
    /// <param name="userRating">Rating actual del usuario (mínimo 100)</param>
    /// <param name="difficulty">Dificultad del challenge</param>
    /// <param name="timeLimitSecs">Tiempo límite del challenge en segundos</param>
    /// <param name="isCorrect">Si el intento fue correcto</param>
    /// <param name="elapsedSecs">Tiempo empleado en segundos</param>
    /// <returns>Nuevo rating ELO del usuario</returns>
    int Calculate(int userRating, Difficulty difficulty, int timeLimitSecs, bool isCorrect, int elapsedSecs);
}
```

**Ubicación**: `src/DevBrain.Domain/Services/IEloRatingService.cs`

---

## Implementación

**Clase**: `EloRatingService`  
**Ubicación**: `src/DevBrain.Domain/Services/EloRatingService.cs`

```csharp
namespace DevBrain.Domain.Services;

public sealed class EloRatingService : IEloRatingService
{
    private const int K_BASE = 32;
    private const int CHALLENGE_RATING_EASY = 800;
    private const int CHALLENGE_RATING_MEDIUM = 1200;
    private const int CHALLENGE_RATING_HARD = 1600;
    private const int MIN_RATING = 100;

    public int Calculate(int userRating, Difficulty difficulty, int timeLimitSecs, bool isCorrect, int elapsedSecs)
    {
        // ... implementar según fórmula
    }
}
```

**Ubicación**: `src/DevBrain.Domain/Services/EloRatingService.cs`

---

## Registro en DI

El servicio es puro (sin estado, sin DB), se registra como `Singleton`:

```csharp
builder.Services.AddSingleton<IEloRatingService, EloRatingService>();
```

En `Program.cs`.

---

## Escenarios de test esperados

### Correctness básica

| Escenario | Resultado esperado |
|-----------|-------------------|
| Correcto, Easy, user=1000, elapsed=0, limit=60 | rating sube (delta positivo) |
| Correcto, Medium, user=1000, elapsed=0, limit=60 | rating sube más que Easy (challenge más difícil) |
| Correcto, Hard, user=1000, elapsed=0, limit=60 | rating sube más que Medium |
| Incorrecto, Easy, user=1000 | rating baja |
| Incorrecto, Hard, user=1000 | rating baja menos que Easy (era más difícil, menos esperado ganar) |

### Time modifier

| Escenario | Resultado esperado |
|-----------|-------------------|
| Correcto, elapsed=0 (instantáneo) | mayor delta que elapsed=timeLimitSecs |
| Correcto, elapsed=timeLimitSecs (justo en el límite) | timeModifier = 1.0 (sin bonus) |
| Incorrecto, elapsed=0 | mismo delta que elapsed=timeLimitSecs (timeModifier no aplica) |

### Floor de rating

| Escenario | Resultado esperado |
|-----------|-------------------|
| userRating=100, incorrecto, Easy | newRating = 100 (no baja del floor) |
| userRating=105, incorrecto, Hard | puede quedar en 100 si delta grande |

### Valores de delta específicos (para verificar la fórmula exacta)

Calculados manualmente para anclar la implementación:

**Caso: user=1000, Easy, correcto, elapsed=0, limit=60**
- challengeRating = 800
- expected = 1 / (1 + 10^((800-1000)/400)) = 1 / (1 + 10^(-0.5)) ≈ 0.7597
- score = 1.0
- timeRatio = 0/60 = 0.0 → timeModifier = 1.0 + 1.0 * 0.25 = 1.25
- delta = 32 * (1.0 - 0.7597) * 1.25 ≈ 32 * 0.2403 * 1.25 ≈ 9.61 → redondeado = **10**
- newRating = 1010

**Caso: user=1000, Hard, incorrecto, elapsed=30, limit=60**
- challengeRating = 1600
- expected = 1 / (1 + 10^((1600-1000)/400)) = 1 / (1 + 10^1.5) ≈ 0.0306
- score = 0.0
- timeModifier = 1.0 (incorrecto)
- delta = 32 * (0.0 - 0.0306) * 1.0 ≈ -0.979 → redondeado = **-1**
- newRating = 999

**Caso: user=100, Easy, incorrecto**
- Cualquier delta negativo → newRating = 100 (floor)

---

## Notas de implementación

- La clase vive en `DevBrain.Domain` — sin referencias a Infrastructure ni Api
- Tests en `DevBrain.Domain.Tests` — pure unit tests, sin DB ni HTTP
- No persiste nada — solo calcula. La persistencia del nuevo rating es responsabilidad de `AttemptService` (Fase D)
- `elapsedSecs` puede ser 0 (legítimo) — no dividir por cero en timeRatio (timeLimitSecs siempre ≥ 30)

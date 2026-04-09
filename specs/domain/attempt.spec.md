# Spec: Attempt (Intento)

**Tipo**: Entidad de dominio  
**Ubicación**: `DevBrain.Domain`  
**Versión**: 1.0  

---

## Qué es

Un `Attempt` representa el intento de un usuario sobre un `Challenge`.
Se crea cuando el usuario envía una respuesta, y registra si fue correcta y cuánto tardó.

---

## Propiedades

| Propiedad       | Tipo             | Reglas                                               |
|-----------------|------------------|------------------------------------------------------|
| `Id`            | `Guid`           | Generado al crear, inmutable                         |
| `ChallengeId`   | `Guid`           | Requerido, no puede ser `Guid.Empty`                 |
| `UserAnswer`    | `string`         | Requerido, no vacío                                  |
| `IsCorrect`     | `bool`           | Calculado al crear según la respuesta del challenge  |
| `ElapsedSecs`   | `int`            | Mayor a 0, no puede superar `TimeLimitSecs` del challenge |
| `OccurredAt`    | `DateTimeOffset` | Asignado al crear, inmutable                         |

---

## Comportamientos

### Creación

- Se crea con `Attempt.Create(challengeId, userAnswer, elapsedSecs, challenge)`
- `IsCorrect` se determina internamente llamando a `challenge.IsCorrectAnswer(userAnswer)`
- `OccurredAt` es asignado internamente con `DateTimeOffset.UtcNow`
- Si alguna validación falla, lanza `DomainException`

---

## Invariantes (reglas que nunca se rompen)

1. `ChallengeId` no puede ser `Guid.Empty`
2. `UserAnswer` no puede ser vacío o solo espacios
3. `ElapsedSecs` debe ser mayor a 0
4. `ElapsedSecs` no puede superar `TimeLimitSecs` del challenge
5. `Id` y `OccurredAt` no cambian después de la creación

---

## Qué NO es esta entidad

- No conoce al usuario (eso vendrá con autenticación en una fase posterior)
- No acumula intentos ni calcula estadísticas (eso es responsabilidad de un servicio)
- No persiste nada por sí mismo (eso es responsabilidad del repositorio)

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| Crear con todos los campos válidos y respuesta correcta | OK — `IsCorrect = true` |
| Crear con todos los campos válidos y respuesta incorrecta | OK — `IsCorrect = false` |
| Crear con `ChallengeId = Guid.Empty` | `DomainException` |
| Crear con `UserAnswer` vacío | `DomainException` |
| Crear con `ElapsedSecs = 0` | `DomainException` |
| Crear con `ElapsedSecs` mayor al `TimeLimitSecs` del challenge | `DomainException` |
| `Id` generado no es `Guid.Empty` | OK |
| `OccurredAt` está asignado | OK |

# Spec: Challenge (Desafío)

**Tipo**: Entidad de dominio  
**Ubicación**: `DevBrain.Domain`  
**Versión**: 1.0  

---

## Qué es

Un `Challenge` es un problema técnico que el usuario resuelve dentro de la app.
Es la unidad central de entrenamiento.

---

## Propiedades

| Propiedad       | Tipo                | Reglas                                      |
|-----------------|---------------------|---------------------------------------------|
| `Id`            | `Guid`              | Generado al crear, inmutable                |
| `Title`         | `string`            | Requerido, entre 5 y 100 caracteres         |
| `Description`   | `string`            | Requerido, no vacío                         |
| `Category`      | `ChallengeCategory` | Valor del enum, requerido                   |
| `Difficulty`    | `Difficulty`        | Easy / Medium / Hard, requerido             |
| `CorrectAnswer` | `string`            | Requerido, no vacío                         |
| `TimeLimitSecs` | `int`               | Entre 30 y 300 segundos                     |
| `CreatedAt`     | `DateTimeOffset`    | Asignado al crear, inmutable                |

---

## Comportamientos

### Creación

- Se crea con `Challenge.Create(title, description, category, difficulty, answer, timeLimit)`
- Valida todas las propiedades al crearse — si alguna es inválida, lanza `DomainException`
- El `Id` y `CreatedAt` son asignados internamente, el caller no los provee

### Verificación de respuesta

- `bool IsCorrectAnswer(string attempt)` — compara ignorando mayúsculas y espacios extremos
- Retorna `true` solo si `attempt` coincide con `CorrectAnswer`

---

## Invariantes (reglas que nunca se rompen)

1. Un `Challenge` no puede existir sin título, descripción ni respuesta correcta
2. El tiempo límite nunca puede ser menor a 30 ni mayor a 300 segundos
3. `Id` y `CreatedAt` no pueden cambiar después de la creación

---

## Qué NO es esta entidad

- No gestiona el historial de intentos (eso es `Attempt`)
- No conoce al usuario que lo resuelve
- No se valida contra base de datos (eso es responsabilidad del repositorio)

---

## Enums relacionados

```
// Solo como referencia de valores esperados, no es implementación
ChallengeCategory: Sql, CodeLogic, Architecture, DevOps, WorkingMemory
Difficulty: Easy, Medium, Hard
```

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| Crear con todos los campos válidos | OK — objeto creado |
| Crear con título vacío | `DomainException` |
| Crear con título de 4 caracteres | `DomainException` |
| Crear con `TimeLimitSecs = 20` | `DomainException` |
| Crear con `TimeLimitSecs = 400` | `DomainException` |
| Crear con descripción vacía | `DomainException` |
| Crear con respuesta correcta vacía | `DomainException` |
| `IsCorrectAnswer` con respuesta exacta | `true` |
| `IsCorrectAnswer` con diferente capitalización | `true` |
| `IsCorrectAnswer` con respuesta incorrecta | `false` |

# Spec: Challenge Types — Multiple Choice Support

**Tipo**: Domain Entity Change + Infrastructure Migration + API DTO Update  
**Ubicación**:
- `src/DevBrain.Domain/Enums/ChallengeType.cs` (nuevo)
- `src/DevBrain.Domain/Entities/Challenge.cs` (update)
- `src/DevBrain.Infrastructure/Persistence/DevBrainDbContext.cs` (update)
- `src/DevBrain.Api/Endpoints/ChallengesEndpoints.cs` (update DTOs)
**Versión**: 1.0

---

## Qué es

Extiende la entidad `Challenge` para soportar un segundo tipo de ejercicio: **Multiple Choice**. Un challenge de este tipo incluye una lista fija de opciones (2-4) y el usuario selecciona una en lugar de escribir texto libre. El tipo `OpenText` es el comportamiento existente y no se altera.

---

## Enum nuevo: `ChallengeType`

```
ChallengeType:
  OpenText      = 0   ← comportamiento actual, valor por defecto
  MultipleChoice = 1
```

---

## Propiedades nuevas en `Challenge`

| Propiedad | Tipo | Reglas |
|-----------|------|--------|
| `Type` | `ChallengeType` | Inmutable, asignado en factory. Default `OpenText` |
| `Options` | `IReadOnlyList<string>` | Vacía para OpenText. 2-4 elementos para MultipleChoice |

Las propiedades existentes (`Id`, `Title`, `Description`, `Category`, `Difficulty`, `CorrectAnswer`, `TimeLimitSecs`, `CreatedAt`) no cambian.

---

## Comportamientos

### 1. `Challenge.Create()` — sin cambios

El factory method existente queda intacto. Siempre produce un challenge de tipo `OpenText` con `Options` vacía. No se modifica su firma ni sus tests existentes.

### 2. `Challenge.CreateMultipleChoice()` — nuevo factory method

Firma:
```csharp
Challenge.CreateMultipleChoice(
    string title,
    string description,
    ChallengeCategory category,
    Difficulty difficulty,
    string correctAnswer,
    int timeLimitSecs,
    IEnumerable<string> options)
```

Validaciones adicionales (además de las existentes en `Create`):
- `options` debe tener entre 2 y 4 elementos
- Ninguna opción puede ser vacía o solo whitespace
- No puede haber opciones duplicadas (comparación case-insensitive)
- `correctAnswer` debe coincidir (case-insensitive) con exactamente una de las opciones

Resultado: `Type = MultipleChoice`, `Options` contiene las opciones trimmeadas.

### 3. `IsCorrectAnswer()` — sin cambios

Sigue comparando `attempt.Trim()` contra `CorrectAnswer` con `OrdinalIgnoreCase`. Para MultipleChoice, el frontend envía el texto exacto de la opción seleccionada, que coincide con `CorrectAnswer`.

### 4. `CreateForSeeding()` — actualización

Agregar parámetros opcionales `ChallengeType type = ChallengeType.OpenText` y `IEnumerable<string>? options = null` para soportar seed data con challenges de tipo MultipleChoice. Sin breaking changes en llamadas existentes.

---

## Infraestructura: Migración de base de datos

### Nuevas columnas en tabla `challenges`

| Columna | Tipo SQL | Nullable | Default |
|---------|----------|----------|---------|
| `Type` | `integer` | NO | `0` (OpenText) |
| `Options` | `text` | YES | `NULL` |

`Options` se almacena como string pipe-separated (`"A|B|C|D"`). EF Core usa `HasConversion` para serializar/deserializar automáticamente.

### Nombre de la migración
`AddChallengeTypeAndOptions`

### Cambios en seed data

Agregar 3 challenges nuevos de tipo `MultipleChoice` al `HasData` existente, con GUIDs fijos. Mantener los 10 challenges de `OpenText` sin cambios.

Ejemplo de categorías para los nuevos challenges:
- 1 challenge de `CodeLogic` (complejidad algorítmica: O(n), O(n²), O(log n), O(n log n))
- 1 challenge de `Sql` (tipo de JOIN: INNER, LEFT, RIGHT, FULL)
- 1 challenge de `Architecture` (patrón de diseño)

---

## API: Actualización de DTOs

### `ChallengeDto` — campos nuevos

```typescript
// Representación en el response JSON:
{
  "id": "uuid",
  "title": "string",
  "description": "string",
  "category": "Sql|CodeLogic|...",
  "difficulty": "Easy|Medium|Hard",
  "timeLimitSecs": 60,
  "type": "OpenText|MultipleChoice",   // ← nuevo
  "options": ["A", "B", "C", "D"]      // ← nuevo, array vacío para OpenText
}
```

- `type`: nombre del enum como string (no el valor numérico)
- `options`: siempre presente; array vacío `[]` para OpenText, array con elementos para MultipleChoice

### Endpoints que devuelven `ChallengeDto`

Ambos endpoints ya existentes retornan el DTO actualizado:
- `GET /api/v1/challenges` (lista paginada)
- `GET /api/v1/challenges/{id}` (detalle)

`POST /api/v1/challenges/{id}/attempt` no cambia — sigue recibiendo `userAnswer: string` y devolviendo `AttemptResponseDto` sin modificaciones.

---

## Invariantes

1. Un challenge `OpenText` siempre tiene `Options` vacía.
2. Un challenge `MultipleChoice` siempre tiene entre 2 y 4 opciones no vacías y únicas.
3. Para un challenge `MultipleChoice`, `CorrectAnswer` siempre es igual (case-insensitive) a una de las opciones.
4. `Challenge.Create()` siempre produce `Type = OpenText` — no acepta opciones.
5. El tipo de un challenge es inmutable tras la creación.
6. `IsCorrectAnswer()` no tiene lógica distinta según el tipo; la responsabilidad de enviar la opción correcta es del frontend.

---

## Qué NO incluye esta spec

- No incluye endpoint para crear challenges vía API (sigue siendo solo seed data)
- No incluye validación en `POST /attempt` sobre si la respuesta es una opción válida (el backend acepta cualquier string)
- No incluye soporte para más de 4 opciones (post-MVP)
- No modifica el cálculo de ELO, streak ni badges
- No cambia `AttemptForm.tsx` (eso es la spec de frontend Phase 4.10)

---

## Escenarios de test esperados

### Domain Tests — `Challenge` entity con MultipleChoice

| Escenario | Resultado |
|-----------|-----------|
| `CreateMultipleChoice` con 4 opciones válidas y correctAnswer en opciones | OK — `Type = MultipleChoice`, `Options.Count = 4` |
| `CreateMultipleChoice` con 2 opciones (mínimo válido) | OK — `Options.Count = 2` |
| `CreateMultipleChoice` con 1 opción | `DomainException` |
| `CreateMultipleChoice` con 5 opciones | `DomainException` |
| `CreateMultipleChoice` con opción vacía en la lista | `DomainException` |
| `CreateMultipleChoice` con opción solo whitespace | `DomainException` |
| `CreateMultipleChoice` con opciones duplicadas (case-insensitive) | `DomainException` |
| `CreateMultipleChoice` con `correctAnswer` que no está en options | `DomainException` |
| `Create()` existente sigue produciendo `Type = OpenText` | `Options` está vacía |
| `IsCorrectAnswer` con opción correcta en challenge MultipleChoice | `true` |
| `IsCorrectAnswer` con opción incorrecta en challenge MultipleChoice | `false` |

**Total estimado**: ~11 tests nuevos en `DevBrain.Domain.Tests`

### Infrastructure Tests — migración y EF

| Escenario | Resultado |
|-----------|-----------|
| DbContext configura columna `Type` (int, not null, default 0) | Columna presente en schema |
| DbContext configura columna `Options` (text, nullable) | Columna presente en schema |
| Guardar challenge `MultipleChoice` y recuperarlo mantiene `Options` intacta | `Options` igual tras round-trip |
| Guardar challenge `OpenText` y recuperarlo tiene `Options` vacía | `Options.Count = 0` |
| Seed data incluye al menos 3 challenges con `Type = MultipleChoice` | `GetAll` retorna challenges de ambos tipos |

**Total estimado**: ~5 tests nuevos en `DevBrain.Infrastructure.Tests`

### API Tests — DTOs actualizados

| Escenario | Resultado |
|-----------|-----------|
| `GET /challenges` incluye campo `type` en cada item | Response tiene `"type": "OpenText"` o `"type": "MultipleChoice"` |
| `GET /challenges` incluye campo `options` en cada item | Response tiene `"options": []` para OpenText |
| `GET /challenges/{id}` para challenge MultipleChoice incluye opciones | `"options"` tiene 4 elementos |
| `GET /challenges/{id}` para challenge OpenText tiene `options` vacío | `"options": []` |

**Total estimado**: ~4 tests nuevos o actualizados en `DevBrain.Api.Tests`

---

## Criterios de éxito

- `ChallengeType` enum creado con `OpenText` y `MultipleChoice`
- `Challenge.CreateMultipleChoice()` implementado con todas las validaciones
- Migración `AddChallengeTypeAndOptions` generada y aplicable
- 3 challenges MultipleChoice en seed data con GUIDs fijos
- `ChallengeDto` incluye `type` y `options`
- ~20 tests nuevos en verde (Domain + Infrastructure + Api)
- 254 tests backend existentes siguen en verde (sin regresiones)

# Spec: Challenge Types — Code Runner Support

**Tipo**: Domain Entity Change + Infrastructure Migration + API DTO Update  
**Ubicación**:
- `src/DevBrain.Domain/Enums/ChallengeType.cs` (update — agregar `CodeRunner`)
- `src/DevBrain.Domain/ValueObjects/CodeTestCase.cs` (nuevo value object)
- `src/DevBrain.Domain/Entities/Challenge.cs` (update)
- `src/DevBrain.Infrastructure/Persistence/DevBrainDbContext.cs` (update)
- `src/DevBrain.Api/Endpoints/ChallengesEndpoints.cs` (update DTOs)
**Versión**: 1.0

---

## Qué es

Extiende `ChallengeType` con el valor `CodeRunner`. Un challenge de este tipo le presenta al usuario un editor de código con un `starterCode` (template inicial) y una lista de `testCases` (input/output esperados). La evaluación del código ocurre **en el cliente** (browser) — el backend no ejecuta código. El resultado (`PASS` / `FAIL`) es lo que el frontend envía como `userAnswer`.

---

## Prerrequisito

Requiere que `ChallengeType` enum ya exista (`specs/domain/challenge-types.spec.md`). Esta spec agrega un tercer valor al enum.

---

## Value Object: `CodeTestCase`

Objeto de valor inmutable, sin identidad propia. Vive en `DevBrain.Domain/ValueObjects/`.

| Propiedad | Tipo | Reglas |
|-----------|------|--------|
| `Input` | `string` | Puede ser vacío (para funciones sin argumentos) |
| `ExpectedOutput` | `string` | Requerido, no vacío |
| `Description` | `string` | Descripción legible del caso (ej: "Returns sum of two numbers") |

No expone constructor público. Solo se crea con `CodeTestCase.Create(input, expectedOutput, description)`.

---

## Cambios en `ChallengeType`

```
ChallengeType:
  OpenText       = 0
  MultipleChoice = 1
  CodeRunner     = 2  ← nuevo
```

---

## Propiedades nuevas en `Challenge`

| Propiedad | Tipo | Reglas |
|-----------|------|--------|
| `StarterCode` | `string` | Template de código inicial. Vacío para tipos no-CodeRunner |
| `TestCases` | `IReadOnlyList<CodeTestCase>` | Vacía para tipos no-CodeRunner. 1-5 casos para CodeRunner |

---

## Comportamientos

### 1. `Challenge.CreateCodeRunner()` — nuevo factory method

Firma:
```csharp
Challenge.CreateCodeRunner(
    string title,
    string description,
    ChallengeCategory category,
    Difficulty difficulty,
    int timeLimitSecs,
    string starterCode,
    IEnumerable<CodeTestCase> testCases)
```

- `correctAnswer` NO es un parámetro — siempre se fija internamente a `"PASS"`
- `starterCode` puede ser cadena vacía (el usuario empieza de cero)
- `testCases` debe tener entre 1 y 5 elementos
- Resultado: `Type = CodeRunner`, `CorrectAnswer = "PASS"`, `Options` vacía

### 2. `IsCorrectAnswer()` para CodeRunner

Recibe `"PASS"` si el frontend evaluó todos los tests como correctos, o `"FAIL"` si alguno falló.
La comparación sigue siendo `OrdinalIgnoreCase` — no hay lógica especial. `"PASS"` == `"PASS"` → `true`.

### 3. Factories existentes — sin cambios

`Create()` (OpenText) y `CreateMultipleChoice()` no se modifican. `CreateForSeeding()` recibe parámetros opcionales adicionales.

---

## Infraestructura: Migración

### Nuevas columnas en tabla `challenges`

| Columna | Tipo SQL | Nullable | Default |
|---------|----------|----------|---------|
| `StarterCode` | `text` | YES | `NULL` |
| `TestCases` | `text` | YES | `NULL` |

`TestCases` se almacena como JSON string. EF Core usa `HasConversion` con `System.Text.Json` para serializar/deserializar `List<CodeTestCase>`.

### Nombre de la migración
`AddChallengeCodeRunnerFields`

### Seed data

Agregar 2 challenges de tipo `CodeRunner` con GUIDs fijos:
- 1 challenge de `CodeLogic`: función JavaScript que devuelve la suma de dos números
- 1 challenge de `CodeLogic`: función que filtra elementos pares de un array

---

## API: Actualización de DTOs

### Campos nuevos en `ChallengeDto`

```typescript
{
  // campos existentes...
  "type": "OpenText|MultipleChoice|CodeRunner",
  "options": [],
  "starterCode": "function solution(a, b) {\n  // Write your code here\n}",
  "testCases": [
    {
      "input": "2, 3",
      "expectedOutput": "5",
      "description": "Returns the sum of 2 and 3"
    }
  ]
}
```

- `starterCode`: string vacío para OpenText y MultipleChoice
- `testCases`: array vacío para OpenText y MultipleChoice

---

## Invariantes

1. `CorrectAnswer` en un challenge `CodeRunner` siempre es `"PASS"`.
2. Un challenge `CodeRunner` siempre tiene entre 1 y 5 test cases.
3. `StarterCode` puede ser vacío — es solo un template opcional.
4. El backend nunca ejecuta código del usuario — solo compara strings.
5. Si el frontend decide que todos los tests pasan, envía `userAnswer = "PASS"`. El backend acepta cualquier string.

---

## Qué NO incluye esta spec

- No incluye ejecución de código en el servidor
- No soporta lenguajes distintos a JavaScript (post-MVP)
- No guarda el código del usuario en la BD (solo si pasó o no)
- No soporta más de 5 test cases

---

## Escenarios de test esperados

### Domain — `CodeTestCase`

| Escenario | Resultado |
|-----------|-----------|
| `CodeTestCase.Create` con input, expectedOutput y description válidos | OK — value object creado |
| `CodeTestCase.Create` con `expectedOutput` vacío | `DomainException` |
| `CodeTestCase.Create` con `description` vacío | `DomainException` |

### Domain — `Challenge.CreateCodeRunner`

| Escenario | Resultado |
|-----------|-----------|
| `CreateCodeRunner` con 3 test cases válidos | `Type = CodeRunner`, `CorrectAnswer = "PASS"`, `TestCases.Count = 3` |
| `CreateCodeRunner` con 1 test case (mínimo válido) | OK |
| `CreateCodeRunner` con 6 test cases | `DomainException` |
| `CreateCodeRunner` con 0 test cases | `DomainException` |
| `IsCorrectAnswer("PASS")` en challenge CodeRunner | `true` |
| `IsCorrectAnswer("FAIL")` en challenge CodeRunner | `false` |

### Infrastructure

| Escenario | Resultado |
|-----------|-----------|
| Guardar challenge CodeRunner con 3 test cases y recuperarlo | `TestCases.Count = 3`, datos intactos |
| `StarterCode` se persiste y recupera | String igual tras round-trip |
| Challenge OpenText tiene `StarterCode` vacío y `TestCases` vacía | Valores default correctos |

**Total estimado**: ~12 tests nuevos

---

## Criterios de éxito

- `CodeTestCase` value object implementado con validaciones
- `Challenge.CreateCodeRunner()` con validaciones de test cases
- Migración `AddChallengeCodeRunnerFields` generada y aplicable
- 2 challenges CodeRunner en seed data
- `ChallengeDto` incluye `starterCode` y `testCases`
- ~12 tests nuevos en verde
- 254+ tests backend existentes siguen en verde

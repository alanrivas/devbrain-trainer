# Spec: Challenge Types — Ordering (Drag & Drop) Support

**Tipo**: Domain Entity Change + Infrastructure Migration + API DTO Update  
**Ubicación**:
- `src/DevBrain.Domain/Enums/ChallengeType.cs` (update — agregar `Ordering`)
- `src/DevBrain.Domain/Entities/Challenge.cs` (update)
- `src/DevBrain.Infrastructure/Persistence/DevBrainDbContext.cs` (update)
- `src/DevBrain.Api/Endpoints/ChallengesEndpoints.cs` (update DTOs)
**Versión**: 1.0

---

## Qué es

Extiende `ChallengeType` con el valor `Ordering`. Un challenge de este tipo presenta una lista de ítems desordenados y el usuario los arrastra hasta ordenarlos correctamente. El orden correcto se almacena en `CorrectAnswer` como una cadena de ítems separados por pipe (`|`). El frontend envía el orden elegido por el usuario en el mismo formato.

---

## Prerrequisito

Requiere que `ChallengeType` enum ya exista con al menos `OpenText`, `MultipleChoice` y `CodeRunner`. Esta spec agrega el cuarto valor.

---

## Cambios en `ChallengeType`

```
ChallengeType:
  OpenText       = 0
  MultipleChoice = 1
  CodeRunner     = 2
  Ordering       = 3  ← nuevo
```

---

## Propiedades nuevas en `Challenge`

| Propiedad | Tipo | Reglas |
|-----------|------|--------|
| `Items` | `IReadOnlyList<string>` | Vacía para tipos no-Ordering. 2-6 ítems para Ordering |

La columna `CorrectAnswer` existente almacena el orden correcto como pipe-separated: `"step1|step2|step3"`.

---

## Comportamientos

### 1. `Challenge.CreateOrdering()` — nuevo factory method

Firma:
```csharp
Challenge.CreateOrdering(
    string title,
    string description,
    ChallengeCategory category,
    Difficulty difficulty,
    int timeLimitSecs,
    IEnumerable<string> items,
    IEnumerable<string> correctOrder)
```

- `correctOrder` debe ser una permutación exacta de `items` (mismos elementos, distinto orden o igual)
- `items` debe tener entre 2 y 6 elementos
- Ningún ítem puede ser vacío o solo whitespace
- No puede haber ítems duplicados (case-insensitive)
- `CorrectAnswer` se construye como `string.Join("|", correctOrder.Select(i => i.Trim()))`
- `Options` vacía — no aplica para este tipo

### 2. `IsCorrectAnswer()` para Ordering

Compara el string enviado por el frontend (pipe-separated) contra `CorrectAnswer` con `OrdinalIgnoreCase`. Si el usuario ordenó exactamente igual, coincide. No hay lógica especial — la comparación de string ya funciona.

### 3. `CreateForSeeding()` — parámetros opcionales adicionales

Agregar `IEnumerable<string>? items = null` para soportar challenges de tipo Ordering en seed data.

---

## Infraestructura: Migración

### Nueva columna en tabla `challenges`

| Columna | Tipo SQL | Nullable | Default |
|---------|----------|----------|---------|
| `Items` | `text` | YES | `NULL` |

`Items` se almacena como string pipe-separated. EF Core usa `HasConversion` para serializar/deserializar (igual que `Options` en MultipleChoice).

### Nombre de la migración
`AddChallengeOrderingItems`

### Seed data

Agregar 2 challenges de tipo `Ordering` con GUIDs fijos:
- 1 challenge de `Architecture`: ordenar las capas de una arquitectura hexagonal
- 1 challenge de `DevOps`: ordenar los pasos de un pipeline CI/CD (build → test → lint → deploy)

---

## API: Actualización de DTOs

### Campo nuevo en `ChallengeDto`

```typescript
{
  // campos existentes...
  "type": "Ordering",
  "options": [],
  "items": ["step3", "step1", "step4", "step2"],  // ← nuevo, en orden aleatorio para el frontend
  "starterCode": "",
  "testCases": []
}
```

- `items`: el frontend los muestra desordenados (el orden en el array ya puede ser aleatorio o siempre igual; el frontend es quien los shufflea para el usuario)
- `items` está vacío para tipos OpenText, MultipleChoice y CodeRunner

**Nota importante**: `CorrectAnswer` **no** se expone en el DTO — el backend ya nunca lo envía al cliente.

---

## Invariantes

1. Un challenge `Ordering` siempre tiene entre 2 y 6 ítems.
2. `correctOrder` debe contener exactamente los mismos ítems que `items` — ni más ni menos.
3. `CorrectAnswer` para Ordering es siempre un pipe-separated de los ítems en el orden correcto.
4. `Items` vacía para cualquier tipo que no sea `Ordering`.
5. El backend no calcula si el orden es "parcialmente correcto" — es todo o nada.

---

## Qué NO incluye esta spec

- No incluye feedback de "estabas a N posiciones del orden correcto"
- No soporta más de 6 ítems
- No incluye ítems con imágenes o iconos (solo texto)
- No modifica el cálculo de ELO (misma fórmula para todos los tipos)

---

## Escenarios de test esperados

### Domain — `Challenge.CreateOrdering`

| Escenario | Resultado |
|-----------|-----------|
| `CreateOrdering` con 4 ítems válidos y `correctOrder` = permutación de items | `Type = Ordering`, `Items.Count = 4` |
| `CreateOrdering` con 2 ítems (mínimo válido) | OK |
| `CreateOrdering` con 1 ítem | `DomainException` |
| `CreateOrdering` con 7 ítems | `DomainException` |
| `CreateOrdering` con ítem vacío | `DomainException` |
| `CreateOrdering` con ítems duplicados | `DomainException` |
| `CreateOrdering` con `correctOrder` que contiene un ítem no presente en `items` | `DomainException` |
| `CreateOrdering` con `correctOrder` con menos ítems que `items` | `DomainException` |
| `IsCorrectAnswer` con el orden correcto en pipe-separated | `true` |
| `IsCorrectAnswer` con orden incorrecto | `false` |

### Infrastructure

| Escenario | Resultado |
|-----------|-----------|
| Guardar challenge Ordering y recuperarlo mantiene `Items` intacta | `Items.Count` igual, strings iguales |
| Challenge OpenText tiene `Items` vacía | `Items.Count = 0` |
| Seed data incluye challenges de tipo Ordering | `GetAll` retorna challenges Ordering |

**Total estimado**: ~13 tests nuevos

---

## Criterios de éxito

- `Challenge.CreateOrdering()` implementado con todas las validaciones
- Migración `AddChallengeOrderingItems` generada y aplicable
- 2 challenges Ordering en seed data con GUIDs fijos
- `ChallengeDto` incluye `items`
- ~13 tests nuevos en verde
- Tests backend existentes siguen en verde (sin regresiones)

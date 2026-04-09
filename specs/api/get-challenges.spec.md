# Spec: GET /challenges

**Tipo**: Endpoint de API (sin autenticación)  
**Ubicación**: `DevBrain.Api`  
**Versión**: 1.0  

---

## Qué es

Endpoint GET que retorna una lista paginada de `Challenge` disponibles en la app.
Permite filtrado opcional por categoría y dificultad.
No requiere autenticación — cualquiera puede listar los challenges.

---

## Ruta y método

```
GET /api/v1/challenges
```

---

## Query Parameters (opcionales)

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `category` | `string` (enum) | Filtrar por categoría — valores: `Sql`, `CodeLogic`, `Architecture`, `DevOps`, `WorkingMemory` | `?category=Sql` |
| `difficulty` | `string` (enum) | Filtrar por dificultad — valores: `Easy`, `Medium`, `Hard` | `?difficulty=Easy` |
| `pageNumber` | `int` | Página (1-indexed, default: 1) | `?pageNumber=2` |
| `pageSize` | `int` | Resultados por página (default: 10, máx: 50) | `?pageSize=20` |

**Reglas**:
- Si `category` o `difficulty` están vacíos o valores inválidos, ignorarlos (no filtran)
- Si `pageNumber` < 1, usar 1
- Si `pageSize` < 1 o > 50, usar 10
- Si no se especifican parámetros, retorna página 1 con 10 resultados

---

## Respuesta exitosa (200 OK)

```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 47,
  "totalPages": 5,
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "title": "SQL: Select Top N Records",
      "description": "Write a query that returns top 5 users by attempt count",
      "category": "Sql",
      "difficulty": "Easy",
      "timeLimitSecs": 60
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "title": "C#: Extract Method",
      "description": "Refactor this code to follow DRY",
      "category": "CodeLogic",
      "difficulty": "Easy",
      "timeLimitSecs": 90
    }
  ]
}
```

**Esquema**:
- `pageNumber` (int): página actual solicitada
- `pageSize` (int): cantidad de items por página
- `totalCount` (int): total de challenges (después de filtros)
- `totalPages` (int): total de páginas calculado
- `items` (array): lista de challenges
  - `id` (UUID): identificador del challenge
  - `title` (string): título del problema
  - `description` (string): descripción/enunciado
  - `category` (string): categoría del challenge
  - `difficulty` (string): nivel de dificultad
  - `timeLimitSecs` (int): tiempo máximo permitido

**Notas**:
- **NO incluir** `correctAnswer` en la respuesta (eso es secreto del servidor)
- **NO incluir** `createdAt` (metadata innecesaria para el cliente)
- Retornar challenges ordenados por `createdAt DESC` (más recientes primero)

---

## Respuesta en caso de parámetros inválidos (400 Bad Request)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "category": ["Invalid category value. Valid values are: Sql, CodeLogic, Architecture, DevOps, WorkingMemory"],
    "difficulty": ["Invalid difficulty value. Valid values are: Easy, Medium, Hard"]
  }
}
```

---

## Escenarios de test esperados

| Escenario | Status | Resultado |
|-----------|--------|-----------|
| GET /api/v1/challenges sin parámetros | 200 | Retorna primera página con ~10 resultados |
| GET /api/v1/challenges?category=Sql | 200 | Retorna solo challenges SQL |
| GET /api/v1/challenges?difficulty=Hard | 200 | Retorna solo challenges Hard |
| GET /api/v1/challenges?category=Sql&difficulty=Medium | 200 | Retorna solo SQL AND Medium |
| GET /api/v1/challenges?pageNumber=2 | 200 | Retorna segunda página |
| GET /api/v1/challenges?pageSize=25 | 200 | Retorna 25 items por página |
| GET /api/v1/challenges?category=InvalidValue | 400 | Error de validación |
| GET /api/v1/challenges?difficulty=InvalidValue | 400 | Error de validación |
| GET /api/v1/challenges?pageNumber=-1 | 200 | Ignora inválido, retorna página 1 |
| GET /api/v1/challenges?pageSize=999 | 200 | Limita a máximo 50, retorna página 1 |
| GET /api/v1/challenges con filtros sin resultados | 200 | Retorna `totalCount=0`, `items=[]` |
| Respuesta no incluye `correctAnswer` | 200 | Campo ausente en response |
| Respuesta no incluye `createdAt` | 200 | Campo ausente en response |
| Total challenges es >= 10 (seed data) | 200 | Validar count en response |

---

## Dependencias

- `IChallengeRepository` — para acceder a los challenges
- `Mapper` — para convertir entidades de dominio a DTOs (sin exponer secretos)
- `Validador de enums` — para validar category y difficulty

---

## DTOs necesarios

### ChallengeResponseDto

```csharp
public sealed record ChallengeResponseDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Difficulty,
    int TimeLimitSecs
);
```

### PaginatedResponseDto

```csharp
public sealed record PaginatedResponseDto<T>(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<T> Items
);
```

---

## Lógica interna

1. **Validación de parámetros**:
   - Si `category` es string válido, parsearlo a enum `ChallengeCategory`
   - Si `difficulty` es string válido, parsearlo a enum `Difficulty`
   - Si hay error en parsing, retornar 400 Bad Request

2. **Ajuste de paginación**:
   - `pageNumber` = max(1, pageNumber)
   - `pageSize` = clamp(pageSize, 1, 50); default 10

3. **Query al repositorio**:
   - Llamar a `challengeRepository.GetAllAsync(category, difficulty)`
   - Esta retorna lista DESC por fecha — NO hay paginación directa en el repo
   - Internamente: hacer Skip/Take en memoria o en LINQ to EF

4. **Mapeo a DTO**:
   - Convertir cada `Challenge` a `ChallengeResponseDto`
   - Omitir `CorrectAnswer` y `CreatedAt`

5. **Construcción de respuesta**:
   - Calcular `totalPages = (totalCount + pageSize - 1) / pageSize`
   - Retornar `PaginatedResponseDto` con paginación aplicada

---

## Qué NO es este endpoint

- No realiza autenticación
- No ordena por rating o usuario (eso es del dashboard)
- No retorna el historial de intentos (eso es otro endpoint)
- No expone la respuesta correcta (seguridad)

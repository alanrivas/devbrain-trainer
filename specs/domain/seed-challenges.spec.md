# Spec: SeedChallenges

**Tipo**: Datos iniciales de dominio (EF Core HasData)  
**Ubicación**: `DevBrain.Infrastructure.Persistence.DevBrainDbContext`  
**Versión**: 1.0  

---

## Qué es

Conjunto de 10 challenges predefinidos que se insertan automáticamente al aplicar las migraciones de EF Core. Permiten que el MVP sea funcional desde el primer deploy sin necesidad de cargar datos manualmente.

El seed se define en `SeedChallenges()` dentro de `OnModelCreating` del `DevBrainDbContext`, usando el factory method `Challenge.CreateForSeeding()` con GUIDs fijos para garantizar idempotencia entre migraciones.

---

## Mecanismo de seeding

- **Método**: EF Core `HasData()` en `ModelBuilder`
- **Factory**: `Challenge.CreateForSeeding(Guid, title, description, category, difficulty, correctAnswer, timeLimitSecs, createdAt)`
- **GUIDs**: fijos en el rango `10000000-0000-0000-0000-00000000000X` (deterministas)
- **Fecha base**: `2024-01-01T00:00:00Z` para todos los challenges

Los GUIDs fijos garantizan que las migraciones sean idempotentes: EF Core compara por Id y no duplica registros al hacer `database update`.

---

## Distribución de challenges

| Categoría       | Fácil | Medio | Difícil | Total |
|-----------------|-------|-------|---------|-------|
| Sql             | 1     | 1     | 0       | 2     |
| CodeLogic       | 2     | 0     | 0       | 2     |
| Architecture    | 0     | 1     | 1       | 2     |
| DevOps          | 1     | 1     | 0       | 2     |
| WorkingMemory   | 1     | 1     | 0       | 2     |
| **Total**       | **5** | **4** | **1**   | **10**|

---

## Catálogo de challenges

### SQL

| Id (sufijo) | Título | Dificultad | TimeLimitSecs |
|-------------|--------|------------|---------------|
| `...0001` | SQL: Select Top N Records | Easy | 60 |
| `...0002` | SQL: Join Multiple Tables | Medium | 120 |

### CodeLogic

| Id (sufijo) | Título | Dificultad | TimeLimitSecs |
|-------------|--------|------------|---------------|
| `...0003` | C#: Extract Method | Easy | 90 |
| `...0004` | C#: Null Coalescing | Easy | 45 |

### Architecture

| Id (sufijo) | Título | Dificultad | TimeLimitSecs |
|-------------|--------|------------|---------------|
| `...0005` | Architecture: SOLID - Single Responsibility | Medium | 75 |
| `...0006` | Architecture: Design Pattern | Hard | 150 |

### DevOps

| Id (sufijo) | Título | Dificultad | TimeLimitSecs |
|-------------|--------|------------|---------------|
| `...0007` | Docker: Container Listing | Easy | 60 |
| `...0008` | Docker: Image Cleanup | Medium | 90 |

### WorkingMemory

| Id (sufijo) | Título | Dificultad | TimeLimitSecs |
|-------------|--------|------------|---------------|
| `...0009` | Memory: Variable Tracing | Medium | 120 |
| `...0010` | Memory: Loop Counting | Easy | 60 |

---

## Invariantes

1. Exactamente 10 challenges al aplicar `InitialCreate`
2. Cada challenge tiene un GUID único y fijo — no se generan con `Guid.NewGuid()`
3. Las 5 categorías están representadas (al menos 1 challenge por categoría)
4. Las respuestas correctas son case-insensitive al evaluarse con `IsCorrectAnswer()`
5. Todos los `TimeLimitSecs` están entre 30 y 300 (válidos según la entidad `Challenge`)

---

## Qué NO es esta spec

- No define cómo agregar challenges en producción desde la API (eso es un endpoint admin, fuera del MVP)
- No define seed de usuarios ni attempts
- No valida las respuestas de los challenges (eso es responsabilidad de `Challenge.IsCorrectAnswer`)

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| DB fresca tras `database update` → `GET /api/v1/challenges` retorna 10 items | 10 challenges |
| `GET /api/v1/challenges?category=Sql` retorna 2 challenges | 2 challenges |
| `GET /api/v1/challenges?difficulty=Easy` retorna 5 challenges | 5 challenges |
| `GET /api/v1/challenges?difficulty=Hard` retorna 1 challenge | 1 challenge (singleton) |
| Aplicar migraciones dos veces → sin duplicados | Idempotente |
| Challenge `...0004` con answer `??` → `IsCorrectAnswer("??")` = true | true |
| Challenge `...0009` con answer `15` → `IsCorrectAnswer("15")` = true | true |

---

## Notas de implementación

- `CreateForSeeding` omite las validaciones de `Create` para poder controlar Id y CreatedAt explícitamente
- En el `CustomWebApplicationFactory` de tests, el seed se aplica automáticamente al hacer `EnsureCreated()` o al correr las migraciones con la DB en memoria/SQLite
- En Railway (producción), las migraciones se corren en el startup con `dbContext.Database.MigrateAsync()`

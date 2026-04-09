# Spec: EF Challenge Repository

**Tipo**: Infraestructura (Implementación EF Core de interfaz de dominio)  
**Ubicación**: `DevBrain.Infrastructure`  
**Versión**: 1.0  

---

## Qué es

`EFChallengeRepository` es la implementación EF Core de la interfaz `IChallengeRepository` (definida en Domain).
Proporciona acceso a datos de `Challenge` mediante consultas a PostgreSQL usando Entity Framework Core.

El repositorio encapsula todas las queries EF Core, mantiene la lógica de acceso a datos separada del dominio y permite persistir/recuperar challenges de la base de datos.

---

## Interfaz implementada

```csharp
public interface IChallengeRepository
{
    Task<Challenge?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Challenge>> GetAllAsync(ChallengeCategory? category = null, Difficulty? difficulty = null);
    Task AddAsync(Challenge challenge);
}
```

---

## Métodos

### GetByIdAsync

```csharp
public async Task<Challenge?> GetByIdAsync(Guid id)
```

**Comportamiento**:
- Busca un `Challenge` en PostgreSQL por su `Id` (Guid)
- Si existe, retorna la entidad completa
- Si no existe, retorna `null`
- Query: `SELECT * FROM challenges WHERE id = @id`

**Reglas**:
- Si `id` es `Guid.Empty`, puede devolver `null` (no es error de dominio, es responsabilidad del caller validar)
- La query debe ser **case-sensitive** sobre el GUID
- No materializa relaciones (challenges no tienen FK a user, solo los attempts los enlazan)

---

### GetAllAsync

```csharp
public async Task<IReadOnlyList<Challenge>> GetAllAsync(ChallengeCategory? category = null, Difficulty? difficulty = null)
```

**Comportamiento**:
- Retorna todos los `Challenge` aplicando filtros opcionales de categoría y dificultad
- Si `category` es `null`, no filtra por categoría (todos los valores)
- Si `difficulty` es `null`, no filtra por dificultad (todos los valores)
- Ambos filtros se combinan con AND (si ambos se proporcionan)
- Retorna `IReadOnlyList<Challenge>` (inmutable)
- Si no hay resultados, retorna lista vacía (nunca `null`)

**Querys generadas**:
- Sin filtros: `SELECT * FROM challenges ORDER BY created_at DESC`
- Con categoría: `SELECT * FROM challenges WHERE category = @cat ORDER BY created_at DESC`
- Con dificultad: `SELECT * FROM challenges WHERE difficulty = @diff ORDER BY created_at DESC`
- Con ambos: `SELECT * FROM challenges WHERE category = @cat AND difficulty = @diff ORDER BY created_at DESC`

**Reglas**:
- Los resultados **siempre están ordenados** por `created_at DESC` (más recientes primero)
- Usa índices en `category` e `difficulty` para optimizar las queries
- Retorna máximo N resultados (sin paginación en este método — paginación viene en specs posteriores)

---

### AddAsync

```csharp
public async Task AddAsync(Challenge challenge)
```

**Comportamiento**:
- Inserta a un nuevo `Challenge` en PostgreSQL y guarda los cambios
- El `Challenge` debe ser una entidad válida creada con `Challenge.Create()`
- Después de insertar, el `Challenge` recibe su `Id` (aunque ya lo tiene, EF lo confirma contra la BD)
- Si hay conflicto o duplicado, propaga la excepción de BD

**Reglas**:
- No valida el challenge (asume que ya fue validado por el dominio)
- Ejecuta `SaveChangesAsync()` internamente (no es responsabilidad del caller)
- Si la BD rechaza la inserción (ej: constraint), deja que la excepción de EF suba (ej: `DbUpdateException`)
- Las propiedades inmutables (`Id`, `CreatedAt`) se persisten tal cual vinieron

---

## Invariantes de implementación

1. Todas las queries se ejecutan **asincronamente** (`async/await`)
2. Las queries materializan completamente — no hay lazy-loading de relaciones
3. El `DbContext` se inyecta en el constructor (DI)
4. No hay transacciones explícitas (usa el comportamiento por defecto de EF)
5. No hay logs directos de SQL (se confía en EF Core logging)

---

## Qué NO es este repositorio

- No implementa paginación (eso es para una versión futura)
- No cachea resultados en memoria (eso es responsabilidad de Redis/estrategia de caché)
- No valida el dominio — el desafío debe venir ya validado
- No accede a `Attempt` ni otros aggregates (cada repositorio es independiente)

---

## Constructor

```csharp
public EFChallengeRepository(DevBrainDbContext context)
```

- Recibe `DevBrainDbContext` inyectado (por DI en `Program.cs`)
- Almacena el contexto como `private readonly` para poder acceder a `context.Challenges`

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| GetByIdAsync con ID válido existente | OK — retorna Challenge completo |
| GetByIdAsync con ID válido no existente | OK — retorna `null` |
| GetByIdAsync con `Guid.Empty` | OK — retorna `null` |
| GetAllAsync sin filtros | OK — retorna lista de todos (seed data ~10 + agregados) |
| GetAllAsync con filtro category=SQL | OK — retorna solo challenges SQL |
| GetAllAsync con filtro difficulty=Easy | OK — retorna solo challenges fáciles |
| GetAllAsync con ambos filtros | OK — retorna challenges SQL AND Easy |
| GetAllAsync con filtros que no coinciden con nada | OK — retorna lista vacía |
| AddAsync con Challenge válido | OK — se persiste, `SaveChangesAsync()` ejecutado |
| AddAsync con Challenge válido (otra instancia) | OK — se persiste también (sin duplicados por ID) |
| GetAllAsync retorna ordered by created_at DESC | OK — orden correcto |
| GetAllAsync retorna IReadOnlyList | OK — interfaz inmutable |

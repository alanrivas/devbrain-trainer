# Spec: EF Attempt Repository

**Tipo**: Infraestructura (Implementación EF Core de interfaz de dominio)  
**Ubicación**: `DevBrain.Infrastructure`  
**Versión**: 1.0  

---

## Qué es

`EFAttemptRepository` es la implementación EF Core de la interfaz `IAttemptRepository` (definida en Domain).
Proporciona acceso a datos de `Attempt` mediante consultas a PostgreSQL usando Entity Framework Core.

El repositorio encapsula todas las queries EF Core para intentos de usuarios, permitiendo persistir intentos y acceder al historial de cada usuario.

---

## Interfaz implementada

```csharp
public interface IAttemptRepository
{
    Task AddAsync(Attempt attempt);
    Task<IReadOnlyList<Attempt>> GetByUserAsync(string userId);
    Task<Attempt?> GetLastByUserAsync(string userId);
    Task<int> CountCorrectByUserAsync(string userId);
}
```

---

## Métodos

### AddAsync

```csharp
public async Task AddAsync(Attempt attempt)
```

**Comportamiento**:
- Inserta un nuevo `Attempt` en PostgreSQL y guarda los cambios
- El `Attempt` debe ser una entidad válida creada con `Attempt.Create()`
- Después de insertar, se ejecuta `SaveChangesAsync()`
- Si hay error en BD, propaga la excepción de EF

**Reglas**:
- No valida el attempt (asume que ya fue validado por dominio)
- No hay transacciones explícitas (usa comportamiento por defecto de EF)
- La propiedad `IsCorrect` ya está calculada en el attempt antes de persistir

---

### GetByUserAsync

```csharp
public async Task<IReadOnlyList<Attempt>> GetByUserAsync(string userId)
```

**Comportamiento**:
- Retorna TODOS los intentos de un usuario específico
- Ordena por `OccurredAt DESC` (intentos más recientes primero)
- Si el usuario no tiene intentos, retorna lista vacía (nunca `null`)
- Retorna `IReadOnlyList<Attempt>`

**Querys**:
- `SELECT * FROM attempts WHERE user_id = @userId ORDER BY occurred_at DESC`

**Reglas**:
- Usa índice compuesto `(user_id, occurred_at DESC)` para optimizar
- Si `userId` es vacío o `null`, retorna lista vacía
- Materializa completamente — no lazy-loading

---

### GetLastByUserAsync

```csharp
public async Task<Attempt?> GetLastByUserAsync(string userId)
```

**Comportamiento**:
- Retorna el ÚLTIMO (más reciente) intento de un usuario
- Ordena por `OccurredAt DESC` y toma el primero
- Si el usuario no tiene ningún intento, retorna `null`

**Querys**:
- `SELECT * FROM attempts WHERE user_id = @userId ORDER BY occurred_at DESC LIMIT 1`

**Reglas**:
- Usa el mismo índice `(user_id, occurred_at DESC)` para optimizar
- Si `userId` es vacío o `null`, retorna `null`
- Retorna exactamente 1 resultado o `null`

---

### CountCorrectByUserAsync

```csharp
public async Task<int> CountCorrectByUserAsync(string userId)
```

**Comportamiento**:
- Cuenta cuántos intentos CORRECTOS tiene un usuario (where `is_correct = true`)
- Retorna un `int` >= 0
- Si el usuario no existe o no tiene intentos correctos, retorna `0`

**Querys**:
- `SELECT COUNT(*) FROM attempts WHERE user_id = @userId AND is_correct = true`

**Reglas**:
- Usa índice compuesto `(user_id, challenge_id, is_correct)` para optimizar
- Nunca retorna `null`, siempre >= 0
- Si `userId` es vacío o `null`, retorna `0`

---

## Constructor

```csharp
public EFAttemptRepository(DevBrainDbContext context)
```

- Recibe `DevBrainDbContext` inyectado (por DI en `Program.cs`)
- Almacena el contexto como `private readonly` para acceder a `context.Attempts`

---

## Invariantes de implementación

1. Todas las queries se ejecutan **asincronamente** (`async/await`)
2. Las queries materializan completamente — no hay lazy-loading de relaciones
3. El `DbContext` se inyecta en el constructor (DI)
4. Usa índices existentes en la BD para optimizar queries
5. No hay logs directos de SQL (se confía en EF Core logging)

---

## Qué NO es este repositorio

- No implementa paginación de intentos (future feature)
- No cachea resultados (Redis es responsable de eso)
- No valida el dominio — el attempt debe venir ya validado
- No accede a `Challenge` ni `User` (cada repositorio es independiente)

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| AddAsync con Attempt válido | OK — se persiste |
| AddAsync con múltiples Attempts del mismo usuario | OK — todos se persisten sin duplicar |
| AddAsync con Attempt is_correct=true | OK — persiste correctamente |
| AddAsync con Attempt is_correct=false | OK — persiste correctamente |
| GetByUserAsync con userId válido existente | OK — retorna lista de todos sus intentos |
| GetByUserAsync con userId sin intentos | OK — retorna lista vacía |
| GetByUserAsync con userId vacío | OK — retorna lista vacía |
| GetByUserAsync retorna ordered by occurred_at DESC | OK — orden correcto |
| GetLastByUserAsync con userId válido con intentos | OK — retorna el más reciente |
| GetLastByUserAsync con userId sin intentos | OK — retorna `null` |
| GetLastByUserAsync con userId vacío | OK — retorna `null` |
| CountCorrectByUserAsync con userId con intentos correctos e incorrectos | OK — cuenta solo correctos |
| CountCorrectByUserAsync con userId sin intentos correctos | OK — retorna `0` |
| CountCorrectByUserAsync con userId sin ningún intento | OK — retorna `0` |
| CountCorrectByUserAsync con userId vacío | OK — retorna `0` |
| GetByUserAsync retorna IReadOnlyList | OK — interfaz inmutable |
| AddAsync luego GetByUserAsync retorna el agregado | OK — consulta después de persistencia |

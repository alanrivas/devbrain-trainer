# Spec: EFUserRepository

**Tipo**: Infraestructura (Implementación EF Core de interfaz de dominio)  
**Ubicación**: `DevBrain.Infrastructure`  
**Versión**: 1.0  

---

## Qué es

`EFUserRepository` es la implementación EF Core de la interfaz `IUserRepository` (definida en Domain).
Proporciona acceso a datos de `User` mediante consultas a PostgreSQL usando Entity Framework Core.

El repositorio encapsula todas las queries EF Core para usuarios, permitiendo persistir nuevos usuarios y buscarlos por email o Id.

---

## Interfaz implementada

```csharp
public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
```

---

## Métodos

### AddAsync

```csharp
public async Task AddAsync(User user, CancellationToken cancellationToken = default)
```

**Comportamiento**:
- Inserta un nuevo `User` en PostgreSQL y guarda los cambios
- El `User` debe ser una entidad válida creada con `User.Create()`
- Después de insertar, ejecuta `SaveChangesAsync(cancellationToken)`
- Si hay error de BD (ej. email duplicado por constraint), propaga la excepción de EF

**Reglas**:
- No valida el usuario (asume que ya fue validado por dominio)
- No hay transacciones explícitas (usa comportamiento por defecto de EF)
- El `PasswordHash` ya fue calculado antes de llamar al repositorio

---

### GetByEmailAsync

```csharp
public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
```

**Comportamiento**:
- Busca un usuario cuyo email coincida, de forma **case-insensitive**
- Normaliza el email a minúsculas antes de comparar: `email.ToLower()`
- Si no existe, retorna `null`

**Query**:
```sql
SELECT * FROM users WHERE LOWER(email) = LOWER(@email) LIMIT 1
```

**Reglas**:
- La búsqueda es case-insensitive (RFC 5321: emails son case-insensitive)
- Retorna el primer resultado o `null` — no puede haber duplicados por el índice único en `email`
- Si `email` es vacío o whitespace, retorna `null` (no lanza excepción)

---

### GetByIdAsync

```csharp
public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
```

**Comportamiento**:
- Busca un usuario por su `Guid Id` primario
- Si no existe, retorna `null`

**Query**:
```sql
SELECT * FROM users WHERE id = @id LIMIT 1
```

**Reglas**:
- Si `id` es `Guid.Empty`, retorna `null` (no lanza excepción)
- Usa el índice de clave primaria para la búsqueda (performante)

---

## Constructor

```csharp
public EFUserRepository(DevBrainDbContext context)
```

- Recibe `DevBrainDbContext` inyectado (por DI en `Program.cs`)
- Almacena el contexto como `private readonly` para acceder a `context.Users`

---

## Invariantes de implementación

1. Todas las queries se ejecutan **asincronamente** (`async/await`)
2. La búsqueda por email es case-insensitive
3. El `DbContext` se inyecta en el constructor (DI)
4. No hay lazy-loading de relaciones
5. No accede a `Challenge` ni `Attempt` (cada repositorio es independiente)

---

## Qué NO es este repositorio

- No implementa update ni delete de usuarios (fuera del alcance del MVP)
- No cachea resultados (Redis es responsable de eso)
- No valida el dominio — el user debe venir ya validado
- No genera el password hash (eso lo hace la capa de aplicación/endpoint)

---

## Cobertura de tests

> `EFUserRepository` no tiene un test file dedicado en `DevBrain.Infrastructure.Tests`.  
> Sus métodos se ejercitan indirectamente vía los tests de integración de la API:
>
> - `AddAsync` + `GetByEmailAsync` → cubiertos por `PostAuthRegisterEndpointTests` (13 tests)  
> - `GetByEmailAsync` → cubierto por `PostAuthLoginEndpointTests` (11 tests)  
> - `GetByIdAsync` → no tiene cobertura de test actual (**deuda técnica**)

### Tests unitarios pendientes (deuda técnica)

| Escenario | Resultado esperado |
|-----------|-------------------|
| `AddAsync` con User válido | Persiste en BD |
| `AddAsync` luego `GetByEmailAsync` retorna el usuario | Consistencia post-insert |
| `GetByEmailAsync` con email existente | Retorna el usuario correcto |
| `GetByEmailAsync` case-insensitive (`ALAN@test.com` encuentra `alan@test.com`) | Retorna el usuario |
| `GetByEmailAsync` con email inexistente | Retorna `null` |
| `GetByEmailAsync` con email vacío | Retorna `null` |
| `GetByIdAsync` con Id existente | Retorna el usuario correcto |
| `GetByIdAsync` con Id inexistente | Retorna `null` |
| `GetByIdAsync` con `Guid.Empty` | Retorna `null` |

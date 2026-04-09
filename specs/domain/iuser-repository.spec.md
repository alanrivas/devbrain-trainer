# Spec: IUserRepository

**Tipo**: Interfaz de dominio (contrato de persistencia)  
**Ubicación**: `DevBrain.Domain`  
**Versión**: 1.0  

---

## Qué es

`IUserRepository` es la interfaz que define el contrato de persistencia para usuarios dentro del dominio. Permite que la lógica de aplicación acceda y almacene usuarios sin depender de EF Core ni de ninguna tecnología de base de datos. La implementación concreta vive en `DevBrain.Infrastructure`.

---

## Métodos

| Método | Retorno | Descripción |
|--------|---------|-------------|
| `AddAsync(User user, CancellationToken ct = default)` | `Task` | Persiste un nuevo usuario |
| `GetByEmailAsync(string email, CancellationToken ct = default)` | `Task<User?>` | Retorna el usuario con ese email (case-insensitive), o `null` si no existe |
| `GetByIdAsync(Guid id, CancellationToken ct = default)` | `Task<User?>` | Retorna el usuario con ese Id, o `null` si no existe |

---

## Comportamientos del contrato

### AddAsync

- Persiste el usuario recibido
- El usuario ya fue validado por `User.Create()` — el repositorio no revalida reglas de dominio
- Soporta `CancellationToken` para operaciones cancelables
- No retorna el usuario (el Id ya existe en el objeto recibido)

### GetByEmailAsync

- Recibe un `string email`
- La búsqueda es **case-insensitive** (`"ALAN@test.com"` encuentra `"alan@test.com"`)
- Retorna `User?` — `null` si no se encuentra (no lanza excepción)
- El caller es responsable de manejar el caso `null`

### GetByIdAsync

- Recibe un `Guid id`
- Retorna `User?` — `null` si no se encuentra (no lanza excepción)
- El caller es responsable de manejar el caso `null`

---

## Invariantes del contrato

1. `GetByEmailAsync` y `GetByIdAsync` nunca lanzan `DomainException` — solo retornan `null` si no existe
2. La búsqueda por email es case-insensitive (emails son case-insensitive por RFC 5321)
3. La interfaz no conoce detalles de base de datos (sin `DbContext`, sin SQL, sin transacciones)
4. La implementación EF Core es responsabilidad de `DevBrain.Infrastructure`, nunca del dominio

---

## Qué NO es esta interfaz

- No valida reglas de dominio (eso ya lo hizo `User.Create()`)
- No gestiona transacciones (eso es responsabilidad del servicio de aplicación)
- No actualiza ni elimina usuarios (fuera del alcance del MVP)
- No contiene ninguna implementación — solo el contrato

---

## Ubicación en el proyecto

```
src/
  DevBrain.Domain/
    Interfaces/
      IUserRepository.cs   ← interfaz de dominio
```

---

## Escenarios de test

> Los tests de `IUserRepository` se verifican en la spec de implementación  
> (`ef-user-repository.spec.md`) usando base de datos en memoria.  
> Para esta spec, se crea solo la interfaz — sin tests en este paso.

| Escenario | Verificado en |
|-----------|---------------|
| `AddAsync` con User válido persiste | `ef-user-repository.spec.md` |
| `GetByEmailAsync` con email existente retorna el usuario | `ef-user-repository.spec.md` |
| `GetByEmailAsync` es case-insensitive | `ef-user-repository.spec.md` |
| `GetByEmailAsync` con email inexistente retorna `null` | `ef-user-repository.spec.md` |
| `GetByIdAsync` con Id existente retorna el usuario | `ef-user-repository.spec.md` |
| `GetByIdAsync` con Id inexistente retorna `null` | `ef-user-repository.spec.md` |

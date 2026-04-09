# Spec: IAttemptRepository

**Tipo**: Interfaz de dominio (contrato de persistencia)  
**Ubicación**: `DevBrain.Domain`  
**Versión**: 1.0  

---

## Qué es

`IAttemptRepository` es la interfaz que define el contrato de persistencia para attempts dentro del dominio. Permite que la lógica de aplicación guarde y consulte attempts sin depender de EF Core ni de ninguna tecnología de base de datos. La implementación concreta vive en `DevBrain.Infrastructure`.

---

## Métodos

| Método | Retorno | Descripción |
|--------|---------|-------------|
| `AddAsync(Attempt attempt)` | `Task` | Persiste un nuevo attempt |
| `GetByUserAsync(string userId)` | `Task<IReadOnlyList<Attempt>>` | Retorna todos los attempts de un usuario, ordenados por `OccurredAt` descendente |
| `GetLastByUserAsync(string userId)` | `Task<Attempt?>` | Retorna el attempt más reciente del usuario, o `null` si no tiene ninguno |
| `CountCorrectByUserAsync(string userId)` | `Task<int>` | Retorna la cantidad de attempts correctos del usuario |

---

## Comportamientos del contrato

### AddAsync

- Persiste el attempt recibido
- El attempt ya fue validado por `Attempt.Create` — el repositorio no revalida reglas de dominio
- No retorna el attempt (el `Id` ya existe en el objeto recibido)

### GetByUserAsync

- Recibe un `string userId` (SupabaseId)
- Retorna todos los attempts del usuario ordenados por `OccurredAt` descendente (más reciente primero)
- Retorna lista vacía si el usuario no tiene attempts (no lanza excepción)
- No filtra por correctos/incorrectos — devuelve todos

### GetLastByUserAsync

- Retorna el attempt más reciente del usuario (`OccurredAt` más alto)
- Retorna `null` si el usuario no tiene ningún attempt (no lanza excepción)
- Usado principalmente por el servicio de streak para saber cuándo fue el último intento

### CountCorrectByUserAsync

- Retorna el total de attempts donde `IsCorrect = true` para el usuario
- Retorna `0` si el usuario no tiene attempts correctos (no lanza excepción)
- Usado para calcular estadísticas del usuario (`accuracy`, totales)

---

## Invariantes del contrato

1. Ningún método lanza `DomainException` — solo retornan `null` o lista vacía cuando no hay datos
2. `GetByUserAsync` nunca retorna `null` — como mínimo retorna lista vacía
3. La interfaz no conoce detalles de base de datos (sin `DbContext`, sin SQL, sin transacciones)
4. La implementación EF Core es responsabilidad de `DevBrain.Infrastructure`, nunca del dominio

---

## Qué NO es esta interfaz

- No valida que el `userId` exista como usuario registrado (eso es responsabilidad del servicio)
- No gestiona transacciones (eso es responsabilidad del servicio de aplicación)
- No actualiza ni elimina attempts — los attempts son inmutables una vez creados
- No contiene ninguna implementación — solo el contrato

---

## Ubicación en el proyecto

```
src/
  DevBrain.Domain/
    Interfaces/
      IChallengeRepository.cs   ← ya existe
      IAttemptRepository.cs     ← nueva interfaz
```

---

## Escenarios de test

> Los tests de `IAttemptRepository` se verifican en la spec de implementación  
> (`ef-attempt-repository.spec.md`) usando una base de datos real o en memoria.  
> Para esta spec, se crea solo la interfaz — sin tests en este paso.

| Escenario | Verificado en |
|-----------|---------------|
| `AddAsync` persiste el attempt | `ef-attempt-repository.spec.md` |
| `GetByUserAsync` retorna attempts ordenados descendente | `ef-attempt-repository.spec.md` |
| `GetByUserAsync` retorna lista vacía si no hay attempts | `ef-attempt-repository.spec.md` |
| `GetLastByUserAsync` retorna el attempt más reciente | `ef-attempt-repository.spec.md` |
| `GetLastByUserAsync` retorna `null` si no hay attempts | `ef-attempt-repository.spec.md` |
| `CountCorrectByUserAsync` retorna conteo correcto | `ef-attempt-repository.spec.md` |
| `CountCorrectByUserAsync` retorna `0` si no hay correctos | `ef-attempt-repository.spec.md` |

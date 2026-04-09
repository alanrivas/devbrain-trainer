# Spec: IChallengeRepository

**Tipo**: Interfaz de dominio (contrato de persistencia)  
**Ubicación**: `DevBrain.Domain`  
**Versión**: 1.0  

---

## Qué es

`IChallengeRepository` es la interfaz que define el contrato de persistencia para challenges dentro del dominio. Permite que la lógica de aplicación acceda y almacene challenges sin depender de EF Core ni de ninguna tecnología de base de datos. La implementación concreta vive en `DevBrain.Infrastructure`.

---

## Métodos

| Método | Retorno | Descripción |
|--------|---------|-------------|
| `GetByIdAsync(Guid id)` | `Task<Challenge?>` | Retorna el challenge con ese Id, o `null` si no existe |
| `GetAllAsync(ChallengeCategory? category, Difficulty? difficulty)` | `Task<IReadOnlyList<Challenge>>` | Lista todos los challenges, con filtros opcionales |
| `AddAsync(Challenge challenge)` | `Task` | Persiste un nuevo challenge |

---

## Comportamientos del contrato

### GetByIdAsync

- Recibe un `Guid id`
- Retorna `Challenge?` — `null` si no se encuentra (no lanza excepción)
- El caller es responsable de manejar el caso `null`

### GetAllAsync

- Ambos parámetros son opcionales (`null` = sin filtro)
- Si `category` es `null`, retorna challenges de todas las categorías
- Si `difficulty` es `null`, retorna challenges de todas las dificultades
- Ambos filtros se pueden combinar
- Retorna lista vacía si no hay resultados (no lanza excepción)

### AddAsync

- Persiste el challenge recibido
- El challenge ya fue validado por su factory method `Challenge.Create` — el repositorio no revalida reglas de dominio
- No retorna el challenge (el Id ya existe en el objeto recibido)

---

## Invariantes del contrato

1. `GetByIdAsync` nunca lanza `DomainException` — solo retorna `null` si no existe
2. `GetAllAsync` nunca retorna `null` — como mínimo retorna lista vacía
3. La interfaz no conoce detalles de base de datos (sin `DbContext`, sin SQL, sin transacciones)
4. La implementación EF Core es responsabilidad de `DevBrain.Infrastructure`, nunca del dominio

---

## Qué NO es esta interfaz

- No valida reglas de dominio (eso ya lo hizo `Challenge.Create`)
- No gestiona transacciones (eso es responsabilidad del servicio de aplicación)
- No actualiza ni elimina challenges (fuera del alcance del MVP)
- No contiene ninguna implementación — solo el contrato

---

## Ubicación en el proyecto

```
src/
  DevBrain.Domain/
    Interfaces/
      IChallengeRepository.cs   ← nueva interfaz
```

---

## Escenarios de test

> Los tests de `IChallengeRepository` se verifican en la spec de implementación  
> (`ef-challenge-repository.spec.md`) usando una base de datos real o en memoria.  
> Para esta spec, se crea solo la interfaz — sin tests en este paso.

| Escenario | Verificado en |
|-----------|---------------|
| `GetByIdAsync` con Id existente retorna el challenge | `ef-challenge-repository.spec.md` |
| `GetByIdAsync` con Id inexistente retorna `null` | `ef-challenge-repository.spec.md` |
| `GetAllAsync` sin filtros retorna todos | `ef-challenge-repository.spec.md` |
| `GetAllAsync` filtrando por `category` retorna solo esa categoría | `ef-challenge-repository.spec.md` |
| `GetAllAsync` filtrando por `difficulty` retorna solo esa dificultad | `ef-challenge-repository.spec.md` |
| `GetAllAsync` sin resultados retorna lista vacía (no null) | `ef-challenge-repository.spec.md` |
| `AddAsync` persiste el challenge | `ef-challenge-repository.spec.md` |

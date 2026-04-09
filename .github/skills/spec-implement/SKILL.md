---
name: spec-implement
description: Lee una .spec.md y ejecuta el ciclo completo SDD+TDD — genera tests xUnit, implementa el código para que pasen, verifica que todos estén en verde, actualiza context.md y sube los cambios a GitHub. Usar después de write-spec.
compatibility: Claude Code
allowed-tools: Read Write Edit Bash Glob
---

## Objetivo

Tomar una spec existente y completar el ciclo de implementación sin intervención manual:

```
spec.md → tests xUnit → implementación → dotnet test → update context.md → git commit → git push
```

## Cuándo usar

- El usuario dice "implementa la spec de X" o "spec-implement X"
- Después de crear una spec con `write-spec`
- Cuando quiere pasar directamente al verde sin pasos intermedios

## Paso 1 — Leer la spec

Leer el archivo `.spec.md` correspondiente en `specs/`. Si no se indica cuál, buscar la spec más reciente o preguntar.

Identificar:
- **Tipo**: entidad de dominio, endpoint, regla de negocio
- **Propiedades** y sus tipos C#
- **Comportamientos** (factory methods, métodos de instancia)
- **Invariantes** (qué debe lanzar `DomainException`)
- **Escenarios de test** de la tabla al final de la spec

## Paso 2 — Leer contexto existente

Antes de escribir código, leer:
- `context.md` — para entender el estado del proyecto
- Entidades existentes en `src/DevBrain.Domain/Entities/` — para detectar dependencias
- El `.csproj` del proyecto de tests — para verificar referencias

## Paso 3 — Generar los tests

**Proyecto**: `tests/DevBrain.Domain.Tests/` para entidades de dominio  
**Archivo**: `{Entidad}Tests.cs`

Reglas:
- Framework: xUnit con assertions nativas (`Assert.*`) — sin FluentAssertions ni NSubstitute
- Un método `[Fact]` por cada fila de "Escenarios de test esperados"
- Patrón de nombre: `{Comportamiento}_Given{Condicion}_Should{Resultado}`
- Incluir un helper privado `CreateValid()` para los tests que necesiten instancias válidas
- Usar `Assert.Throws<DomainException>()` para invariantes
- No usar mocks para entidades de dominio puras

Ejemplo de estructura:
```csharp
using DevBrain.Domain.Entities;
using DevBrain.Domain.Enums;
using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.Tests;

public class {Entidad}Tests
{
    private static {Entidad} CreateValid() => {Entidad}.Create(/* parámetros válidos */);

    [Fact]
    public void Create_GivenValidArguments_ShouldReturn{Entidad}()
    {
        var entity = CreateValid();
        Assert.NotNull(entity);
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Create_GivenEmpty{Campo}_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => {Entidad}.Create(/* campo vacío */));
    }
}
```

## Paso 4 — Implementar el código

### Para entidades de dominio

Crear archivos en `src/DevBrain.Domain/`:
- `Entities/{Entidad}.cs` — clase sellada con constructor privado y factory method `Create`
- `Enums/{NombreEnum}.cs` — si la spec define enums nuevos
- `Exceptions/DomainException.cs` — si no existe

Reglas de implementación:
- `sealed class`, constructor `private`
- Factory method `public static {Entidad} Create(...)` que valida y construye
- Propiedades con `{ get; }` (inmutables)
- `Guid.NewGuid()` y `DateTimeOffset.UtcNow` asignados internamente
- Validaciones con `throw new DomainException("mensaje")` antes de construir
- Métodos de instancia (`IsCorrectAnswer`, etc.) implementados según spec

### Para endpoints (specs/api/)

- Crear el endpoint en `src/DevBrain.Api/`
- Crear la interfaz del repositorio en `src/DevBrain.Domain/`
- Usar `WebApplicationFactory` de `Microsoft.AspNetCore.Mvc.Testing` para tests de integración

## Paso 5 — Agregar referencia si falta

Si el proyecto de tests no referencia el proyecto de dominio:
```bash
dotnet add tests/DevBrain.Domain.Tests/DevBrain.Domain.Tests.csproj reference src/DevBrain.Domain/DevBrain.Domain.csproj
```

## Paso 6 — Verificar en verde

```bash
dotnet test tests/DevBrain.Domain.Tests/
```

- Si algún test falla: leer el error, corregir la implementación, volver a correr
- No avanzar al siguiente paso hasta que todos estén en verde
- No modificar los tests para que pasen — modificar la implementación

## Paso 7 — Actualizar context.md

Marcar como completado en `context.md`:
- `- [x] Spec + implementación de {Entidad} (N tests en verde)`
- Actualizar "Último paso completado" con el resumen y el próximo paso

## Paso 8 — Commit y push a GitHub

Una vez que context.md está actualizado y todos los tests están en verde:

```bash
git add {archivos nuevos o modificados en este ciclo}
git commit -m "feat: spec + implementación de {Entidad} ({N} tests en verde)

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
git push origin main
```

Reglas:
- Nunca usar `git add .` — agregar solo los archivos de este ciclo (spec, tests, implementación, context.md)
- No incluir `bin/`, `obj/`, ni `.claude/`
- El mensaje de commit debe mencionar la entidad y la cantidad de tests

## Errores comunes

| Error | Causa probable | Solución |
|-------|---------------|----------|
| `CS0234: namespace no existe` | Falta referencia entre proyectos | `dotnet add reference` |
| `CS0246: tipo no encontrado` | Falta crear el archivo de la entidad o enum | Crear el archivo faltante |
| Test falla por `DomainException` no lanzada | Falta validación en `Create` | Agregar la guard clause |
| Test falla por comparación case-sensitive | `IsCorrectAnswer` no usa `OrdinalIgnoreCase` | Corregir la comparación |

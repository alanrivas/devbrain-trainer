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

## Paso 6 — Compilar y verificar tests (TODOS, no solo la suite nueva)

### 6a — Compilar la solución completa

```bash
dotnet build
```

- Debe terminar con `0 Errores`
- Las advertencias preexistentes (ej: SYSLIB0060 en PasswordHashService) son aceptables
- Si hay errores de compilación: corregir antes de continuar

### 6b — Correr TODOS los tests

```bash
dotnet test --no-build -q
```

- Verificar que las **tres suites** estén en verde: `Domain.Tests`, `Infrastructure.Tests`, `Api.Tests`
- El total acumulado debe coincidir con el nuevo conteo esperado
- Si algún test falla: leer el error, corregir la implementación, volver a correr desde 6a
- **Nunca modificar los tests para que pasen — modificar la implementación**

### 6c — Verificar que la API levanta en localhost

El puerto local está definido en `src/DevBrain.Api/Properties/launchSettings.json` — perfil `http`: **`http://localhost:5118`**.

```bash
dotnet run --project src/DevBrain.Api/ --no-build &
sleep 5
curl -s -o /dev/null -w "%{http_code}" http://localhost:5118/health
kill %1 2>/dev/null
```

- Debe retornar `200`
- Si retorna `000` (connection refused): la API no levantó — revisar logs de startup
- Si retorna `404`: revisar que `/health` esté registrado en `Program.cs`
- Matar el proceso con `kill %1` o `taskkill /F /IM dotnet.exe /T` antes de continuar

**No avanzar al commit si alguno de los tres pasos falla.**

## Paso 7 — Actualizar la colección Postman (solo specs de API)

**Solo aplica si la spec está en `specs/api/`.**

Leer `postman/devbrain-trainer.postman_collection.json` y agregar o actualizar el endpoint:

1. Ubicar la carpeta correcta en `item` (ej: "Challenges", "Users") — crear la carpeta si no existe
2. Agregar el request con:
   - `method`, `url` (usando `{{baseUrl}}`), `header` con `Authorization: Bearer {{bearerToken}}`
   - `body` en modo `raw` + `application/json` si es POST/PUT
   - `description` con una línea explicando qué hace el endpoint
3. Agregar ejemplos de respuesta (`response[]`) para **todos** los escenarios de la spec:
   - `200 OK` — todos los casos felices definidos en la spec (puede ser más de uno)
   - `400 Bad Request` — por cada validación que puede fallar
   - `401 Unauthorized` — siempre, si el endpoint requiere auth
   - `404 Not Found` — si aplica
   - Cada ejemplo con `body` JSON real (no placeholder genérico) acorde al contrato de la spec

Reglas:
- No modificar requests ni ejemplos de endpoints ya existentes
- Usar los mismos valores de ejemplo que se usaron en los tests de integración
- El body de los ejemplos debe ser JSON formateado con indentación de 2 espacios

## Paso 8 — Actualizar context.md

Marcar como completado en `context.md`:
- `- [x] Spec + implementación de {Entidad/Endpoint} (N tests en verde)`
- Actualizar "Último paso completado" con el resumen y el próximo paso

## Paso 9 — Commit y push a GitHub

Una vez que context.md está actualizado, los tests están en verde y la colección Postman actualizada (si aplica):

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

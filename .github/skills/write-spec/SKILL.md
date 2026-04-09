---
name: write-spec
description: Crea un archivo .spec.md siguiendo la metodología SDD del proyecto. Usar antes de implementar cualquier entidad de dominio, endpoint o regla de negocio. La spec define el contrato (qué), nunca el cómo.
compatibility: Claude Code
allowed-tools: Read Write Glob
---

## Objetivo

Crear un archivo `.spec.md` completo en la carpeta correcta de `specs/`. La spec es la fuente de verdad que guía los tests y la implementación posterior.

**Regla central**: la spec describe comportamiento y contratos, nunca código ni decisiones de implementación.

## Cuándo usar

- El usuario dice "escribe la spec de X" o "write-spec X"
- Antes de implementar cualquier entidad nueva
- Antes de definir un endpoint nuevo

## Paso 1 — Determinar tipo y ubicación

| Tipo | Carpeta | Ejemplo |
|------|---------|---------|
| Entidad de dominio | `specs/domain/` | `challenge.spec.md` |
| Endpoint de API | `specs/api/` | `get-challenges.spec.md` |
| Regla de gamificación | `specs/gamification/` | `streak.spec.md` |

## Paso 2 — Leer contexto existente

Antes de escribir la spec, leer:
- `context.md` — para entender el estado actual del proyecto
- Specs existentes en `specs/` — para mantener consistencia de estilo y evitar duplicar conceptos

## Paso 3 — Crear el archivo

Ruta: `specs/{subcarpeta}/{nombre-en-kebab-case}.spec.md`

Usar el template de abajo. Completar **todas** las secciones con información real y concreta del dominio de DevBrain Trainer. No dejar placeholders genéricos.

## Template

```markdown
# Spec: {Nombre}

**Tipo**: {Entidad de dominio | Endpoint | Regla de negocio}  
**Ubicación**: `{DevBrain.Domain | DevBrain.Api}`  
**Versión**: 1.0  

---

## Qué es

{Descripción en 2-3 oraciones. Qué representa en el dominio y cuál es su rol en la app.}

---

## Propiedades

| Propiedad | Tipo | Reglas |
|-----------|------|--------|
| `Id` | `Guid` | Generado al crear, inmutable |
| ... | ... | ... |

---

## Comportamientos

### {Comportamiento principal, ej: Creación}

- Se crea con `{Entidad}.Create(...)` 
- {Regla 1}
- {Regla 2}

### {Otro comportamiento}

- {Descripción}

---

## Invariantes (reglas que nunca se rompen)

1. {Invariante 1}
2. {Invariante 2}

---

## Qué NO es esta entidad

- {Límite 1 — qué responsabilidad pertenece a otra entidad}
- {Límite 2}

---

## Enums relacionados (si aplica)

```
// Solo referencia de valores, no es implementación
{NombreEnum}: Valor1, Valor2, Valor3
```

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| {Escenario feliz con datos válidos} | OK — objeto creado |
| {Campo vacío o nulo} | `DomainException` |
| {Valor fuera de rango} | `DomainException` |
| {Comportamiento retorna true} | `true` |
| {Comportamiento retorna false} | `false` |
```

## Convenciones

- Nombres de propiedades y tipos en inglés
- Descripciones y comentarios en español
- Los tipos C# deben ser válidos (`Guid`, `string`, `int`, `DateTimeOffset`, `bool`)
- `DomainException` para todas las violaciones de invariantes
- Factory method estático `Create(...)` para entidades (nunca constructor público)

## Después de crear la spec

Ejecutar el skill `spec-implement` para generar los tests y la implementación completa.

> **Nota para specs de API** (`specs/api/`): el skill `spec-implement` actualiza automáticamente  
> `postman/devbrain-trainer.postman_collection.json` al terminar la implementación.  
> No actualizar la colección manualmente antes de ese paso.

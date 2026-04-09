---
name: update-context
description: Updates the project context file (context.md at repo root) after completing a development iteration. Use when finishing a task, completing a spec, making tests pass, or ending a session. Marks completed steps, sets the last completed step, and defines the next action.
compatibility: Designed for Claude Code and GitHub Copilot. Requires Read and Write access to context.md at the repo root.
allowed-tools: Read Write Edit
---

## Objetivo

Mantener `context.md` (en la raíz del repo) actualizado al final de cada iteración para que cualquier sesión futura (con cualquier herramienta de IA) pueda continuar sin perder contexto.

## Cuándo usar este skill

- Al terminar de implementar una feature o fix
- Después de que los tests pasen
- Al terminar una sesión de trabajo
- Cuando el usuario dice "actualiza el contexto" o "update context"

## Pasos

1. **Leer el contexto actual**
   - Lee `context.md` en la raíz del repo

2. **Determinar qué se completó**
   - Revisá el historial de la conversación actual
   - Identificá qué tareas del plan se terminaron en esta sesión

3. **Actualizar checkboxes del plan**
   - Cambiá `- [ ]` a `- [x]` para cada tarea completada
   - No marques como completado lo que no se hizo

4. **Actualizar "Último paso completado"**
   - Reemplazá el bloque `> ...` con una descripción concisa de lo que se hizo
   - Incluí el resultado concreto (ej: "Tests de Challenge pasan: 4/4")
   - Incluí el próximo paso recomendado

5. **Escribir el archivo actualizado**
   - Preservá toda la estructura y secciones existentes
   - Solo modificá lo que corresponde al progreso real

## Formato del bloque "Último paso completado"

```markdown
## Último paso completado
> {Descripción de lo que se hizo en esta iteración}.  
> Próximo paso: {acción concreta que sigue}.
```

## Ejemplo

Antes:
```markdown
- [ ] Definir specs del dominio (Challenge, Category, Attempt)
- [ ] Implementar modelos con TDD

## Último paso completado
> Repo creado y clonado. Próximo paso: crear solución ASP.NET Core 10.
```

Después (si se completaron las specs):
```markdown
- [x] Definir specs del dominio (Challenge, Category, Attempt)
- [ ] Implementar modelos con TDD

## Último paso completado
> Specs de dominio creadas: Challenge, Category, Attempt en specs/domain/.  
> Próximo paso: implementar modelos con TDD partiendo de specs/domain/challenge.spec.md.
```

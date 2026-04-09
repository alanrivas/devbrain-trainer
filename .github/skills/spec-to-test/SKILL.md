---
name: spec-to-test
description: DEPRECADO — usar spec-implement en su lugar. spec-implement hace el ciclo completo (tests + implementación + verificación).
compatibility: Claude Code
allowed-tools: Read
---

## Este skill fue reemplazado

Usar **`spec-implement`** en su lugar.

`spec-implement` ejecuta el ciclo completo:
- Lee la spec
- Genera los tests xUnit
- Implementa el código
- Corre `dotnet test` y verifica verde
- Actualiza `context.md`

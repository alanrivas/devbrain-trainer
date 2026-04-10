# Session Validator Agent

Agente autónomo que valida estado del proyecto al iniciar nueva sesión.

## Cuando llamarlo

Desde la sesión anterior antes de terminar:

```
Usuario → Copilot:
"Cuando se reinicie la sesión, quiero que ejecutes el agente session-validator
 para que revises todo y me digas si está todo sincronizado"
```

O en nueva sesión:

```
Usuario → Claude:
(runSubagent prompt)
"Ejecuta session-validator para saber el estado del proyecto"
```

## Lo que hace

```
1. Lee context.md (last 80 lines)
2. git status           → ¿working tree clean?
3. git log --oneline    → ¿últimos commits matchean context?
4. dotnet test --no-build -c Release --logger "console;verbosity=minimal"
   → ¿todos los tests pasan?
5. Busca specs/**/*.spec.md pendientes (no implementadas aún)
6. Genera reporte estructurado:
   
   ✅ Project Validation Report
   
   **Git Status**: clean ✅
   **Tests**: 212/212 passing ✅
   **context.md**: synchronized with reality ✅
   **Last Commit**: e033e60 (2 hours ago)
   **Current Phase**: 3.2 complete, Phase 3.3 ready
   
   **What's Next**:
   Phase 3.3 — Endpoint Logging Integration
   (See context.md line 47: "Próximo paso...")
   
   **Actionable**:
   → Ready to start Phase 3.3 implementation
   → Or continue with pending work
```

## Cómo invocarlo

### Via runSubagent

```
(Usar herramienta: runSubagent)
agentName: "session-validator"
description: "Validar estado del proyecto"
prompt: """Ejecuta este agente para validar que el proyecto está en estado sincronizado:

1. Lee context.md (últimas 50 líneas)
2. Ejecuta: git status, git log --oneline -5
3. Corre: dotnet test --no-build -c Release --logger "console;verbosity=minimal"
4. Genera reporte comparando context.md con estado real
5. Identifica el siguiente paso según context.md

Reporta al usuario:
- ✅ si todo está sincronizado
- ❌ y qué está desincronizado
- → Próximo paso a ejecutar
"""
```

---

## Ventajas vs Skill Manual

| Aspecto | Skill Manual | Agente |
|---|---|---|
| **Control** | User maneja todos los pasos | Agente hace todo automático |
| **Errores** | User puede saltarse pasos | Agente sigue checklist completo |
| **Reporte** | User resume manualmente | Agente genera reporte estructurado |
| **Análisis** | User interpreta resultados | Agente sugiere próximo paso |
| **Tiempo** | 5-10 min manual | 2-3 min automático |

---

## Caso de Uso Real

Sesión 1 (fue muy larga, tokens llenos):
```
Usuario (final de sesión 1):
"Copilot, en la próxima sesión ejecuta el agente session-validator 
 para que sepas dónde estamos y qué hacer"
```

Sesión 2 (nueva):
```
Usuario (inicio de sesión 2):
"Ejecuta session-validator"

Copilot runSubagent → session-validator →
✅ Project Validation Report
✅ Git: clean, branch main
✅ Tests: 212/212 ✅
✅ context.md: Up-to-date
→ Next: Phase 3.3 ready

Usuario: "Perfecto, empezamos Phase 3.3, lee la spec..."
```

Sin agente (antes):
```
Usuario (sesión 2):
"Revisa context.md, verifica git, corre tests, dime estado"

(Multiplepasos manuales, user describes, repite)
→ 10 min de pre-requisites
→ Luego: "OK empecemos"
```

---

## Integración en Instructions

Para que el agente se lance automáticamente, actualizá:

### CLAUDE.md

```markdown
## Agente disponible: `session-validator`

Especialista en validación de estado inicial del proyecto.
Ejecutalo AL INICIAR cada sesión para asegurar sincronización.

**Cuándo usarlo**: Siempre al comenzar nueva sesión (después de que anterior fue cerrada por token limit)

**Invocación**:
runSubagent(agentName="session-validator", ...)
```

### copilot-instructions.md

```markdown
## Protocolo: Validación de Sesión

Cada vez que inicies una sesión nueva en este workspace:

1. El usuario dirá algo como: "revisa estado" o "validá proyecto"
2. Ejecuta el agente `session-validator`
3. Espera resultado
4. Reporta al usuario el estado + próximo paso

Si el usuario no pide explícitamente, puedes ofrecer:
"Quieres que ejecute session-validator para revisar estado del proyecto?"
```

---

## Salida Esperada del Agente

```
## 📋 Project Validation Report — Session Initialization

### System Status
✅ Git Repository
  └─ Branch: main
  └─ Status: working tree clean
  └─ Last commit: e033e60 (docs: comprehensive documentation) — 2 hours ago

✅ Test Suite
  └─ Total: 212/212 passing
  └─ Domain.Tests: 69/69 ✅
  └─ Infrastructure.Tests: 58/58 ✅
  └─ Api.Tests: 83/83 ✅
  └─ Integration.Tests: 2/2 ✅

✅ Project Context
  └─ context.md: Synchronized ✅
  └─ Current Phase: 3.2 (Serilog + Application Insights) — COMPLETE
  └─ Last documented step: Deploy to Azure — COMPLETE

### Validation Results
✅ All systems nominal
✅ No pending uncommitted changes
✅ No failing tests
✅ context.md matches reality

### Next Actions (per context.md)

📌 **Phase 3.3 Ready**: Endpoint Logging Integration
   - Add ILogger<T> to all endpoints
   - Integrate Serilog.LogContext for request tracking
   - Validate all 212+ tests still pass
   - Estimate: 3-4 hours

📌 **Estimated sequence**:
   1. Write spec: specs/api/endpoint-logging.spec.md
   2. Create tests: 8-12 new test methods
   3. Implement logging: add ILogger<T> to 8 endpoints
   4. Update context.md
   5. Commit + push

### Recommendation
✅ Project is ready to continue development
✅ Proceed with Phase 3.3 as planned
```

---

## Limitaciones & Notas

- Agente puede no tener acceso a todos los hermit commands (OS dependent)
- En Windows puede necesitar PowerShell específico
- Si Docker/PostgreSQL/Redis no están corriendo, algunos tests pueden darse omitidos
- Mensaje: "⚠️ Some integration tests skipped (Docker/Services not available)" es normal en sesión inicial

---

## Relacionado

- **Skill**: `.github/skills/session-startup/SKILL.md` (versión manual si necesitas acción granular)
- **Update**: `.github/skills/update-context/SKILL.md` (para actualizar estado después)

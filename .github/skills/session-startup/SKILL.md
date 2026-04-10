# Session Startup Skill

**Propósito**: Validar que el proyecto está en estado consistente al iniciar una nueva sesión y establecer el contexto correcto.

**Cuándo usarlo**: SIEMPRE al iniciar una nueva sesión de Claude/Copilot (cuando la anterior se termina por límite de tokens).

---

## Workflow

### Paso 1: Leer `context.md`

```bash
# Verificar que exista y está actualizado
cat context.md | head -50
```

**Qué revisar**:
- `## Estado actual` — Checkboxes de features completadas
- `## Test Suites Status` — Número de tests y estado (✅ XXX/XXX)
- `## Último paso completado` — Descripción de qué se hizo y próximo paso
- `## Stack decidido` — Tecnologías definidas

### Paso 2: Verificar Estado Real

```bash
# 1. Git status: ¿hay cambios uncommitted?
git status

# 2. Commits pending: ¿últimos commits reflejan lo en context.md?
git log --oneline -10

# 3. Ejecutar tests: ¿todos pasan?
dotnet test --no-build -c Release --logger "console;verbosity=minimal"

# 4. Estructura: ¿archivos esperados existen?
ls -la .github/skills/
ls -la database/setup/  # Si existen migraciones
ls -la docs/*/
```

### Paso 3: Generar Reporte de Validación

Comparar context.md con estado real. Si hay discrepancias, documentarlas:

| Item | Según context.md | Estado Real | ¿Sincronizado? |
|------|---|---|---|
| Total tests | 212/212 ✅ | `dotnet test` result | ✅ o ❌ |
| Último commit | e033e60 (docs...) | `git log --oneline -1` | ✅ o ❌ |
| Branch actual | main | `git status` | ✅ o ❌ |
| Working tree | clean | `git status` | ✅ o ❌ |

### Paso 4: Determinar Próximo Paso

Basado en `## Último paso completado`:
- Si dice "Próximo paso: Phase 3.3...", comienza ese trabajo
- Si dice "Pending commit", ejecutar `git push`
- Si hay discrepancias, resolver primero antes de continuar

---

## Ejecución Automática

Ideal para incluir en instrucciones de IA:

```markdown
# Instrucciones para sesión inicial

Cuando inicies una nueva sesión:

1. Ejecuta el skill `session-startup` (ver abajo)
2. Valida que todo esté sincronizado
3. Identifica el próximo paso
4. Comienza el trabajo

### Ejecutar Session Startup

Lee primero el archivo `.github/skills/session-startup/SKILL.md` para entender el proceso.

Luego ejecuta estos comandos en orden:

\`\`\`bash
# 1. Leer context.md (últimas 30 líneas con status)
tail -30 context.md

# 2. Ver estado de git
git status
git log --oneline -5

# 3. Ejecutar tests
dotnet test --no-build -c Release --logger "console;verbosity=minimal"

# 4. Resumir hallazgos
echo "✓ Session startup validation complete"
\`\`\`
```

---

## Casos Comunes

### Caso A: Todo Sincronizado

```
✅ context.md está actualizado
✅ git status: working tree clean
✅ 212/212 tests passing
✅ Branch: main, último commit: e033e60 (hace 2h)

→ SIGUIENTE: Proceder con próximo step descrito en context.md
```

### Caso B: Tests Failing

```
❌ context.md dice 212/212 ✅
❌ Aquí fallan: 3 tests
   - TestX en namespace Y
   - TestZ en namespace W

→ SIGUIENTE: Debuggear failing tests ANTES de continuar
→ Ejecutar: dotnet test --filter "TestX" --no-build -c Debug
```

### Caso C: Cambios No Commiteados

```
❌ git status: Changes not staged
   - src/DevBrain.Api/Program.cs (modificado)
   - README.md (modificado)

→ SIGUIENTE: ¿Estos cambios son intencionales?
→ Si SÍ: git add + git commit
→ Si NO: git checkout para revertir
```

### Caso D: context.md Desactualizado

```
❌ context.md dice "Phase 3.2 completado"
❌ Pero git log muestra 5 commits nuevos sin documentar
❌ Tests: 225/225 ✅ (pero context dice 212)

→ SIGUIENTE: Ejecutar `update-context` skill para sincronizar
→ Commit: git commit -m "docs: update context.md after session sync"
```

---

## Plantilla de Reporte

Cuando valides, documenta así en tu respuesta al usuario:

```markdown
## 📋 Session Startup Validation

✅ **Project Status**: Ready to continue

### Breakdown:
- Tests: 212/212 passing
- Git: main branch, working tree clean
- context.md: Up-to-date (Phase 3.2 complete)
- Last commit: e033e60 (docs: comprehensive documentation)

### What's Next:
According to context.md, Phase 3.3 should begin:
> Phase 3.3 — Endpoint Logging Integration
> Add ILogger<T> to endpoints, integrate Serilog calls

### Action:
Ready to proceed with Phase 3.3 implementation.
```

---

## Automatización Futura

Ideal para crear un GitHub Action que corra al iniciar cada PR:

```yaml
name: Session Startup Validation

on:
  workflow_dispatch:  # Manual trigger

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Run Tests
        run: dotnet test --no-build -c Release
      - name: Validate context.md
        run: |
          echo "Context.md validation"
          cat context.md | grep "## Test Suites Status" -A 20
```

---

## Próximo Relacionado: `update-context`

Después de validar todo, usa el skill [`update-context`](../.../../update-context/SKILL.md) para:
- Actualizar test counts si cambiaron
- Marcar features como completadas
- Documentar próximo paso
- Crear commit automático

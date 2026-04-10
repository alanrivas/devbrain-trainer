# Agents & Skills Reference — DevBrain Trainer

Guía completa de todos los agentes y skills disponibles con ejemplos de uso.

---

## ¿Qué es un Skill?

Un **skill** es un workflow estructurado (procedimiento paso a paso) para automatizar tareas de desarrollo.

**Ubicación**: `.github/skills/{nombre}/SKILL.md`  
**Propósito**: Ejecutable por Claude/Copilot mediante `read_file` + instrucciones

---

## ¿Qué es un Agente?

Un **agente** es un sistema autónomo que se encargade completar una tarea compleja sin intervención manual.

Ejemplos de agentes:
- `Explore` (búsqueda de código)
- `static-site-deployer` (deploy a GitHub Pages)
- `railroad-deployer` (deploy a Railway)

**Ventaja**: El agente maneja múltiples subtareas complejas internamente.

---

## 📋 Skills del Proyecto DevBrain

### 1. `session-startup` — Validar Estado de Sesión

**Archivo**: `.github/skills/session-startup/SKILL.md`

**Propósito**: Validar que el proyecto está en estado consistente al iniciar nueva sesión.

**Cuándo usarlo**:
- ✅ Siempre al iniciar sesión de Claude/Copilot  
- ✅ Cuando reinicia por límite de tokens
- ✅ Para sincronizar context.md con estado real

**Pasos internos**:
1. Leer `context.md` (mira `## Último paso completado`)
2. Ejecutar `git status` (verifica git limpio)
3. Ejecutar `dotnet test` (valida tests pasan)
4. Comparar: ¿context.md === estado real?
5. Reportar discrepancias
6. Sugerir siguiente paso

**Ejemplo de uso**:

```
Usuario → Copilot: "nueva sesión, revisa estado"

Asistente:
1. Lee .github/skills/session-startup/SKILL.md
2. Ejecuta:
   - tail -30 context.md
   - git status
   - git log --oneline -5
   - dotnet test --no-build -c Release --logger "console;verbosity=minimal"
3. Reporta:
   ✅ Project Status: Ready to continue
   ✅ Tests: 212/212 passing
   ✅ Git: main branch, working tree clean
   ✅ context.md: up-to-date (Phase 3.2 complete)
   
   → Próximo paso (según context.md):
   Phase 3.3 — Endpoint Logging Integration
```

**Salida esperada**:
```
## 📋 Session Startup Validation

✅ Tests: 212/212 passing
✅ Git: clean, main branch
✅ context.md: synchronized
✅ Ready to continue with Phase 3.3
```

---

### 2. `update-context` — Actualizar Documentación de Estado

**Archivo**: `.github/skills/update-context/SKILL.md`

**Propósito**: Actualizar `context.md` al completar una iteración.

**Cuándo usarlo**:
- ✅ Al terminar una feature o phase
- ✅ Al hacer tests pasar (actualizar count)
- ✅ Al preparar para cleanup o commit final

**Pasos internos**:
1. Leer estado actual del proyecto
2. Actualizar `context.md`:
   - `## Estado actual`: marcar checkboxes completados
   - `## Test Suites Status`: actualizar números si cambiaron
   - `## Último paso completado`: documentar qué se hizo, próximo paso
3. Hacer commit automático
4. Sugerir push

**Ejemplo de uso**:

```
Usuario → Copilot: "actualizá context.md con Phase 3.2 completado"

Asistente:
1. Lee .github/skills/update-context/SKILL.md
2. Ejecuta:
   - dotnet test --no-build para verificar counts
   - Analiza specs/ para listar features nuevas
   - Lee último commit message para contexto
3. Actualiza context.md:
   - [x] Phase 3.2: Serilog infrastructure
   - Tests: 207 → 212
   - Próximo paso: Phase 3.3...
4. Commits:
   git commit -m "docs: update context.md — Phase 3.2 complete, 212/212 tests"
5. Sugiere: "git push"

Result:
✅ context.md actualizado
✅ Changes committed
```

**Archivo antes**:
```markdown
## Último paso completado
> (outdated info about Phase 3.1)
```

**Archivo después**:
```markdown
## Último paso completado
> ✅ **Phase 3.2: Serilog + Application Insights** — COMPLETADO
> 
> **Resumen**: 212/212 tests, multi-sink logging, Azure deployment
> **Próximo paso**: Phase 3.3 — Endpoint logging integration
```

---

### 3. `write-spec` — Crear Specification SDD

**Archivo**: `.github/skills/write-spec/SKILL.md`

**Propósito**: Crear `.spec.md` siguiendo metodología SDD (sin implementación).

**Cuándo usarlo**:
- ✅ ANTES de escribir tests
- ✅ Para documentar contrato de feature (qué, no cómo)
- ✅ Entidades de dominio, endpoints, servicios

**Pasos internos**:
1. Analizar requisito desde usuario
2. Crear `specs/{tipo}/{nombre}.spec.md`
   - `## Purpose`
   - `## Contracts` (inputs/outputs)
   - `## Error Cases` (tabla)
   - `## Invariantes` (reglas de negocio)
3. No incluir código de implementación
4. Validar ortografía y coherencia

**Ejemplo de uso**:

```
Usuario → Claude: "crea spec para nuevo endpoint POST /leaderboard/my-rank"

Asistente:
1. Lee .github/skills/write-spec/SKILL.md
2. Crea: specs/api/post-leaderboard-my-rank.spec.md
3. Contenido:
   - Purpose: User gets their rank/percentile among all players
   - Request: GET /leaderboard/me (no body needed)
   - Response: { rank: 42, percentile: 87.5, totalPlayers: 500 }
   - Error cases: User not found (404), Not authenticated (401)
   - Invariantes: Rank es 1-based, percentile es 0-100
4. Commit: git add specs/api/...
   
Resultado:
✅ Spec created
✅ Ready for spec-implement skill
```

**El spec define**:
- ✅ QUÉ (contrato HTTP)
- ❌ No HOW (no código)

---

### 4. `spec-implement` — Implementar Spec Completa (TDD)

**Archivo**: `.github/skills/spec-implement/SKILL.md`

**Propósito**: Lee `.spec.md` y ejecuta ciclo SDD+TDD completo (tests + implementación).

**Cuándo usarlo**:
- ✅ DESPUÉS de `write-spec`
- ✅ Para convertir spec en código con tests obligatorios

**Pasos internos**:
1. Lee `.spec.md` (la spec que escribiste con write-spec)
2. Genera tests basados en spec cases
3. Implementa código para que tests pasen
4. Verifica: `dotnet test` 100%
5. Actualiza `context.md`
6. Hace commit + push

**Ejemplo de uso**:

```
Usuario → Claude: "implementá spec/api/post-leaderboard-my-rank.spec.md"

Asistente:
1. Lee .github/skills/spec-implement/SKILL.md
2. Lee specs/api/post-leaderboard-my-rank.spec.md
3. Genera tests en <Tests/LeaderboardTests.cs>:
   - GetLeaderboardMyRank_WithValidUser_Returns200AndRank
   - GetLeaderboardMyRank_WithNonExistentUser_Returns404
   - etc (4 tests total)
4. Implementa endpoint:
   app.MapGet("/api/v1/leaderboard/me", GetMyRank)
   async Task<Result> GetMyRank(ILeaderboardService svc, ...) { ... }
5. Ejecuta: dotnet test
   ✅ 4/4 tests green
   ✅ Total: 216/216 tests (was 212)
6. Actualiza context.md:
   - [x] Spec: post-leaderboard-my-rank.spec.md
   - [x] Endpoint GET /leaderboard/me (4 tests)
   - [x] Fase 3.4 completada
7. Commits:
   git commit -m "feat: leaderboard endpoint — Phase 3.4 (216/216 tests)"
   git push

Resultado:
✅ Spec → Tests (red)
✅ Implementation (green)
✅ All tests passing
✅ context.md updated
✅ Pushed to GitHub
```

**Workflow SDD+TDD**:
```
spec-implement automatiza esto:
  spec.md (escrito manualmente)
    ↓
  Genera tests del spec
    ↓
  Red: Tests fallan (sin implementación)
    ↓
  Implementation
    ↓
  Green: Tests pasan
    ↓
  Updated context.md
    ↓
  Commit + push
```

---

## 🤖 Agentes del Proyecto DevBrain

### 1. `Explore` — Búsqueda y Análisis de Código

**Ubicación**: Agente built-in de Copilot

**Propósito**: Buscar y analizar código sin clutterear conversation.

**Parámetros**:
- `query` — Qué buscar
- `thoroughness` — `quick` | `medium` | `thorough`

**Ejemplo de uso**:

```
Usuario → Copilot: "@Explore ¿dónde está la lógica de ELO rating?"

Explore busca y reporta:
✅ Found in: src/DevBrain.Domain/Services/EloRatingService.cs
   - Public method: CalculateEloChange(int currentElo, bool isCorrect, int difficulty)
   - Formula: newElo = currentElo + K * (actual - expected)
   - K-factor: 32 for standard users, 24 for high-rating
   
✅ Tests: tests/DevBrain.Domain.Tests/EloRatingServiceTests.cs (12 tests)
✅ Usage: EFAttemptRepository updates user ELO after attempt
```

**Cuándo usarlo**:
- Understanding existing code
- Finding where to add logging
- Searching for usage patterns

---

### 2. Agentes de Deployment

#### `static-site-deployer` — Desplegar Sitio Estático a GitHub Pages

**Propósito**: Setup completo de Docusaurus → GitHub Pages + Cloudflare DNS.

**Cuándo usarlo**: Crear nuevo sitio de docs/blog.

**Ejemplo**:
```
Usuario: "Deploy docs site con dominio docs.alanrivas.me"

Agente:
1. Verifica Docusaurus existe
2. Configura docusaurus.config.ts
3. Crea static/CNAME
4. Crea .github/workflows/deploy.yml
5. Verifica DNS en Cloudflare
6. Test HTTPS
7. Reporta: "✅ Deployed to https://docs.alanrivas.me"
```

---

#### `railway-deployer` — Desplegar API a Railway

**Propósito**: Deploy ASP.NET Core a Railway via Docker.

**Cuándo usarlo**: Migrar del entorno local a Railway.

**Ejemplo**:
```
Usuario: "Deploy backend a Railway"

Agente:
1. Crea Dockerfile (si no existe)
2. Crea railway.json
3. Linkea Railway CLI
4. Setea env vars (DB, Redis, secrets)
5. Deploy via railway CLI
6. Valida endpoint está vivo
7. Reporta: "✅ API live at https://devbrain-api.railway.app"
```

---

## 🔄 Flujo Completo Recomendado

### Ciclo de Desarrollo Estándar

```
1️⃣ Nueva Sesión
   → Ejecutas: session-startup skill
   → Lee: context.md
   → Valida: estado real vs context.md
   → Reporta: "✅ Ready, next step is Phase 3.3"

2️⃣ Diseñar Feature
   → Usuario: "crea spec para XYZ"
   → write-spec skill
   → Resultado: specs/api/xyz.spec.md ✅

3️⃣ Implementar Feature
   → Usuario: "implementá xyz.spec.md"
   → spec-implement skill
      ├─ Lee spec
      ├─ Genera tests (red)
      ├─ Implementa (green)
      ├─ Valida 100%
      ├─ Actualiza context.md
      └─ Commit + push

4️⃣ Sesión Termina (tokens llenos)
   → Siguiente sesión vuelve a 1️⃣
```

---

## 📚 Estructura de Skills

Cada skill tiene esta estructura:

```
.github/skills/{skill-name}/
├── SKILL.md              ← Instrucciones (leídas por Copilot)
├── template.md           ← Template (si aplica)
└── examples/             ← Ejemplos de uso (opcional)
```

**Leer skill**:
```
Copilot: "Lee el skill write-spec"
→ read_file('.github/skills/write-spec/SKILL.md')
→ Sigue instrucciones paso a paso
```

---

## 🎯 Checklist: Cómo Usar Skills Correctamente

- [ ] **Antes de cualquier tarea**: Ejecuta `session-startup`
- [ ] **Antes de implementar**: Lee el skill relevante con `read_file`
- [ ] **Durante el trabajo**: Sigue instrucciones del skill
- [ ] **Al terminar**: Usa `update-context` para documentar
- [ ] **Antes de terminar sesión**: Commit + push

---

## 📖 Referencias Rápidas

| Necesitas hacer... | Usa este skill/agente | Ubicación |
|---|---|---|
| Validar estado al iniciar sesión | `session-startup` | `.github/skills/session-startup/SKILL.md` |
| Actualizar context.md después de terminar | `update-context` | `.github/skills/update-context/SKILL.md` |
| Crear doc de requisitos para feature | `write-spec` | `.github/skills/write-spec/SKILL.md` |
| Implementar feature (spec → tests → code) | `spec-implement` | `.github/skills/spec-implement/SKILL.md` |
| Buscar código existente | `Explore` agente | Built-in Copilot |
| Deploy a GitHub Pages | `static-site-deployer` | `.claude/skills/deploy-gh-pages/SKILL.md` |
| Deploy a Railway | `railway-deployer` | `.claude/skills/deploy-railway/SKILL.md` |

---

## 💡 Pro Tips

### Tip 1: Leer Skill Primero
Siempre, SIEMPRE lee el skill `.md` antes de pedir que lo ejecute:
```
"Copilot, lee .github/skills/spec-implement/SKILL.md"
→ (Copilot lee)
"Ahora ejecutá spec-implement para xyz"
```

### Tip 2: Combinació Skill + Agente
A veces necesitas skill + agente:
```
"Usa Explore para encontrar donde está EloRatingService,
 luego lee la spec de EloRating en specs/gamification/elo-rating.spec.md"
```

### Tip 3: Si Falla un Skill
Si skill falla, verifica:
1. Archivos relacionados existen
2. Lei el `.md` del skill completamente
3. Status del proyecto es correcto (tests pasan)
4. Si sigue fallando: corre `session-startup` para diagnosticar

---

## 🚀 Próximas Mejoras

- [ ] Crear skill `validate-schema` para revisar migraciones EF Core
- [ ] Agregar skill `generate-docs-from-endpoints` para auto-gen swagger
- [ ] Agente `performance-profiler` para análisis de tests lentos
- [ ] Integración CI/CD que corre skills automáticamente

---

**Última actualización**: 10 de Abril de 2026 (Phase 3.2)  
**Próximo step**: Phase 3.3 — Endpoint Logging Integration

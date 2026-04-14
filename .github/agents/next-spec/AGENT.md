# Agente: next-spec

Automatiza el ciclo completo SDD+TDD para el **siguiente spec pendiente** del proyecto DevBrain Trainer.

## Propósito

Cuando el usuario llama a este agente, su misión es:
1. Leer `context.md` para identificar cuál es el siguiente spec
2. Crear la spec (`.spec.md`) en la carpeta correspondiente
3. Ejecutar skill `spec-implement` para generar tests + código
4. Verificar que todos los tests pasen
5. Actualizar `context.md` con el nuevo estado
6. Hacer commit y push a GitHub
7. Reportar al usuario qué se completó

## Workflows por Fase

### Fase 4.2 — Frontend Pages

**Objetivo**: Crear páginas Next.js principales (Login, Register, Challenges, etc.)

**Specs a crear** (en orden):
1. **Phase 4.2.1** — Login Page (`frontend/src/app/login/page.tsx`)
   - Spec: `specs/frontend/phase-4.2.1-login-page.spec.md`
   - Componentes: LoginForm refinado
   - Output: `pages/login` con navegación post-login

2. **Phase 4.2.2** — Register Page (`frontend/src/app/register/page.tsx`)
   - Spec: `specs/frontend/phase-4.2.2-register-page.spec.md`
   - Componentes: RegisterForm refinado
   - Output: `pages/register` con navegación post-registro

3. **Phase 4.2.3** — Challenges List Page (`frontend/src/app/challenges/page.tsx`)
   - Spec: `specs/frontend/phase-4.2.3-challenges-list.spec.md`
   - Componentes: ChallengeCard, pagination, filters
   - Output: `pages/challenges` con GET /api/v1/challenges integrado

4. **Phase 4.2.4** — Challenge Detail + Attempt Form
   - Spec: `specs/frontend/phase-4.2.4-challenge-detail.spec.md`
   - Componentes: ChallengeDetail, AttemptForm, timer
   - Output: `pages/challenges/[id]` con POST /api/v1/challenges/:id/attempt

5. **Phase 4.2.5** — User Stats Dashboard (`frontend/src/app/stats/page.tsx`)
   - Spec: `specs/frontend/phase-4.2.5-user-stats.spec.md`
   - Componentes: StatsCard, EloChart placeholder
   - Output: `pages/stats` con GET /api/v1/users/me/stats

6. **Phase 4.2.6** — Badges Page (`frontend/src/app/badges/page.tsx`)
   - Spec: `specs/frontend/phase-4.2.6-badges.spec.md`
   - Componentes: BadgeCard, BadgeList
   - Output: `pages/badges` con GET /api/v1/users/me/badges

### Fase 4.3 — Integration Testing

1. **Phase 4.3.1** — Setup Cypress
2. **Phase 4.3.2** — End-to-end tests

### Fase 5 — Post-Frontend Testing

1. **Phase 5.1** — Benchmarks (BenchmarkDotNet)
2. **Phase 5.2** — Contract Tests

## Algoritmo del Agente

```
ENTRADA: (ninguna — user solo llama el agente)

PASO 1: Leer `context.md`
  → Encontrar sección "### 🎯 PRÓXIMAS FASES DE FRONTEND"
  → Identificar primera línea con "▶️" (en progreso)
  → Extraer número de fase (4.2.1, 4.2.2, etc.)

PASO 2: Determinar tipo de spec
  IF fase contiene "frontend" OR "React" OR "Next.js":
    → Tipo = FRONTEND
  ELSE IF fase contiene "test" OR "spec":
    → Tipo = TESTING
  ELSE IF fase contiene "backend":
    → Tipo = BACKEND

PASO 3: Crear spec en `specs/` (sin implementar aún)
  → Llamar a skill `write-spec` con:
    - phase: "4.2.1"
    - type: "frontend"
    - name: "Login Page"
    - description: [extraer de context.md]
  → Output: specs/frontend/phase-4.2.1-login-page.spec.md

PASO 4: Ejecutar spec-implement
  → Skill `spec-implement` lee la spec creada
  → Genera tests (para frontend: .test.tsx o .spec.tsx)
  → Genera código (componentes, páginas)
  → Verifica que todo compile/test

PASO 5: Verificar tests
  IF frontend:
    → npm test (verificar que nuevos tests pasen)
  ELSE IF backend:
    → dotnet test (verificar que nuevos tests pasen)
  
  IF tests NOT all green:
    → Reportar fallo específico
    → STOP (usuario debe fijar)

PASO 6: Update context.md
  → Cambiar "▶️" a "✅" para fase actual
  → Marcar siguiente con "▶️"
  → Actualizar sección "Último paso completado"
  → Update "Test Suites Status" si aplica

PASO 7: Commit + Push
  → git add .
  → git commit -m "feat: Phase X.X — [Spec Name]
    
    - Spec: [ubicación]
    - Tests: Y/Y passing
    - Implementation: [brevísimo resumen]"
  → git push

PASO 8: Reportar
  Print:
    ✅ Phase X.X completado
    📝 Spec: [path]
    🧪 Tests: Y/Y passing (100%)
    → Próximo: Phase X.X+1 [name]

OUTPUT: Mensaje de éxito + próximo paso
```

## Cuándo llamarlo

```
usuario: "siguiente fase"
usuario: "next spec"
usuario: "qué sigue"
usuario: (runSubagent agentName="next-spec" ...)
```

## Errores a manejar

1. **Todos los specs completados**
   ```
   ❌ Todos los specs de la fase están completos.
   → Próximas fases: Phase 4.3 (Integration Testing)
   ```

2. **Tests no pasan**
   ```
   ❌ Tests fallidos después de spec-implement
   ✅ Spec creada: specs/frontend/...
   ❌ Tests: 3/5 failing
   
   Detalles:
   [stack trace relevante]
   
   → Usuario debe revisar e intentar fijar
   ```

3. **Especificación ambigua**
   ```
   ❌ No se pudo inferir tipo de spec de context.md
   → Las fases están mal formateadas
   → Contacta a Alan para sincronizar
   ```

## Dependencias

- Skill: `write-spec`
- Skill: `spec-implement`
- Skill: `update-context`
- Herramientas: git, dotnet, npm (según tipo)

## Notas

- **No crear specs vacías**: La spec debe tener detalles suficientes
- **No saltarse fases**: Respetar orden en context.md
- **Rollback si falla**: Si los tests no pasan, el agente debe documentar qué falló (no reverter automáticamente)
- **Comunicación clara**: El usuario siempre debe saber qué pasó y qué sigue

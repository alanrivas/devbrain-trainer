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

PASO 1.5: VERIFICAR SI LA FUNCIONALIDAD YA EXISTE (NUEVO)
  → Buscar en el código si la funcionalidad está implementada
  
  === FRONTEND ===
  IF Phase 4.2.1 (Login):
    → Buscar: `frontend/src/app/login/page.tsx`
    → Buscar: `frontend/src/components/LoginForm.tsx`
    
  IF Phase 4.2.2 (Register):
    → Buscar: `frontend/src/app/register/page.tsx`
    → Buscar: `frontend/src/components/RegisterForm.tsx`
    
  IF Phase 4.2.3 (Challenges List):
    → Buscar: `frontend/src/app/challenges/page.tsx`
    → Buscar: `frontend/src/components/ChallengeCard.tsx` o `ChallengesList.tsx`
    
  IF Phase 4.2.4 (Challenge Detail):
    → Buscar: `frontend/src/app/challenges/[id]/page.tsx`
    → Buscar: `frontend/src/components/ChallengeDetail.tsx`
    → Buscar: `frontend/src/components/AttemptForm.tsx`
    
  IF Phase 4.2.5 (User Stats):
    → Buscar: `frontend/src/app/stats/page.tsx`
    → Buscar: `frontend/src/components/StatsCard.tsx`
    
  IF Phase 4.2.6 (Badges):
    → Buscar: `frontend/src/app/badges/page.tsx`
    → Buscar: `frontend/src/components/BadgeCard.tsx`
    
  === BACKEND ===
  IF Phase 3.X:
    → Buscar en `src/DevBrain.Api/` por controlador o endpoint
    → Buscar en `src/DevBrain.Domain/` por entidad
    → Buscar en `src/DevBrain.Infrastructure/` por repositorio/servicio
  
  IF funcionalidad EXISTS:
    → Report: ✅ Phase X.X ya implementada
    → Cambiar context.md: marcar como ✅ (COMPLETADA)
    → Mark siguiente phase como ▶️ (EN PROGRESO)
    → Commit context.md
    → STOP y pasar a siguiente phase recursivamente
  
  IF funcionalidad NO exists:
    → Continuar a PASO 2

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

1. **Funcionalidad ya existe** (NUEVO)
   ```
   ✅ Phase 4.2.1 (Login Page) ya implementada
      Found: frontend/src/app/login/page.tsx
      Found: frontend/src/components/LoginForm.tsx
      Found: frontend/src/components/LoginForm.test.tsx
   
   → Marcando como ✅ COMPLETADA en context.md
   → Moviendo a siguiente: Phase 4.2.2 (Register Page)
   ```

2. **Todos los specs completados**
   ```
   ❌ Todos los specs de la fase están completos.
   → Próximas fases: Phase 4.3 (Integration Testing)
   ```

3. **Tests no pasan**
   ```
   ❌ Tests fallidos después de spec-implement
   ✅ Spec creada: specs/frontend/...
   ❌ Tests: 3/5 failing
   
   Detalles:
   [stack trace relevante]
   
   → Usuario debe revisar e intentar fijar
   ```

4. **Especificación ambigua**
   ```
   ❌ No se pudo inferir tipo de spec de context.md
   → Las fases están mal formateadas
   → Contacta a Alan para sincronizar
   ```

## Checklist de Verificación de Funcionalidad Existente

Antes de crear una spec, el agente debe verificar estos archivos **archivo por archivo**:

### Phase 4.2.1 — Login Page
```
Buscar (TODOS estos deben existir para marcar como COMPLETO):
  ✓ frontend/src/app/login/page.tsx
  ✓ frontend/src/components/LoginForm.tsx
  ✓ frontend/src/components/LoginForm.test.tsx
  ✓ frontend/src/app/login/page.test.tsx

Verificar contenido:
  → LoginForm.tsx debe tener: import useAuth, handleSubmit, email/password inputs, error handling
  → login/page.tsx debe tener: useRouter redirect si isAuthenticated, imports LoginForm
  → Tests deben cubrir: rendering, validation, submission, errors, accessibility

Si TODOS existen + tienen contenido → Marcar como ✅ COMPLETADA
```

### Phase 4.2.2 — Register Page
```
Buscar (TODOS estos deben existir para marcar como COMPLETO):
  ✓ frontend/src/app/register/page.tsx
  ✓ frontend/src/components/RegisterForm.tsx
  ✓ frontend/src/components/RegisterForm.test.tsx
  ✓ frontend/src/app/register/page.test.tsx

Verificar contenido:
  → RegisterForm.tsx debe tener: import useAuth, handleSubmit, email/password/displayName inputs
  → register/page.tsx debe tener: useRouter redirect si isAuthenticated, imports RegisterForm
  → Tests deben cubrir: rendering, validation, password matching, submission, errors

Si TODOS existen + tienen contenido → Marcar como ✅ COMPLETADA
```

### Phase 4.2.3 — Challenges List Page
```
Buscar (TODOS estos deben existir para marcar como COMPLETO):
  ✓ frontend/src/app/challenges/page.tsx
  ✓ frontend/src/components/ChallengeCard.tsx OR ChallengesList.tsx
  ✓ frontend/src/app/challenges/page.test.tsx

Verificar contenido:
  → challenges/page.tsx debe tener: GET /api/v1/challenges call, pagination, filtersby category/difficulty
  → ChallengeCard debe mostrar: title, category, difficulty, description preview, stats
  → Tests deben cubrir: rendering lista, pagination, filters, empty state

Si TODOS existen + tienen contenido → Marcar como ✅ COMPLETADA
```

### Phase 4.2.4 — Challenge Detail + Attempt Form
```
Buscar (TODOS estos deben existir para marcar como COMPLETO):
  ✓ frontend/src/app/challenges/[id]/page.tsx
  ✓ frontend/src/components/ChallengeDetail.tsx
  ✓ frontend/src/components/AttemptForm.tsx
  ✓ frontend/src/app/challenges/[id]/page.test.tsx

Verificar contenido:
  → [id]/page.tsx debe tener: useRouter params, useAuth check, GET /api/v1/challenges/:id
  → ChallengeDetail debe mostrar: title, description, category, difficulty, instructions
  → AttemptForm debe tener: text input, submit, POST /api/v1/challenges/:id/attempt, resultado (correcto/incorrecto)
  → Tests deben cubrir: rendering, form submission, error handling, success response

Si TODOS existen + tienen contenido → Marcar como ✅ COMPLETADA
```

### Phase 4.2.5 — User Stats Dashboard
```
Buscar (TODOS estos deben existir para marcar como COMPLETO):
  ✓ frontend/src/app/stats/page.tsx
  ✓ frontend/src/components/StatsCard.tsx
  ✓ frontend/src/app/stats/page.test.tsx

Verificar contenido:
  → stats/page.tsx debe tener: GET /api/v1/users/me/stats, DisplayStats
  → StatsCard debe mostrar: elo rating, total attempts, accuracy %, streaks
  → Tests deben cubrir: rendering stats, loading state, empty state

Si TODOS existen + tienen contenido → Marcar como ✅ COMPLETADA
```

### Phase 4.2.6 — Badges Page
```
Buscar (TODOS estos deben existir para marcar como COMPLETO):
  ✓ frontend/src/app/badges/page.tsx
  ✓ frontend/src/components/BadgeCard.tsx
  ✓ frontend/src/app/badges/page.test.tsx

Verificar contenido:
  → badges/page.tsx debe tener: GET /api/v1/users/me/badges, DisplayBadges
  → BadgeCard debe mostrar: badge name, icon/image, description, unlockedAt date
  → Badge gris si no desbloqueado
  → Tests deben cubrir: rendering badges, empty state, locked/unlocked states

Si TODOS existen + tienen contenido → Marcar como ✅ COMPLETADA
```

### Procedimiento de Verificación (Paso 1.5)
```bash
# Para cada archivo esperado:
if [ -f "path/to/file.tsx" ]; then
  if grep -q "importantKeyword" "path/to/file.tsx"; then
    echo "✅ $file exists and has expected content"
  else
    echo "❌ $file exists but missing key implementation"
    echo "   → Create spec para refactorizar/completar"
  fi
else
  echo "❌ $file NOT FOUND"
  echo "   → Create spec para implementar"
fi
```

**IMPORTANTE**: 
- Si **todos** los archivos existen + tienen contenido → Marcar phase como ✅
- Si **algunos** faltan o incompletos → Crear spec para completar
- Reportar al usuario qué archivos se encontraron y cuáles faltan

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

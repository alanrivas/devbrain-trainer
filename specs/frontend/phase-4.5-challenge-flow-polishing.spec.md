# Spec: Phase 4.5 — Challenge Flow Polishing

**Tipo**: Frontend Feature (UX polish + navegación + persistencia)
**Ubicación**:
- `frontend/src/components/AttemptForm.tsx`
- `frontend/src/components/AttemptForm.test.tsx`
- `frontend/src/app/challenges/[id]/page.tsx`
- `frontend/src/app/challenges/[id]/page.test.tsx`
- `frontend/src/app/challenges/page.tsx`
**Versión**: 1.0

---

## Qué es

Phase 4.5 pule el flujo completo de resolución de challenges para hacerlo fluido, sin fricciones y más motivador. Se trabajan tres ejes independientes:

1. **Draft persistence**: el borrador del usuario sobrevive navegaciones accidentales usando `localStorage`
2. **Navegación entre challenges**: el usuario puede pasar al siguiente challenge sin volver al listado
3. **Feedback de resultado enriquecido**: el resumen post-intento muestra rendimiento de tiempo y contexto más accionable

---

## Eje 1 — Draft Persistence

### Comportamiento

- Cuando el usuario escribe en el textarea de `AttemptForm`, el texto se guarda en `localStorage` con clave `draft-attempt-{challengeId}`
- Al montar `AttemptForm` con un `challengeId`, si existe un draft guardado para ese id, se restaura automáticamente en el textarea
- Tras un submit exitoso (resultado recibido del backend), el draft se elimina de `localStorage`
- Al hacer reset del formulario (botón "Try again"), el draft se elimina de `localStorage`
- El draft **no** se usa para alterar el payload enviado al backend; solo pre-rellena el textarea

### Invariantes

1. El draft nunca persiste después de un intento completado (éxito o fallo con resultado visible)
2. Si el draft está vacío o es solo whitespace, se ignora al restaurar
3. La clave de `localStorage` siempre incluye el `challengeId` para evitar mezclar drafts entre challenges

### Escenarios de test

| Escenario | Resultado |
|-----------|-----------|
| Usuario escribe texto → guarda en localStorage | OK — `localStorage.setItem` llamado con clave correcta |
| Montar form con draft existente | OK — textarea pre-rellenado con el draft |
| Montar form sin draft existente | OK — textarea vacío |
| Submit exitoso → draft eliminado | OK — `localStorage.removeItem` llamado |
| Reset del formulario → draft eliminado | OK — `localStorage.removeItem` llamado |
| Draft solo whitespace → ignorado al montar | OK — textarea vacío |

---

## Eje 2 — Navegación entre challenges

### Comportamiento

- La página de challenges (`/challenges`) almacena la lista de IDs ordenados en `sessionStorage` (clave `challenge-list-ids`) al cargar los challenges
- La página de detalle (`/challenges/[id]`) lee `challenge-list-ids` desde `sessionStorage` para determinar la posición del challenge actual en la lista
- Si el challenge tiene un siguiente en la lista, aparece un botón **"Next challenge →"** en la zona de acciones post-attempt
- Si el challenge tiene un anterior en la lista, aparece un botón **"← Previous challenge"**
- Los botones de navegación aparecen **siempre** (no solo tras un intento), como una barra de navegación discreta en la parte superior del detalle
- Si no hay lista en `sessionStorage` (acceso directo por URL), la barra de navegación no se muestra — solo el breadcrumb "← Back to challenges"

### Props y datos necesarios

La página de detalle no recibe props de navegación; lee directamente de `sessionStorage`.

```typescript
// Formato guardado en sessionStorage
// clave: 'challenge-list-ids'
// valor: JSON.stringify(string[]) — array de IDs en el orden del listado
```

### Invariantes

1. La navegación entre challenges nunca altera el estado del intento en curso
2. Si el usuario tiene un draft en el challenge actual, navegar no lo borra (el draft de destino es independiente)
3. Los botones de navegación no se muestran en medio de un submit (`loading = true`)

### Escenarios de test

| Escenario | Resultado |
|-----------|-----------|
| sessionStorage tiene lista con challenge actual | OK — botones prev/next visibles según posición |
| Challenge es el primero de la lista | OK — solo "Next challenge →" visible |
| Challenge es el último de la lista | OK — solo "← Previous challenge" visible |
| Challenge es el único de la lista | OK — ningún botón de navegación visible |
| sessionStorage vacío / sin lista | OK — barra de navegación oculta |
| Click "Next challenge →" | OK — navega a `/challenges/{nextId}` |
| Click "← Previous challenge" | OK — navega a `/challenges/{prevId}` |
| Submit en curso → botones deshabilitados | OK — no se puede navegar durante submit |

---

## Eje 3 — Feedback de resultado enriquecido

### Comportamiento

El bloque de resultado (dentro de `AttemptForm`) se expande para mostrar:

1. **Rendimiento de tiempo**: comparar `elapsedSeconds` con `timeLimitSecs`
   - Si `elapsedSeconds <= timeLimitSecs * 0.5`: "Fast answer" (verde)
   - Si `elapsedSeconds <= timeLimitSecs * 0.8`: "In time" (azul)
   - Si `elapsedSeconds > timeLimitSecs * 0.8`: "Cutting it close" (ámbar)

2. **Badge visual de resultado**: la sección de resultado tiene un indicador más prominente
   - Correcto: encabezado en verde con texto "Correct!"
   - Incorrecto: encabezado en ámbar con texto "Not quite"

3. **Respuesta correcta siempre visible en intentos incorrectos**: el bloque de `correctAnswer` no queda enterrado; sube a la segunda línea del resultado

### Props requeridas nuevas

`AttemptForm` recibe `timeLimitSecs` (ya existe). No se necesitan nuevas props para este eje.

### Invariantes

1. El performance badge siempre se calcula con `result.elapsedSeconds` (el valor confirmado por el backend), no con el timer visual
2. Si el backend no devuelve `correctAnswer`, el bloque de respuesta correcta no se renderiza (sin cambio respecto a Phase 4.4)
3. El feedback enriquecido no reemplaza, sino que complementa, los botones "Try again" y "Back to challenges"

### Escenarios de test

| Escenario | Resultado |
|-----------|-----------|
| `elapsedSeconds` ≤ 50% del límite | OK — badge "Fast answer" visible |
| `elapsedSeconds` entre 50%-80% del límite | OK — badge "In time" visible |
| `elapsedSeconds` > 80% del límite | OK — badge "Cutting it close" visible |
| Resultado correcto | OK — encabezado "Correct!" visible |
| Resultado incorrecto | OK — encabezado "Not quite" visible |
| Incorrecto con `correctAnswer` del backend | OK — respuesta correcta en posición prominente |
| Incorrecto sin `correctAnswer` | OK — bloque de respuesta correcta ausente |

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `frontend/src/components/AttemptForm.tsx` | Draft persistence (localStorage) + feedback enriquecido |
| `frontend/src/components/AttemptForm.test.tsx` | Tests nuevos para draft y feedback |
| `frontend/src/app/challenges/page.tsx` | Guardar lista de IDs en sessionStorage al cargar |
| `frontend/src/app/challenges/[id]/page.tsx` | Leer lista de sessionStorage + renderizar barra prev/next |
| `frontend/src/app/challenges/[id]/page.test.tsx` | Tests nuevos para navegación prev/next |

---

## Escenarios de test — resumen por archivo

### AttemptForm.test.tsx — tests nuevos (~10)

**Draft persistence (6)**
- Escribe en textarea → guarda en localStorage
- Monta con draft existente → textarea pre-rellenado
- Monta sin draft → textarea vacío
- Submit exitoso → localStorage limpio
- Reset del form → localStorage limpio
- Draft whitespace → ignorado al montar

**Feedback enriquecido (4)**
- Badge "Fast answer" cuando elapsedSeconds ≤ 50% timeLimitSecs
- Badge "In time" cuando elapsedSeconds entre 50%-80%
- Badge "Cutting it close" cuando elapsedSeconds > 80%
- Respuesta correcta prominente en resultado incorrecto con correctAnswer

### challenges/[id]/page.test.tsx — tests nuevos (~7)

**Navegación prev/next (5)**
- sessionStorage con lista → botones visibles
- Primer challenge → solo "Next"
- Último challenge → solo "Previous"
- Click "Next" → navega correctamente
- Click "Previous" → navega correctamente

**Sin lista en sessionStorage (2)**
- Acceso directo → barra de navegación oculta
- Solo breadcrumb "Back to challenges" visible

---

## Qué NO es esta fase

- No modifica el contrato HTTP del backend
- No agrega XP display (no está en el response del endpoint `/attempt`)
- No agrega animaciones CSS complejas (confetti, etc.)
- No persiste el borrador en el backend
- No agrega paginación de challenges dentro del detail
- No cambia el sistema de autenticación

---

## Criterios de éxito

- El borrador del usuario sobrevive al recargar la página o volver al detalle
- El usuario puede navegar al siguiente challenge sin pasar por el listado
- El resultado del intento muestra performance de tiempo de forma clara
- Los tests previos (Phase 4.2–4.4, 94/94) siguen en verde
- Los nuevos tests (~17) están en verde

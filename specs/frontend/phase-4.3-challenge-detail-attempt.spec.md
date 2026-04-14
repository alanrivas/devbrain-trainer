# Spec: Phase 4.3 — Challenge Detail Page + Attempt Form

**Tipo**: Frontend Feature (Next.js Page + React Component)
**Ubicación**:
- `frontend/src/app/challenges/[id]/page.tsx`
- `frontend/src/components/AttemptForm.tsx`
- `frontend/src/app/challenges/page.tsx` (navegacion a detalle)
**Version**: 1.0

---

## Que es

Phase 4.3 implementa el flujo completo de intento de challenge desde frontend:
- El usuario abre el detalle de un challenge (`/challenges/[id]`)
- Visualiza enunciado, metadata y tiempo limite
- Envia su respuesta con un formulario dedicado
- Recibe feedback inmediato (correct/incorrect) y contexto del intento

Esta fase conecta la lista de challenges (Phase 4.2.3) con el endpoint de intento ya disponible en backend.

---

## Contratos HTTP utilizados

### 1) GET `/api/v1/challenges/{id}`

**Objetivo**: obtener detalle del challenge seleccionado.

**Response 200**:
```json
{
  "id": "uuid",
  "title": "string",
  "description": "string",
  "category": "Sql|CodeLogic|Architecture|DevOps|WorkingMemory",
  "difficulty": "Easy|Medium|Hard",
  "timeLimitSecs": 60
}
```

**Errores esperados**:
- `404` challenge inexistente
- `400` id invalido
- `401` token invalido/expirado (si aplica por configuracion actual del backend)

### 2) POST `/api/v1/challenges/{id}/attempt`

**Objetivo**: enviar intento del usuario autenticado.

**Request**:
```json
{
  "userAnswer": "string",
  "elapsedSeconds": 45
}
```

**Response 201**:
```json
{
  "attemptId": "uuid",
  "challengeId": "uuid",
  "userId": "uuid",
  "userAnswer": "string",
  "isCorrect": true,
  "correctAnswer": "string",
  "elapsedSeconds": 45,
  "challengeTitle": "string",
  "occurredAt": "2026-04-14T12:00:00Z"
}
```

**Errores esperados**:
- `400` validacion (answer vacia o elapsedSeconds invalido)
- `401` no autenticado
- `404` challenge inexistente

---

## Comportamientos funcionales

### 1. Navegacion desde lista de challenges

- En `challenges/page.tsx`, callback `onAttempt(challengeId)` debe navegar a `/challenges/{challengeId}`
- El flujo deja de usar `console.log` como placeholder

### 2. Rendering inicial de Challenge Detail Page

- Ruta: `/challenges/[id]`
- Si no hay token: redirigir a `/login`
- Mientras carga: mostrar estado de loading
- Si la carga falla: mostrar estado de error con mensaje claro
- Si `404`: mostrar estado "Challenge not found" con opcion de volver al listado

### 3. Contenido del detalle

- Titulo y descripcion completa
- Badges de categoria y dificultad
- Tiempo limite (`timeLimitSecs`) visible
- Breadcrumb o boton "Back to challenges"

### 4. AttemptForm (componente reutilizable)

Props propuestas:
```typescript
interface AttemptFormProps {
  challengeId: string;
  timeLimitSecs: number;
  onSuccess?: (result: AttemptResult) => void;
}
```

Estado interno minimo:
- `userAnswer`
- `loading`
- `error`
- `attemptResult` (opcional si se decide manejarlo dentro del form)

Comportamiento:
- Valida `userAnswer` no vacia ni whitespace
- Calcula `elapsedSeconds` desde que la pagina/form se habilita para responder
- En submit, llama POST `/challenges/{id}/attempt`
- Deshabilita boton durante submit
- Muestra resultado:
  - Correcto: mensaje de exito
  - Incorrecto: mensaje de intento fallido + respuesta correcta (si backend la provee)

### 5. Manejo de errores

| Escenario | Mensaje esperado |
|---|---|
| `400` body invalido | "Invalid answer. Please review your input." |
| `401` no autenticado | limpiar auth + redirect `/login` |
| `404` challenge not found | "Challenge not found." |
| `500`/network | "Server error. Try again." |

### 6. Accesibilidad

- `label` asociado a textarea/input de respuesta
- Mensajes de error con `role="alert"`
- Boton submit con estado `disabled` y texto de loading
- Flujo usable solo con teclado (tab + enter)

---

## Invariantes

1. Nunca se envia un intento con `userAnswer` vacia.
2. `elapsedSeconds` siempre es mayor o igual a 0.
3. Si usuario no autenticado, nunca se permite permanencia en la pagina de intento.
4. El `challengeId` utilizado en POST siempre coincide con el id de la ruta.
5. El frontend no calcula `isCorrect`; solo renderiza el resultado del backend.

---

## Que NO incluye esta fase

- No incluye editor avanzado con syntax highlighting
- No incluye reintentos automáticos con cooldown
- No incluye leaderboard en vivo
- No incluye actualizacion optimista de stats globales

---

## Archivos objetivo

1. `frontend/src/app/challenges/[id]/page.tsx` (nuevo)
2. `frontend/src/components/AttemptForm.tsx` (nuevo)
3. `frontend/src/components/AttemptForm.test.tsx` (nuevo)
4. `frontend/src/app/challenges/[id]/page.test.tsx` (nuevo)
5. `frontend/src/app/challenges/page.tsx` (update navegacion)

---

## Escenarios de test esperados

### AttemptForm tests (16 objetivo)

**Rendering (4)**
- Renderiza textarea/input de respuesta
- Renderiza boton de submit
- Renderiza hint de tiempo limite
- Renderiza labels accesibles

**Validation (3)**
- Rechaza respuesta vacia
- Rechaza respuesta solo whitespace
- No llama API cuando la validacion falla

**Submission (5)**
- Envia payload correcto (`userAnswer`, `elapsedSeconds`)
- Deshabilita submit durante loading
- Muestra exito cuando `isCorrect = true`
- Muestra fallo cuando `isCorrect = false`
- Ejecuta callback `onSuccess` cuando corresponde

**Errors (4)**
- Maneja 400 con mensaje de validacion
- Maneja 401 con flujo de auth
- Maneja 404 challenge
- Maneja 500/network error

### Challenge detail page tests (12 objetivo)

**Routing/Auth (3)**
- Redirige a login sin token
- Permanece en pagina con token valido
- Navega de vuelta a listado

**Data fetching (4)**
- Fetch exitoso y renderiza challenge
- Loading state visible durante fetch
- Error state en fallo de red
- Not-found state para 404

**Integration with AttemptForm (3)**
- Renderiza AttemptForm con props correctas
- Usa `challenge.id` de route params
- Muestra feedback tras intento

**Accessibility/UI (2)**
- Estructura semantica principal (main + headings)
- Mensajes de error accesibles (`role=alert`)

**Total objetivo Phase 4.3**: 28 tests

---

## Criterios de exito

- `AttemptForm` implementado y cubierto por tests
- Pagina `/challenges/[id]` funcional con estados loading/error/success
- Navegacion desde `ChallengeCard` conectada al detalle
- Tests nuevos en verde
- Sin regresiones en los 69 tests frontend existentes

# Spec: Phase 4.12 — Drag & Drop (Ordering) Exercise UI

**Tipo**: Frontend Feature (nuevo componente + página de detalle actualizada)  
**Ubicación**:
- `frontend/src/components/OrderingForm.tsx` (nuevo)
- `frontend/src/components/OrderingForm.test.tsx` (nuevo)
- `frontend/src/app/challenges/[id]/page.tsx` (update — renderizado condicional)
- `frontend/src/app/challenges/[id]/page.test.tsx` (update)
**Versión**: 1.0

---

## Qué es

Phase 4.12 introduce `OrderingForm`, un componente de respuesta con drag & drop para los challenges de tipo `Ordering`. El usuario recibe una lista de ítems desordenados y los arrastra para reordenarlos. Al enviar, el orden elegido se compara con el correcto en el backend.

Es ideal para challenges de arquitectura, pipelines CI/CD, pasos de algoritmos, o fases de un ciclo de desarrollo.

---

## Prerrequisito

El backend debe devolver `items` en `GET /challenges/{id}` para challenges tipo `Ordering` (spec `challenge-ordering.spec.md`).

---

## Dependencia: `@dnd-kit/core` + `@dnd-kit/sortable`

Librería de drag & drop para React. Se instala con `npm install @dnd-kit/core @dnd-kit/sortable`.
- `@dnd-kit/core` — gestión de eventos drag
- `@dnd-kit/sortable` — preset para listas reordenables

---

## Contrato HTTP

`GET /api/v1/challenges/{id}` devuelve (para tipo Ordering):
```typescript
{
  "type": "Ordering",
  "items": ["step3", "step1", "step4", "step2"]  // orden original del backend
}
```

`POST /api/v1/challenges/{id}/attempt` recibe:
```typescript
{ "userAnswer": "step1|step2|step3|step4", "elapsedSeconds": number }
```

`userAnswer` es el orden elegido por el usuario, pipe-separated, en el mismo formato que `CorrectAnswer` en el backend.

---

## Comportamiento: `OrderingForm`

### Props

```typescript
interface OrderingFormProps {
  challengeId: string;
  timeLimitSecs: number;
  items: string[];
  onSuccess?: (result: AttemptResult) => void;
}
```

### Estado interno

- `orderedItems: string[]` — lista de ítems en el orden actual del usuario. Inicializada con `items` shuffleados aleatoriamente al montar el componente.
- `loading: boolean`
- `error: string`
- `result: AttemptResult | null`

### Rendering inicial

- Muestra el timer visual (igual que los otros formularios)
- Lista de ítems como tarjetas arrastrables, en orden aleatorio (shuffled al montar)
- Cada ítem tiene un ícono de "grip" (⠿) a la izquierda indicando que es arrastrable
- Botón "Submit Order" siempre habilitado (el usuario siempre tiene algún orden)
- Instrucción contextual: `"Drag items into the correct order"`

### Interacción drag & drop

- El usuario arrastra cualquier ítem y lo suelta en otra posición
- La lista se reordena en tiempo real durante el drag (sortable)
- Al soltar, `orderedItems` se actualiza con el nuevo orden
- El drag es táctil y de escritorio (soporte de `@dnd-kit`)
- Mientras `loading = true`: drag deshabilitado, ítems bloqueados

### Submit

1. Construye `userAnswer = orderedItems.join("|")`
2. Llama `POST /challenges/{id}/attempt` con `{ userAnswer, elapsedSeconds }`
3. `elapsedSeconds` calculado desde que el componente se montó

### Tarjeta de resultado (idéntica a otros formularios)

- "Correct!" (verde) / "Not quite" (ámbar)
- Si incorrecto: `"Correct order: {correctAnswer}"` — el backend devuelve `correctAnswer` en el `AttemptResponseDto`
- ELO, streak, badges si presentes
- Botones: "Try again" (re-shufflea los ítems y limpia el resultado) y "Back to challenges"

### Accesibilidad

- Los ítems tienen `aria-grabbed` y `aria-dropeffect` donde aplique
- Instrucciones de teclado visibles: `"Use arrow keys to reorder when focused"`
- Con teclado: al enfocar un ítem y presionar Space se activa el modo drag; ArrowUp/ArrowDown mueven el ítem; Space/Enter confirman la posición; Escape cancela
- Mensajes de error con `role="alert"`

---

## Comportamiento: página `/challenges/[id]`

### Renderizado condicional actualizado

```
'MultipleChoice' → <MultipleChoiceForm />
'CodeRunner'     → <CodeRunnerForm />
'Ordering'       → <OrderingForm />    ← nuevo
'OpenText'       → <AttemptForm />
```

---

## Invariantes

1. Los ítems mostrados siempre son los mismos que devuelve el backend — no se agregan ni eliminan.
2. El orden inicial es siempre aleatorio (shuffled al montar) — nunca el orden original del backend.
3. "Try again" re-shufflea el orden — no restaura el orden original del backend.
4. `userAnswer` siempre contiene exactamente los mismos ítems que `items`, solo reordenados.
5. El drag está deshabilitado durante el submit.

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `frontend/src/components/OrderingForm.tsx` | Nuevo componente |
| `frontend/src/components/OrderingForm.test.tsx` | Tests nuevos |
| `frontend/src/app/challenges/[id]/page.tsx` | Caso `Ordering` en renderizado condicional |
| `frontend/src/app/challenges/[id]/page.test.tsx` | Test para tipo Ordering |

---

## Escenarios de test — `OrderingForm.test.tsx`

> `@dnd-kit` se mockea en tests — no se simulan eventos de drag reales. Los tests verifican el comportamiento al reordenar el estado, no la mecánica del drag.

### Rendering (4)

| Escenario | Resultado |
|-----------|-----------|
| Renderiza un ítem arrastrable por cada elemento de `items` | N ítems en el DOM |
| Muestra el texto de cada ítem | Textos visibles |
| Muestra instrucción "Drag items into the correct order" | Texto presente |
| Botón "Submit Order" visible y habilitado desde el inicio | Botón presente, sin `disabled` |

### Interacción y estado (3)

| Escenario | Resultado |
|-----------|-----------|
| Al reordenar ítems (simulado via setState), el orden se actualiza | Nuevo orden visible |
| Timer visible al montar | Elemento de timer presente |
| Ítems deshabilitados durante loading | Drag bloqueado (`aria-disabled` o clase CSS) |

### Submit (4)

| Escenario | Resultado |
|-----------|-----------|
| Submit llama API con `userAnswer` = ítems en orden actual, pipe-separated | Payload correcto |
| Submit deshabilita botón durante loading | `disabled` activo |
| Resultado `isCorrect: true` muestra "Correct!" | Texto verde |
| Resultado `isCorrect: false` muestra "Not quite" + orden correcto | Ámbar + correctAnswer visible |

### Reset y resultado (3)

| Escenario | Resultado |
|-----------|-----------|
| "Try again" limpia el resultado | `result` null |
| Resultado con `newEloRating` muestra ELO | "ELO: {n}" visible |
| Resultado con `newBadges` muestra badges | Badges visibles |

### Errores de red (3)

| Escenario | Resultado |
|-----------|-----------|
| Error 401 hace clearAuth + redirect | `router.push('/login')` |
| Error 404 muestra mensaje | "Challenge not found." |
| Error 500 muestra mensaje | "Server error. Try again." |

### Accesibilidad (2)

| Escenario | Resultado |
|-----------|-----------|
| Mensajes de error tienen `role="alert"` | Atributo presente |
| Instrucciones de teclado visibles | Texto de instrucción en DOM |

**Total estimado `OrderingForm.test.tsx`**: ~19 tests

### `page.test.tsx` — tests nuevos (2)

| Escenario | Resultado |
|-----------|-----------|
| Challenge con `type: 'Ordering'` renderiza `OrderingForm` | Componente presente |
| `OrderingForm` recibe `items` correctos del challenge | Props pasadas |

---

## Qué NO incluye esta fase

- No soporta ítems con imágenes o HTML enriquecido — solo texto plano
- No muestra feedback de "estabas N posiciones del orden correcto"
- No persiste el orden del usuario entre navegaciones
- No anima el resultado correcto mostrando los ítems reordenándose automáticamente
- No extrae el timer ni la tarjeta de resultado a componentes compartidos

---

## Criterios de éxito

- `OrderingForm` implementado con drag & drop funcional
- Página de detalle renderiza el formulario correcto para `Ordering`
- ~21 tests nuevos en verde
- Tests frontend existentes sin regresiones

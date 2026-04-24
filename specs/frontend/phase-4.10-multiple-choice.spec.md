# Spec: Phase 4.10 — Multiple Choice Exercise UI

**Tipo**: Frontend Feature (nuevo componente + actualización de página de detalle)  
**Ubicación**:
- `frontend/src/components/MultipleChoiceForm.tsx` (nuevo)
- `frontend/src/components/MultipleChoiceForm.test.tsx` (nuevo)
- `frontend/src/app/challenges/[id]/page.tsx` (update — renderizado condicional)
- `frontend/src/app/challenges/[id]/page.test.tsx` (update — tests para ambos tipos)
**Versión**: 1.0

---

## Qué es

Phase 4.10 introduce `MultipleChoiceForm`, un componente de respuesta con botones de opción (radio buttons) para los challenges de tipo `MultipleChoice`. La página de detalle del challenge elige automáticamente qué formulario renderizar según el campo `type` que ahora devuelve el backend.

Para challenges `OpenText`, el comportamiento actual (`AttemptForm` con textarea) no cambia.

---

## Prerrequisito

Esta fase requiere que el backend ya devuelva los campos `type` y `options` en `GET /challenges/{id}`. Eso lo implementa la spec `specs/domain/challenge-types.spec.md`.

---

## Contrato HTTP utilizado

`GET /api/v1/challenges/{id}` ya devuelve (tras la spec de dominio):

```typescript
{
  "id": "uuid",
  "title": "string",
  "description": "string",
  "category": "string",
  "difficulty": "string",
  "timeLimitSecs": 60,
  "type": "OpenText" | "MultipleChoice",  // ← nuevo
  "options": string[]                      // ← nuevo, vacío para OpenText
}
```

`POST /api/v1/challenges/{id}/attempt` no cambia — sigue recibiendo `userAnswer: string`.

---

## Comportamiento: `MultipleChoiceForm`

### Props

```typescript
interface MultipleChoiceFormProps {
  challengeId: string;
  timeLimitSecs: number;
  options: string[];
  onSuccess?: (result: AttemptResult) => void;
}
```

### Estados internos

- `selectedOption: string | null` — opción seleccionada por el usuario (null = ninguna)
- `loading: boolean` — submit en curso
- `error: string` — mensaje de error de red o validación
- `result: AttemptResult | null` — resultado recibido del backend

### Rendering inicial

- Muestra el timer visual con barra de progreso (igual al de `AttemptForm`)
- Muestra cada opción como un radio button con su label
- El botón "Submit" está **deshabilitado** hasta que se seleccione una opción
- Hint de teclado: `"Enter to submit"` (sin Ctrl — Enter solo es suficiente para radios)

### Interacción con opciones

- Al hacer click en una opción: queda visualmente marcada, el botón Submit se habilita
- Solo una opción puede estar seleccionada a la vez
- Mientras `loading = true`, los radio buttons quedan deshabilitados

### Submit

1. Validación: si `selectedOption` es null → muestra error "Please select an option"
2. Llama `POST /challenges/{challengeId}/attempt` con `{ userAnswer: selectedOption, elapsedSeconds }`
3. `elapsedSeconds` se calcula desde que el componente se montó (igual que `AttemptForm`)
4. Durante la llamada: loading = true, radios + botón deshabilitados

### Tarjeta de resultado (idéntica a `AttemptForm`)

Tras recibir respuesta del backend, muestra:
- "Correct!" (verde) o "Not quite" (ámbar)
- Si incorrecto: `"Correct answer: {correctAnswer}"`
- Tiempo transcurrido + badge de performance (Fast / In time / Cutting it close)
- ELO si `result.newEloRating` está presente
- Streak si `result.newStreak` está presente
- Badges ganados si `result.newBadges` tiene elementos
- Botones: "Try again" (resetea el formulario) y "Back to challenges" (navega a `/challenges`)
- Atajo `R` para reiniciar cuando hay resultado visible

### Manejo de errores de red

| Código | Mensaje mostrado |
|--------|-----------------|
| 400 | "Invalid answer. Please review your input." |
| 401 | clearAuth() + redirect a `/login` |
| 404 | "Challenge not found." |
| 500 / red | "Server error. Try again." |

### Accesibilidad

- Las opciones están agrupadas en un `<fieldset>` con `<legend>Choose your answer</legend>`
- Cada radio button tiene su `<label>` asociado por `htmlFor` / `id`
- Mensajes de error con `role="alert"`
- El botón Submit con `disabled` semántico cuando no hay opción seleccionada
- Navegable completamente con teclado (Tab para moverse entre radios, Enter/Space para seleccionar)

---

## Comportamiento: página `/challenges/[id]`

### Cambios en la interfaz `Challenge` local

Agregar los campos nuevos del backend al tipo local de la página:

```typescript
interface Challenge {
  // campos existentes...
  type: 'OpenText' | 'MultipleChoice';
  options: string[];
}
```

### Renderizado condicional del formulario

```
si challenge.type === 'MultipleChoice'
  → <MultipleChoiceForm challengeId options timeLimitSecs onSuccess />
si challenge.type === 'OpenText' (o undefined por compatibilidad)
  → <AttemptForm challengeId timeLimitSecs onSuccess />
```

El comportamiento de la página (fetch, loading/error state, navegación prev/next, breadcrumb) no cambia.

---

## Invariantes

1. `MultipleChoiceForm` nunca envía un intento si `selectedOption` es null.
2. El `userAnswer` enviado siempre es el texto exacto de una de las opciones renderizadas.
3. Los radio buttons están deshabilitados durante el submit y cuando `result` ya fue recibido.
4. Si `options` está vacía, el componente no se renderiza (la página detail nunca lo monta sin opciones).
5. El timer se inicia al montar el componente, no al seleccionar una opción.
6. `AttemptForm` no se modifica — no recibe props de opciones, comportamiento sin cambios.

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `frontend/src/components/MultipleChoiceForm.tsx` | Nuevo componente |
| `frontend/src/components/MultipleChoiceForm.test.tsx` | Tests nuevos |
| `frontend/src/app/challenges/[id]/page.tsx` | Añadir campos `type`/`options` a interfaz + renderizado condicional |
| `frontend/src/app/challenges/[id]/page.test.tsx` | Tests para ambos tipos de challenge |

---

## Qué NO incluye esta fase

- No modifica `AttemptForm.tsx` ni sus tests
- No modifica `ChallengeCard.tsx` ni la lista de challenges
- No agrega animaciones al seleccionar una opción
- No extrae el timer ni la tarjeta de resultado a componentes compartidos (eso es una refactorización futura)
- No soporta challenges con más de 4 opciones

---

## Escenarios de test — `MultipleChoiceForm.test.tsx`

### Rendering (5)

| Escenario | Resultado |
|-----------|-----------|
| Renderiza un radio button por cada opción provista | N radio buttons visibles |
| Muestra el label de cada opción correctamente | Texto de cada opción en la UI |
| Botón Submit deshabilitado al montar (sin opción seleccionada) | `disabled` en el botón |
| Muestra timer visible al montar | Elemento de timer presente |
| Muestra hint de teclado "Enter to submit" | Texto presente |

### Selección de opción (3)

| Escenario | Resultado |
|-----------|-----------|
| Hacer click en una opción la marca como seleccionada | Radio queda checked |
| Tras seleccionar, el botón Submit se habilita | `disabled` removido del botón |
| Seleccionar segunda opción deselecciona la primera | Solo un radio checked |

### Submit (5)

| Escenario | Resultado |
|-----------|-----------|
| Submit sin opción seleccionada | Muestra "Please select an option", no llama API |
| Submit con opción seleccionada llama POST con `userAnswer` = texto de la opción | API llamada con payload correcto |
| Submit deshabilita radios y botón durante loading | Inputs con `disabled` |
| Resultado `isCorrect: true` muestra "Correct!" | Texto verde visible |
| Resultado `isCorrect: false` muestra "Not quite" + respuesta correcta | Texto ámbar + correctAnswer |

### Resultado y reset (4)

| Escenario | Resultado |
|-----------|-----------|
| Resultado con `newEloRating: 1250` muestra "ELO: 1250" | Texto presente |
| Resultado con `newStreak: 3` muestra "Streak: 3 days" | Texto presente |
| Resultado con `newBadges: ['FirstAttempt']` muestra "New badge: FirstAttempt" | Texto presente |
| Click en "Try again" resetea la selección y limpia el resultado | Radios sin seleccionar, result null |

### Manejo de errores (4)

| Escenario | Resultado |
|-----------|-----------|
| Error 400 muestra mensaje de validación | "Invalid answer. Please review your input." |
| Error 401 limpia auth y redirige a login | `clearAuth` + `router.push('/login')` |
| Error 404 muestra "Challenge not found." | Mensaje visible |
| Error 500 muestra "Server error. Try again." | Mensaje visible |

### Accesibilidad (3)

| Escenario | Resultado |
|-----------|-----------|
| Opciones están dentro de un `<fieldset>` | Elemento `fieldset` presente |
| `<legend>` describe el grupo de opciones | `<legend>` con texto presente |
| Mensajes de error tienen `role="alert"` | Atributo presente |

**Total estimado `MultipleChoiceForm.test.tsx`**: ~24 tests

---

## Escenarios de test — `challenges/[id]/page.test.tsx` (tests nuevos)

| Escenario | Resultado |
|-----------|-----------|
| Challenge con `type: 'MultipleChoice'` renderiza `MultipleChoiceForm` | Componente presente en DOM |
| Challenge con `type: 'OpenText'` renderiza `AttemptForm` (sin regresiones) | Componente correcto presente |
| `MultipleChoiceForm` recibe las `options` del challenge | Props pasadas correctamente |

**Total estimado nuevos en `page.test.tsx`**: ~3 tests

---

## Criterios de éxito

- `MultipleChoiceForm` implementado con las 24+ specs en verde
- Página de detalle selecciona el formulario correcto según `challenge.type`
- 145 tests frontend existentes siguen en verde (sin regresiones)
- Grand total estimado: **399 + 27 = 426 tests en verde**

# Spec: Phase 4.9 — Navegación por teclado en challenges

**Tipo**: Frontend Feature  
**Ubicación**:
- `frontend/src/components/AttemptForm.tsx` — Ctrl+Enter para submit, R para retry
- `frontend/src/app/challenges/[id]/page.tsx` — shortcuts de navegación (ArrowLeft, ArrowRight, Escape)
- `frontend/src/components/AttemptForm.test.tsx` — tests de keyboard en el formulario
- `frontend/src/app/challenges/[id]/page.test.tsx` — tests de keyboard en la página  
**Versión**: 1.0

---

## Qué es

Phase 4.9 agrega atajos de teclado en el challenge flow para que los usuarios puedan interactuar sin necesidad del mouse.

| Atajo | Contexto | Acción |
|-------|----------|--------|
| `Ctrl+Enter` | Textarea enfocada | Envía el formulario de respuesta |
| `ArrowLeft` | Textarea NO enfocada | Navega al challenge anterior |
| `ArrowRight` | Textarea NO enfocada | Navega al challenge siguiente |
| `Escape` | Textarea NO enfocada | Vuelve a `/challenges` |
| `r` / `R` | Resultado visible | Reinicia el formulario (Try again) |

---

## Comportamiento detallado

### Ctrl+Enter en textarea

- Cuando el usuario está escribiendo en el textarea y presiona `Ctrl+Enter` (o `Cmd+Enter`):
  - Se ejecuta el submit del formulario (equivalente a clickear "Submit Attempt")
  - Si el textarea está vacío, muestra el error de validación igual que el click normal
  - Si el formulario está en loading, no hace nada (el submit ya tiene guardia)
- No interfiere con el comportamiento normal de `Enter` (insertar salto de línea)
- El hint `"Ctrl+Enter to submit"` se muestra como texto visible pequeño bajo el textarea

### Navegación prev/next (ArrowLeft / ArrowRight)

- El listener se registra en `keydown` a nivel de `document` en la página `/challenges/[id]`
- Solo actúa cuando el elemento activo **no** es un textarea o input (`document.activeElement?.tagName` no es `TEXTAREA` ni `INPUT`)
- `ArrowLeft` → `router.push('/challenges/' + prevId)` solo si `prevId !== null`
- `ArrowRight` → `router.push('/challenges/' + nextId)` solo si `nextId !== null`
- Si `prevId` o `nextId` es `null`, el atajo se ignora silenciosamente

### Escape → volver a challenges

- Listener en `keydown` a nivel de `document` en la página `/challenges/[id]`
- Solo actúa cuando el textarea **no** está enfocado
- `Escape` → `router.push('/challenges')`

### R → Try again

- Listener en `keydown` a nivel de `document` en `AttemptForm`
- Solo actúa cuando `result !== null` (hay un resultado visible)
- `r` o `R` → llama a `resetForm()`

---

## Implementación

### `AttemptForm.tsx`

Agregar `useEffect` con listener `keydown` en `document`:

```tsx
useEffect(() => {
  const handler = (e: KeyboardEvent) => {
    // Ctrl+Enter (o Cmd+Enter) → submit
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      formRef.current?.requestSubmit();
      return;
    }
    // R → retry (solo cuando hay resultado)
    if ((e.key === 'r' || e.key === 'R') && result !== null) {
      resetForm();
    }
  };
  document.addEventListener('keydown', handler);
  return () => document.removeEventListener('keydown', handler);
}, [result]);
```

- Usar `formRef` (nuevo `useRef<HTMLFormElement>`) apuntando al `<form>`
- Agregar debajo del textarea: `<p className="text-xs text-gray-400 mt-1">Ctrl+Enter to submit</p>`

### `challenges/[id]/page.tsx`

Agregar `useEffect` con listener `keydown` en `document`:

```tsx
useEffect(() => {
  const handler = (e: KeyboardEvent) => {
    const tag = (document.activeElement as HTMLElement)?.tagName;
    if (tag === 'TEXTAREA' || tag === 'INPUT') return;

    if (e.key === 'ArrowLeft' && prevId) router.push(`/challenges/${prevId}`);
    if (e.key === 'ArrowRight' && nextId) router.push(`/challenges/${nextId}`);
    if (e.key === 'Escape') router.push('/challenges');
  };
  document.addEventListener('keydown', handler);
  return () => document.removeEventListener('keydown', handler);
}, [prevId, nextId, router]);
```

---

## Escenarios de test

### `AttemptForm.test.tsx` — 4 tests nuevos

| Escenario | Resultado |
|-----------|-----------|
| Ctrl+Enter con texto en textarea → envía formulario | `api.post` es llamado |
| Ctrl+Enter con textarea vacío → muestra error de validación | Error visible, `api.post` no llamado |
| `r` cuando hay resultado visible → resetea el formulario | Textarea vacío, resultado oculto |
| Ctrl+Enter cuando loading → no envía doble | `api.post` llamado solo 1 vez |

### `challenges/[id]/page.test.tsx` — 5 tests nuevos

| Escenario | Resultado |
|-----------|-----------|
| `ArrowRight` con nextId → navega al siguiente challenge | `router.push('/challenges/next-id')` |
| `ArrowLeft` con prevId → navega al challenge anterior | `router.push('/challenges/prev-id')` |
| `ArrowRight` sin nextId → no navega | `router.push` no llamado con challenge id |
| `ArrowLeft` sin prevId → no navega | `router.push` no llamado con challenge id |
| `Escape` → navega a /challenges | `router.push('/challenges')` |

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `frontend/src/components/AttemptForm.tsx` | + keyboard handler (Ctrl+Enter, R) + hint texto |
| `frontend/src/app/challenges/[id]/page.tsx` | + keyboard handler (ArrowLeft, ArrowRight, Escape) |
| `frontend/src/components/AttemptForm.test.tsx` | + 4 tests de keyboard |
| `frontend/src/app/challenges/[id]/page.test.tsx` | + 5 tests de keyboard |

---

## Recuento de tests nuevos

| Suite | Tests nuevos |
|-------|-------------|
| `Frontend.Tests (AttemptForm)` | +4 |
| `Frontend.Tests (challenge detail page)` | +5 |
| **Total nuevos** | **9** |
| **Grand total esperado** | **390 + 9 = 399** |

---

## Qué NO es esta fase

- No agrega atajos en `/challenges` (lista), `/history`, ni `/stats`
- No implementa modal de ayuda con lista de atajos
- No cambia el comportamiento del mouse ni del tab-focus
- No modifica el backend
- No agrega soporte para gamepad ni otros dispositivos de entrada

---

## Criterios de éxito

- `Ctrl+Enter` en el textarea envía el formulario
- `ArrowLeft`/`ArrowRight` navegan entre challenges cuando el textarea no está enfocado
- `Escape` vuelve a la lista de challenges cuando el textarea no está enfocado
- `R` reinicia el formulario cuando hay un resultado visible
- El hint "Ctrl+Enter to submit" es visible bajo el textarea
- 9 tests nuevos en verde
- 390 tests anteriores siguen en verde
- Grand total: **399/399 ✅**

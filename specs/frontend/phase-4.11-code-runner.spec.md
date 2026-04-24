# Spec: Phase 4.11 — Code Runner Exercise UI

**Tipo**: Frontend Feature (nuevo componente + página de detalle actualizada)  
**Ubicación**:
- `frontend/src/components/CodeRunnerForm.tsx` (nuevo)
- `frontend/src/components/CodeRunnerForm.test.tsx` (nuevo)
- `frontend/src/app/challenges/[id]/page.tsx` (update — renderizado condicional)
- `frontend/src/app/challenges/[id]/page.test.tsx` (update)
**Versión**: 1.0

---

## Qué es

Phase 4.11 introduce `CodeRunnerForm`, un componente que embebe un editor de código (Monaco Editor) y ejecuta el código del usuario contra test cases en el navegador — sin servidor. Si todos los test cases pasan, el usuario puede enviar el intento. Si alguno falla, puede seguir editando.

Este es el diferenciador principal del producto: el usuario resuelve problemas reales de código, ve los tests pasar en tiempo real, y solo cuando está seguro envía el intento al backend.

---

## Prerrequisito

El backend debe devolver `starterCode` y `testCases` en `GET /challenges/{id}` (spec `challenge-code-runner.spec.md`).

---

## Contrato HTTP

`GET /api/v1/challenges/{id}` devuelve (para tipo CodeRunner):
```typescript
{
  "type": "CodeRunner",
  "starterCode": "function solution(a, b) {\n  // Write your code here\n}",
  "testCases": [
    { "input": "2, 3", "expectedOutput": "5", "description": "Returns sum of 2 and 3" }
  ]
}
```

`POST /api/v1/challenges/{id}/attempt` recibe:
```typescript
{ "userAnswer": "PASS" | "FAIL", "elapsedSeconds": number }
```

---

## Dependencia: `@monaco-editor/react`

Librería para embeber Monaco Editor (el mismo editor de VS Code) en React. Se instala con `npm install @monaco-editor/react`.

---

## Comportamiento: `CodeRunnerForm`

### Props

```typescript
interface CodeRunnerFormProps {
  challengeId: string;
  timeLimitSecs: number;
  starterCode: string;
  testCases: CodeTestCase[];
  onSuccess?: (result: AttemptResult) => void;
}

interface CodeTestCase {
  input: string;
  expectedOutput: string;
  description: string;
}
```

### Estado interno

- `code: string` — código del usuario, inicializado con `starterCode`
- `testResults: TestResult[]` — resultado de cada test case tras la última ejecución
- `allPassed: boolean` — `true` si todos los tests pasaron en la última ejecución
- `loading: boolean` — submit en curso
- `error: string` — error de red
- `result: AttemptResult | null` — resultado del backend tras submit

```typescript
interface TestResult {
  description: string;
  passed: boolean;
  actualOutput: string;
  error?: string;  // si el código lanzó excepción
}
```

### Editor de código

- Monaco Editor con lenguaje `javascript`
- Altura mínima: 300px, responsiva
- Tema: `vs-dark` (oscuro, estilo VS Code)
- El código inicial es `starterCode` del challenge
- El usuario puede editar libremente

### Panel de test cases

Debajo del editor: lista de test cases con estado visual:
- ⏳ Sin ejecutar (estado inicial)
- ✅ Test passed — verde
- ❌ Test failed — rojo, muestra `actualOutput` y `expectedOutput`
- 💥 Error — el código lanzó excepción, muestra mensaje de error

### Botón "Run Tests"

- Siempre disponible (no depende de si el código cambió)
- Al hacer click: ejecuta el código del usuario contra todos los test cases
- Ejecución ocurre en el cliente usando un `Worker` inline o `Function()` en un try-catch aislado
- Actualiza `testResults` y `allPassed`
- No llama al backend

### Mecanismo de ejecución en el cliente

Para cada test case con `input` y `expectedOutput`:
1. Envolver el código del usuario + llamada con el input en un bloque try-catch
2. Ejecutar con `new Function(wrappedCode)()`
3. Comparar el valor retornado (`.toString()`) con `expectedOutput` (trim + case-insensitive)
4. Registrar `passed`, `actualOutput`, y `error` si hubo excepción

El código del usuario no puede acceder al DOM ni a `window` (usar Web Worker o iframe sandboxed para mayor seguridad — decisión de implementación). Para tests unitarios del componente, se mockea la función de ejecución.

### Botón "Submit Attempt"

- Solo se habilita cuando `allPassed === true` (todos los tests pasaron)
- Llama `POST /challenges/{id}/attempt` con `userAnswer: "PASS"`
- Si no todos pasan: botón deshabilitado con texto "Run tests first"
- Durante loading: botón deshabilitado con texto "Submitting..."

### Timer visual

Mismo diseño que `AttemptForm`: barra de progreso con estados normal/warning/critical basados en `remainingSeconds / timeLimitSecs`.

### Tarjeta de resultado (tras submit)

Idéntica a `AttemptForm` y `MultipleChoiceForm`:
- "Correct!" (verde) o "Not quite" (ámbar)
- ELO, streak, badges si presentes
- Botones: "Try again" (resetea editor al `starterCode` y limpia tests) y "Back to challenges"

### Manejo de errores de red

| Código | Mensaje |
|--------|---------|
| 401 | clearAuth() + redirect `/login` |
| 404 | "Challenge not found." |
| 500 | "Server error. Try again." |

---

## Comportamiento: página `/challenges/[id]`

### Interfaz `Challenge` local — campos nuevos

```typescript
interface Challenge {
  // campos existentes...
  type: 'OpenText' | 'MultipleChoice' | 'CodeRunner' | 'Ordering';
  options: string[];
  starterCode: string;
  testCases: CodeTestCase[];
  items: string[];
}
```

### Renderizado condicional actualizado

```
'MultipleChoice' → <MultipleChoiceForm />
'CodeRunner'     → <CodeRunnerForm />    ← nuevo
'OpenText'       → <AttemptForm />
```

---

## Invariantes

1. El usuario nunca puede hacer submit con `allPassed = false` — el botón está semánticamente deshabilitado.
2. `userAnswer` enviado al backend siempre es `"PASS"` (cuando todos los tests pasan).
3. El timer sigue corriendo mientras el usuario edita — no se pausa al ejecutar tests.
4. "Try again" resetea el editor al `starterCode` original, no a un string vacío.
5. Los test results se limpian al hacer "Try again".
6. El código del usuario no se persiste en `localStorage` (a diferencia de `AttemptForm`).

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `frontend/src/components/CodeRunnerForm.tsx` | Nuevo componente |
| `frontend/src/components/CodeRunnerForm.test.tsx` | Tests nuevos |
| `frontend/src/app/challenges/[id]/page.tsx` | Campos nuevos + renderizado condicional actualizado |
| `frontend/src/app/challenges/[id]/page.test.tsx` | Tests para CodeRunner |

---

## Escenarios de test — `CodeRunnerForm.test.tsx`

> La función de ejecución de código se mockea completamente en tests. No se ejecuta código real en Jest.

### Rendering (4)

| Escenario | Resultado |
|-----------|-----------|
| Renderiza el editor de código (mock de Monaco) | Elemento del editor presente |
| Muestra `starterCode` como valor inicial del editor | Código inicial visible |
| Muestra la lista de test cases con estado pendiente | N test cases en estado "not run" |
| Botón "Submit" deshabilitado en estado inicial | `disabled` presente |

### Ejecución de tests (5)

| Escenario | Resultado |
|-----------|-----------|
| Click "Run Tests" cuando todos los tests pasan (mock) | `allPassed = true`, tests marcados ✅ |
| Click "Run Tests" cuando un test falla (mock) | Test marcado ❌, muestra `actualOutput` |
| Click "Run Tests" cuando código lanza excepción (mock) | Test marcado 💥, muestra mensaje de error |
| Tras todos los tests pasar, botón Submit se habilita | `disabled` removido del botón Submit |
| Tras test fallido, botón Submit sigue deshabilitado | `disabled` permanece |

### Submit (4)

| Escenario | Resultado |
|-----------|-----------|
| Submit con `allPassed = true` llama API con `userAnswer: "PASS"` | Payload correcto |
| Submit deshabilita botón durante loading | `disabled` activo |
| Resultado `isCorrect: true` muestra "Correct!" | Texto verde |
| Resultado `isCorrect: false` muestra "Not quite" | Texto ámbar |

### Reset y resultado (3)

| Escenario | Resultado |
|-----------|-----------|
| "Try again" resetea el editor al `starterCode` | Editor vuelve al código inicial |
| "Try again" limpia los test results | Tests vuelven a estado pendiente |
| Resultado con `newEloRating` muestra ELO | "ELO: {n}" visible |

### Errores de red (3)

| Escenario | Resultado |
|-----------|-----------|
| Error 401 hace clearAuth + redirect | `router.push('/login')` llamado |
| Error 404 muestra mensaje | "Challenge not found." |
| Error 500 muestra mensaje | "Server error. Try again." |

### Accesibilidad (2)

| Escenario | Resultado |
|-----------|-----------|
| Mensajes de error tienen `role="alert"` | Atributo presente |
| Cada test case tiene descripción visible | Texto de `description` en DOM |

**Total estimado `CodeRunnerForm.test.tsx`**: ~21 tests

### `page.test.tsx` — tests nuevos (2)

| Escenario | Resultado |
|-----------|-----------|
| Challenge con `type: 'CodeRunner'` renderiza `CodeRunnerForm` | Componente presente |
| `CodeRunnerForm` recibe `starterCode` y `testCases` correctos | Props correctas |

---

## Qué NO incluye esta fase

- No soporta otros lenguajes (Python, C#) — solo JavaScript
- No usa un sandbox externo (Judge0, Piston) — la ejecución es local en el browser
- No guarda el código del usuario en ningún lugar (no localStorage, no backend)
- No muestra diff entre output actual y esperado (solo los strings)
- No extrae la tarjeta de resultado ni el timer a componentes compartidos

---

## Criterios de éxito

- `CodeRunnerForm` funciona con Monaco Editor y ejecución mockeada en tests
- La página de detalle renderiza el formulario correcto para `CodeRunner`
- ~23 tests nuevos en verde
- Tests frontend existentes sin regresiones

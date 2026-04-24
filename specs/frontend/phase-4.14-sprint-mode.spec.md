# Spec: Phase 4.14 — Sprint Mode

**Tipo**: Frontend Feature (nueva ruta + componentes de sesión de sprint)  
**Ubicación**:
- `frontend/src/app/sprint/page.tsx` (nuevo)
- `frontend/src/app/sprint/page.test.tsx` (nuevo)
- `frontend/src/components/SprintSession.tsx` (nuevo)
- `frontend/src/components/SprintSession.test.tsx` (nuevo)
**Versión**: 1.0

---

## Qué es

Phase 4.14 introduce el **Modo Sprint**: una sesión cronometrada de 5 challenges consecutivos con un timer global de 3 minutos. Al terminar (por agotar challenges o tiempo), el usuario ve un resumen con puntaje, ELO ganado y badges obtenidos. Es pura gamificación sobre la infraestructura existente — no requiere nuevos tipos de challenge ni cambios de backend.

---

## Prerrequisito

Requiere la feature de Multiple Choice (Phase 4.10) para que los challenges de ese tipo se rendericen correctamente en el sprint.

---

## Contratos HTTP utilizados

### `GET /api/v1/challenges?page=1&pageSize=20`

Obtener el pool de challenges disponibles. Se seleccionan 5 aleatoriamente en el cliente.

### `POST /api/v1/challenges/{id}/attempt`

Para cada challenge del sprint (misma firma que siempre).

No hay endpoint de "sprint" en el backend — cada intento se registra de forma independiente.

---

## Ruta: `/sprint`

### Setup de sprint (pantalla inicial)

Antes de comenzar, el usuario ve:
- Título "Sprint Mode"
- Descripción: "5 challenges — 3 minutes — how many can you solve?"
- Selector de dificultad (Easy / Medium / Hard / Mixed) — filtra el pool inicial
- Botón "Start Sprint" — inicia la sesión y el timer global

### Estado de carga

Si el fetch del pool de challenges falla, muestra error y botón "Try again". Si no hay challenges disponibles para el filtro seleccionado, muestra mensaje y permite cambiar dificultad.

---

## Comportamiento: sesión de sprint (`SprintSession`)

### Props

```typescript
interface SprintSessionProps {
  challenges: Challenge[];        // exactamente 5, pre-seleccionados
  onComplete: (summary: SprintSummary) => void;
}

interface SprintSummary {
  totalChallenges: number;        // siempre 5
  solved: number;                 // respuestas correctas
  attempted: number;              // challenges respondidos (incluso incorrectos)
  skipped: number;                // challenges no respondidos al agotar el tiempo
  eloDeltas: number[];            // ELO ganado/perdido por cada intento (puede tener < 5 elementos)
  badgesEarned: string[];         // badges acumulados de todos los intentos
  totalElapsedSecs: number;       // tiempo total usado
}
```

### Timer global de 3 minutos

- Contador regresivo de 180 segundos visible en la parte superior durante toda la sesión
- Barra de progreso global que se reduce con el tiempo
- Cuando llega a 0: el sprint termina inmediatamente (no se puede enviar más respuestas)
- Estados: normal (azul) → warning (ámbar, últimos 60s) → critical (rojo, últimos 30s)

### Progreso de challenges

- Indicador de posición: "Challenge 2 of 5" visible en todo momento
- Al responder un challenge (correcto o incorrecto): avanza automáticamente al siguiente tras 1.5s de delay mostrando el resultado
- Al llegar al challenge 5 y responderlo: termina el sprint (muestra resumen)

### Renderizado de cada challenge

- Muestra el título y descripción del challenge actual
- Renderiza el formulario correcto según `challenge.type`:
  - `MultipleChoice` → `MultipleChoiceForm` (sin botón "Back to challenges")
  - `OpenText` → versión inline de `AttemptForm` (sin botón "Back to challenges")
  - (CodeRunner y Ordering son post-MVP para sprint — si aparecen, mostrar como OpenText)
- El `timeLimitSecs` del challenge individual se ignora — solo importa el timer global

### Botón "Skip"

- Disponible en cualquier momento antes de responder
- Omite el challenge actual y avanza al siguiente sin registrar un intento en el backend
- Incrementa `skipped` en el resumen final

### Respuesta registrada

Para cada intento enviado al backend:
- Guarda `newEloRating`, `newStreak`, `newBadges` de la respuesta para el resumen
- No muestra tarjeta de resultado completa — solo un flash visual breve:
  - ✅ flash verde + "Correct!" si `isCorrect = true`
  - ❌ flash rojo + "Not quite" si `isCorrect = false`
  - Luego avanza automáticamente al siguiente challenge tras 1.5s

---

## Pantalla de resumen final

Se muestra cuando:
- El usuario responde (o salta) los 5 challenges, O
- El timer global llega a 0

Contenido:
- Título: "Sprint Complete!" o "Time's up!"
- Score: `{solved}/{attempted}` (ej: "3/5 correct")
- ELO final (el último `newEloRating` recibido, si hay alguno)
- Racha actual (`newStreak` del último intento)
- Badges ganados en el sprint (lista de strings, puede estar vacía)
- Tiempo usado: `{totalElapsedSecs}s / 180s`
- Botones:
  - "Play Again" — reinicia el sprint con nuevos challenges aleatorios
  - "Back to Challenges" — navega a `/challenges`

---

## Invariantes

1. El sprint siempre tiene exactamente 5 challenges — nunca más, nunca menos.
2. El timer global no se pausa entre challenges ni durante el submit de un intento.
3. Cuando el timer llega a 0, no se puede enviar ningún intento más (incluso si había uno en curso).
4. Los intentos se registran en el backend igual que en modo normal — el sprint no tiene endpoint propio.
5. Un challenge "skipped" no genera un intento en el backend.
6. Los 5 challenges se seleccionan aleatoriamente del pool al inicio — no se repiten en la misma sesión.

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `frontend/src/app/sprint/page.tsx` | Nuevo — setup + orquestación |
| `frontend/src/app/sprint/page.test.tsx` | Tests de la página |
| `frontend/src/components/SprintSession.tsx` | Nuevo — sesión activa + resumen |
| `frontend/src/components/SprintSession.test.tsx` | Tests del componente |

---

## Qué NO incluye esta fase

- No hay leaderboard de sprint ni comparación con otros usuarios
- No hay modos de sprint personalizados (más de 5 challenges, tiempo distinto)
- No guarda el resultado del sprint en el backend — solo los intentos individuales
- No soporta CodeRunner ni Ordering en el sprint (post-MVP)
- No hay pausa ni reanudación del sprint

---

## Escenarios de test — `SprintSession.test.tsx`

### Rendering inicial (3)

| Escenario | Resultado |
|-----------|-----------|
| Muestra el primer challenge al montar | Título del challenge 1 visible |
| Muestra "Challenge 1 of 5" | Indicador de progreso visible |
| Timer global empieza en 180s visible | "180" o "3:00" en el DOM |

### Progreso de challenges (4)

| Escenario | Resultado |
|-----------|-----------|
| Tras responder el challenge 1 (mock de submit), avanza al challenge 2 | "Challenge 2 of 5" visible |
| Al skipear, avanza al siguiente sin llamar API | "Challenge 2 of 5" y API no llamada |
| Responder el challenge 5 llama `onComplete` con el resumen | Callback disparado |
| Al agotar el timer (timer mockeado a 0), llama `onComplete` | Callback disparado |

### Timer visual (3)

| Escenario | Resultado |
|-----------|-----------|
| Timer visible en estado normal (> 60s) | Clases de color normal |
| Timer visible en estado warning (≤ 60s, mock) | Clases de color ámbar |
| Timer en estado critical (≤ 30s, mock) | Clases de color rojo |

### Resumen final (4)

| Escenario | Resultado |
|-----------|-----------|
| `onComplete` recibe `solved` correcto según intentos registrados | Valor correcto |
| `onComplete` recibe `skipped` correcto | Valor correcto |
| `onComplete` recibe `badgesEarned` acumulados de todos los intentos | Array con todos los badges |
| `onComplete` recibe `totalElapsedSecs` mayor que 0 | Valor > 0 |

### Errores de submit (2)

| Escenario | Resultado |
|-----------|-----------|
| Error 401 en submit durante sprint hace clearAuth + redirect | `router.push('/login')` |
| Error 500 muestra mensaje breve y permite continuar | Mensaje visible, sprint no termina |

**Total estimado `SprintSession.test.tsx`**: ~16 tests

### `sprint/page.test.tsx` (5 tests estimados)

| Escenario | Resultado |
|-----------|-----------|
| Redirige a `/login` si no hay token | Redirect a login |
| Muestra pantalla de setup antes de iniciar | Botón "Start Sprint" visible |
| "Start Sprint" fetchea challenges y muestra `SprintSession` | Sesión visible |
| Al completar el sprint, muestra el resumen | Pantalla de resumen visible |
| "Play Again" reinicia el sprint con nuevos challenges | Nueva sesión iniciada |

---

## Criterios de éxito

- Modo sprint completo y funcional: 5 challenges, timer global, resumen final
- ~21 tests nuevos en verde
- Tests frontend existentes sin regresiones
- El sprint usa los mismos endpoints existentes — sin cambios en el backend

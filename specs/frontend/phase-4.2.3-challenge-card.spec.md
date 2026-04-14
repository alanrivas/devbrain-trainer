# Phase 4.2.3 — ChallengeCard Component Spec

**Tipo**: Frontend Component (Extraction)  
**Ubicación**: `frontend/src/components/ChallengeCard.tsx`  
**Fecha**: 14 de Abril 2026  

---

## Qué es

`ChallengeCard` es un componente reutilizable que renderiza una tarjeta individual de challenge en la lista de challenges. Extrae lógica visual y comportamental del componente `challenges/page.tsx` para mejorar composabilidad y testabilidad.

---

## Propiedades (Props)

| Prop | Tipo | Requerido | Descripción |
|------|------|-----------|-------------|
| `challenge` | `Challenge` | ✅ | Objeto con `id`, `title`, `description`, `category`, `difficulty` |
| `onAttempt` | `(challengeId: string) => void` | ✅ | Callback cuando usuario hace click en "Attempt" |

### Challenge Interface
```typescript
interface Challenge {
  id: string;
  title: string;
  description: string;
  category: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
}
```

---

## Comportamientos

### Renderizado

- Renderiza tarjeta (card) con fondo blanco, redondeado y sombra
- Muestra título del challenge
- Muestra descripción corta
- Muestra badge con difficulty level (coloreado según dificultad)
- Muestra categoría como pequeño tag
- Muestra botón "Attempt →" derecha

### Coloreado por Dificultad

| Difficulty | Colores | Ejemplo |
|-----------|---------|---------|
| Easy | bg-green-100, text-green-800 | Verde claro |
| Medium | bg-yellow-100, text-yellow-800 | Amarillo claro |
| Hard | bg-red-100, text-red-800 | Rojo claro |

### Interactividad

- Card: Hover effect (shadow aumenta con transición suave)
- Botón "Attempt": 
  - Hover: text-blue-700 (más oscuro)
  - Click: Ejecuta `onAttempt(challenge.id)`
  - Nunca navega directamente (deja eso al parent)

### Accesibilidad

- Button tiene texto descriptivo ("Attempt →")
- Card es semánticamente correcta
- Colores contrastados según WCAG
- Ningún comportamiento dependiente de mouse-only

---

## Invariantes

1. `onAttempt` SIEMPRE es llamado cuando usuario hace click en "Attempt"
2. Nunca redirige directamente — siempre usa callback
3. Challenge data nunca es modificado por el componente
4. Dificultad SIEMPRE está coloreada correctamente según el mapa de colores

---

## Test Coverage (15 tests objetivo)

### Rendering (5)
- Should render challenge card with all content
- Should display title, description, category
- Should display difficulty badge
- Should have attempt button

### Styling by Difficulty (3)
- Easy: should have green styling
- Medium: should have yellow styling
- Hard: should have red styling

### Interaction (5)
- Should call onAttempt when button clicked
- Should pass correct challengeId to onAttempt
- Should show hover effects on card
- Should be keyboard accessible

### Props Validation (2)
- Should render with valid Challenge prop
- Should not render if Challenge prop missing

---

## Notas Implementación

- Este es un presentational component (no hace data fetching)
- Usa Tailwind CSS (como el resto del proyecto)
- No tiene estado interno
- Use React.FC<Props> o explicit typing
- Test con React Testing Library + userEvent

---

## Archivo de Salida Post-Spec

Después de aplicar `spec-implement`:

1. **ChallengeCard.tsx** — Componente extraído, limpio, reutilizable
2. **ChallengeCard.test.tsx** — 15 tests (rendering, styles, interaction, a11y)
3. **challenges/page.tsx** — Actualizado para usar ChallengeCard component

---

## Estimación de Esfuerzo

- Extracción de componente: ~10 min
- Tests: ~20 min
- Refactor challenges/page.tsx: ~5 min
- **Total**: ~35 min
- **Target**: 15/15 tests en verde

# Spec: Phase 4.7 — Gamification Result Feedback

**Tipo**: Frontend Feature (feedback de gamificación en tarjeta de resultado)  
**Ubicación**:
- `frontend/src/components/AttemptForm.tsx`
- `frontend/src/components/AttemptForm.test.tsx`  
**Versión**: 1.0

---

## Qué es

Phase 4.7 expone los datos de gamificación que el backend ya devuelve en la respuesta de un intento pero que el frontend actualmente ignora. Tras cada submit, el usuario verá su nuevo ELO, su racha activa y los badges recién ganados — directamente en la tarjeta de resultado del `AttemptForm`.

---

## Contexto: datos disponibles

El endpoint `POST /challenges/{id}/attempt` ya devuelve en la respuesta:

```typescript
// Campos que el backend envía pero AttemptResult aún no mapea:
newEloRating: number;   // ELO del usuario tras el intento
newStreak:    number;   // días consecutivos de actividad
newBadges:    string[]; // badges ganados en este intento (puede ser vacío)
```

Estos campos existen en `AttemptResponseDto` (C#) pero no están en la interfaz `AttemptResult` del frontend ni se renderizan en la UI.

---

## Comportamiento

### 1 — Extender `AttemptResult`

Agregar tres campos opcionales a la interfaz exportada `AttemptResult`:

```typescript
export interface AttemptResult {
  // campos existentes...
  newEloRating?: number;
  newStreak?: number;
  newBadges?: string[];
}
```

Los campos son opcionales para no romper el mock existente en `challenges/[id]/page.test.tsx`.

### 2 — Mostrar ELO en la tarjeta de resultado

- Cuando `result.newEloRating` está presente (> 0), la tarjeta de resultado muestra:
  `"ELO: {newEloRating}"`
- Se muestra en ambos casos: correcto e incorrecto

### 3 — Mostrar racha en la tarjeta de resultado

- Cuando `result.newStreak` está presente (≥ 0), la tarjeta muestra:
  `"Streak: {newStreak} days"`
- Se muestra en ambos casos: correcto e incorrecto

### 4 — Mostrar badges ganados

- Cuando `result.newBadges` tiene al menos un elemento, se muestra una sección con cada badge:
  `"New badge: {badgeName}"` (un elemento por badge)
- Cuando `result.newBadges` está vacío (`[]`) o es `undefined`, no se renderiza ninguna mención a badges

### 5 — Sin regresiones en campos existentes

- Los campos `isCorrect`, `correctAnswer`, `elapsedSeconds` y el performance badge siguen funcionando igual
- El texto "Correct!" / "Not quite" no cambia
- Los botones "Try again" y "Back to challenges" no cambian

---

## Invariantes

1. Los tres campos nuevos son opcionales — si el backend no los envía (o la respuesta es parcial), la UI no falla
2. Un `newStreak` de `0` sí se muestra: `"Streak: 0 days"` (el usuario perdió la racha, dato relevante)
3. El ELO y la racha se muestran **siempre** cuando están presentes, no solo en intentos correctos
4. Los badges se muestran como lista; si hay más de uno se rinden todos

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `frontend/src/components/AttemptForm.tsx` | Extender interfaz + renderizar ELO / streak / badges |
| `frontend/src/components/AttemptForm.test.tsx` | Tests nuevos para los tres campos |

---

## Escenarios de test — `AttemptForm.test.tsx` (tests nuevos)

### ELO (~2)

| Escenario | Resultado |
|-----------|-----------|
| Resultado incluye `newEloRating: 1250` | Muestra texto que coincida con `/ELO: 1250/i` |
| Resultado no incluye `newEloRating` (undefined) | No se renderiza texto de ELO |

### Streak (~2)

| Escenario | Resultado |
|-----------|-----------|
| Resultado incluye `newStreak: 3` | Muestra texto que coincida con `/Streak: 3 days/i` |
| Resultado no incluye `newStreak` (undefined) | No se renderiza texto de streak |

### Badges (~3)

| Escenario | Resultado |
|-----------|-----------|
| `newBadges: ['FirstAttempt']` | Muestra `"New badge: FirstAttempt"` |
| `newBadges: ['FirstAttempt', 'TenAttempts']` | Muestra ambos badges |
| `newBadges: []` | No se renderiza sección de badges |

**Total de tests nuevos**: ~7  
**Total frontend estimado**: 120 + 7 = **127 tests**

---

## Qué NO es esta fase

- No cambia el contrato del backend (el endpoint ya devuelve estos datos)
- No modifica la página `/stats` (ya muestra ELO/streak a nivel global)
- No añade animaciones o efectos visuales al ganar un badge
- No persiste el historial de ELO ni muestra la variación (`+25 ELO`)
- No cambia `challenges/[id]/page.tsx` ni sus tests (el mock existente sigue válido porque los campos son opcionales)

---

## Criterios de éxito

- El usuario ve su ELO actual, racha y badges ganados inmediatamente tras enviar un intento
- Los ~7 tests nuevos están en verde
- Los 120 tests previos siguen en verde (sin regresiones)
- Grand total: **369 + 7 = 376 tests en verde**

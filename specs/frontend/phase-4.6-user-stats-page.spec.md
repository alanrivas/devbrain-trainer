# Spec: Phase 4.6 — User Stats Page

**Tipo**: Frontend Feature (página de estadísticas / perfil de usuario)  
**Ubicación**:
- `frontend/src/app/stats/page.tsx` (nuevo)
- `frontend/src/app/stats/page.test.tsx` (nuevo)  
**Versión**: 1.0

---

## Qué es

Phase 4.6 crea la página `/stats` del usuario autenticado. Esta página consume dos endpoints ya existentes del backend:
- `GET /api/v1/users/me/stats` — estadísticas de uso (intentos, aciertos, racha, ELO)
- `GET /api/v1/users/me/badges` — badges ganados por el usuario

El link a `/stats` ya existe en `Header.tsx` para usuarios autenticados; esta phase lo activa creando la página destino.

---

## Endpoints de backend usados

### `GET /api/v1/users/me/stats`

Requiere JWT. Devuelve:

```typescript
interface UserStatsResponse {
  userId: string;
  displayName: string;
  totalAttempts: number;
  correctAttempts: number;
  accuracyRate: number;       // 0–100 float
  currentStreak: number;      // días consecutivos
  eloRating: number;          // entero, empieza en 1000
  lastAttemptAt: string | null; // ISO 8601 o null si nunca intentó
}
```

### `GET /api/v1/users/me/badges`

Requiere JWT. Devuelve array de:

```typescript
interface UserBadgeResponse {
  type: string;       // ej. "FirstAttempt", "TenAttempts", "StreakThree"
  earnedAt: string;  // ISO 8601
}
```

---

## Comportamiento

### Acceso y autenticación

- Si el usuario no tiene token JWT, la página redirige inmediatamente a `/login`
- Si el backend devuelve 401, se llama `clearAuth()` y se redirige a `/login`

### Estado de carga

- Mientras se esperan las dos respuestas del backend, se muestra un indicador de carga (`Loading stats...`)
- Las dos peticiones se lanzan en paralelo (`Promise.all` o dos `useEffect` independientes)

### Datos mostrados — Stats

La página muestra una sección de estadísticas con estos campos:

| Campo visual | Fuente | Formato |
|---|---|---|
| Nombre de usuario | `displayName` | texto plano |
| Total de intentos | `totalAttempts` | número entero |
| Intentos correctos | `correctAttempts` | número entero |
| Precisión | `accuracyRate` | `XX.X%` (un decimal) |
| Racha actual | `currentStreak` | `X días` |
| ELO | `eloRating` | número entero |
| Último intento | `lastAttemptAt` | fecha local, o "Never" si null |

### Datos mostrados — Badges

La página muestra una sección de badges:

- Si el usuario tiene badges: muestra una lista con el tipo de badge y la fecha en que lo ganó
- Si el usuario **no** tiene badges: muestra el texto `"No badges earned yet"`
- La fecha de cada badge se muestra en formato de fecha local legible

### Manejo de errores

- Error genérico (5xx u otro): muestra un `role="alert"` con texto que coincida con `/failed to load/i`
- El error 401 no muestra alerta — redirige directamente sin mostrar contenido

---

## Invariantes

1. La página nunca muestra datos de stats y el estado de carga al mismo tiempo
2. Si solo falla una de las dos peticiones (stats o badges), la página muestra el error genérico — no muestra datos parciales
3. El campo `accuracyRate` siempre se muestra con exactamente un decimal y símbolo `%`
4. Si `lastAttemptAt` es null o ausente, se muestra el texto `"Never"` (no fecha vacía)

---

## Archivos objetivo

| Archivo | Cambio |
|---------|--------|
| `frontend/src/app/stats/page.tsx` | Nueva página que consume los dos endpoints |
| `frontend/src/app/stats/page.test.tsx` | Tests de la página |

---

## Escenarios de test — `stats/page.test.tsx`

### Auth y redirects (~2)

| Escenario | Resultado |
|-----------|-----------|
| No hay token | Redirige a `/login` |
| Backend devuelve 401 | Llama `clearAuth()` + redirige a `/login` |

### Estado de carga (~1)

| Escenario | Resultado |
|-----------|-----------|
| Petición pendiente | Muestra texto que coincida con `/loading stats/i` |

### Renderizado de stats (~4)

| Escenario | Resultado |
|-----------|-----------|
| Stats cargadas correctamente | Muestra `displayName` del usuario |
| Stats cargadas correctamente | Muestra `totalAttempts` y `correctAttempts` |
| Stats cargadas correctamente | Muestra `accuracyRate` formateado como `XX.X%` |
| `lastAttemptAt` es null | Muestra `"Never"` |

### Renderizado de badges (~2)

| Escenario | Resultado |
|-----------|-----------|
| Usuario tiene badges | Muestra el tipo de cada badge |
| Usuario no tiene badges | Muestra `"No badges earned yet"` |

### Manejo de errores (~1)

| Escenario | Resultado |
|-----------|-----------|
| Backend devuelve 5xx en stats | Muestra `role="alert"` con texto `/failed to load/i` |

**Total estimado**: ~10 tests

---

## Qué NO es esta fase

- No implementa un endpoint nuevo en el backend (ya existen los dos necesarios)
- No muestra historial de intentos individuales (no hay endpoint de lista de attempts aún)
- No permite editar el perfil del usuario
- No cambia el `Header.tsx` (el link a `/stats` ya existe)
- No agrega paginación de badges
- No implementa un sistema de ranking o leaderboard

---

## Criterios de éxito

- La ruta `/stats` renderiza sin errores para un usuario autenticado
- Los ~10 tests están en verde
- Los tests previos (110/110) siguen en verde
- Grand total: 359 + ~10 = ~369 tests en verde

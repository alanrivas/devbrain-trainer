# Spec: Phase 4.4 — Attempt UX Enhancements

**Tipo**: Frontend Feature (UX refinement)
**Ubicación**:
- `frontend/src/components/AttemptForm.tsx`
- `frontend/src/app/challenges/[id]/page.tsx`
- `frontend/src/app/challenges/page.tsx` (si hace falta ajustar CTA)
**Versión**: 1.0

---

## Qué es

Phase 4.4 mejora la experiencia de resolución de challenges después de que el flujo funcional ya existe. La meta es hacer el intento más claro, más guiado y más accionable: el usuario ve el tiempo restante, recibe feedback visual más rico al enviar y tiene salidas claras tras completar un intento.

---

## Contrato funcional

### Estado del intento

El formulario debe distinguir, como mínimo, estos estados visibles:
- `idle`: el usuario está escribiendo
- `submitting`: el intento se está enviando
- `success`: el backend respondió con intento correcto o incorrecto
- `error`: ocurrió un error validable o de red

### Temporizador visual

- La UI muestra el tiempo transcurrido y/o restante de forma visible
- El tiempo restante se actualiza mientras la página está abierta
- Cuando se acerca al límite, el indicador cambia visualmente para avisar al usuario
- El temporizador no bloquea el submit por sí mismo; solo informa

### Navegación post-attempt

Después de un intento exitoso o fallido, la pantalla debe ofrecer acciones claras:
- volver al listado de challenges
- intentar de nuevo
- permanecer en la página actual si el usuario quiere corregir su respuesta

---

## Comportamientos funcionales

### 1. Visualización del tiempo restante

- `AttemptForm` muestra el tiempo restante basado en `timeLimitSecs`
- La cuenta visible se actualiza cada segundo mientras el formulario está montado
- Debe ser fácil de leer en desktop y mobile
- Si el tiempo llega a cero, la UI muestra estado agotado o crítico

### 2. Indicador visual de presión de tiempo

- La barra o badge de tiempo cambia de color según el progreso:
  - normal al inicio
  - advertencia cuando resta poca parte del tiempo
  - crítico cuando queda muy poco
- El cambio debe ser perceptible sin depender solo del color

### 3. Estados de envío más ricos

- Mientras se envía:
  - el botón muestra un texto de progreso más claro
  - el textarea se deshabilita
  - se muestra un bloque de estado o spinner textual
- Al completar:
  - se muestra un resumen breve del intento
  - se conserva el resultado visible hasta que el usuario decida continuar

### 4. Feedback de resultado mejorado

- Si `isCorrect = true`:
  - mensaje de éxito visible
  - CTA para volver al listado o ir al siguiente paso
- Si `isCorrect = false`:
  - mensaje de corrección visible
  - se muestra la respuesta correcta cuando venga del backend
  - CTA para reintentar sin perder contexto

### 5. Navegación post-attempt

- La página de detalle ofrece una salida clara después del intento
- El usuario no queda atrapado en una pantalla sin acciones
- La navegación de retorno debe mantener el flujo simple hacia `/challenges`

---

## Invariantes

1. El temporizador visual nunca altera el payload enviado al backend.
2. El usuario sigue pudiendo enviar el intento mientras el tiempo restante sea mayor que cero.
3. La UI nunca pierde el resultado una vez que el backend respondió, salvo que el usuario reinicie manualmente el formulario.
4. La navegación post-attempt siempre ofrece una ruta clara de vuelta al listado.

---

## Qué NO es esta fase

- No modifica el contrato HTTP backend
- No agrega leaderboard
- No agrega autosave de borradores
- No agrega chat ni hints automáticos
- No cambia el scoring de gamificación

---

## Componentes / archivos afectados

1. `frontend/src/components/AttemptForm.tsx`
2. `frontend/src/components/AttemptForm.test.tsx`
3. `frontend/src/app/challenges/[id]/page.tsx`
4. `frontend/src/app/challenges/[id]/page.test.tsx`

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| Renderiza timer inicial | OK — tiempo visible |
| El timer decrece con el tiempo | OK — la UI se actualiza |
| El timer entra en estado de warning | OK — cambia el estilo |
| Envío en progreso | OK — muestra estado richer/loading |
| Resultado correcto | OK — muestra success + acciones |
| Resultado incorrecto | OK — muestra feedback + acciones |
| Reset manual del formulario | OK — limpia estado y reinicia timer |
| Volver al listado | OK — navega a `/challenges` |

---

## Criterios de éxito

- El formulario de intento se siente guiado y no plano
- El usuario siempre ve cuánto tiempo le queda
- El resultado del intento ofrece un siguiente paso claro
- No se rompen los tests existentes de Phase 4.2.3 y Phase 4.3

# Phase 4.2.2 — Register Page Spec

**Objetivo**: Página de registro con formulario validado y tests siguiendo SDD+TDD. Componentes y página existen pero necesitan tests formales.

**Ubicación**: 
- Página: `frontend/src/app/register/page.tsx`
- Componente: `frontend/src/components/RegisterForm.tsx`
- Tests: `frontend/src/components/RegisterForm.test.tsx` + `frontend/src/app/register/page.test.tsx`

**Stack**: Next.js 16.2.3 + React 19.2.4 + TypeScript 5 + Tailwind CSS 4 + Jest + React Testing Library

---

## Funcionalidad Existente (a documentar en tests)

### RegisterForm.tsx (Componente Existente)
```typescript
// Debe tener:
- Inputs: email, password, confirmPassword, displayName
- Validación client-side:
  * Email: requerido + formato válido
  * Password: requerido + mínimo 8 caracteres + complejidad (mayús, minús, número)
  * Confirm Password: debe coincidir con password
  * Display Name: requerido + 2-50 caracteres
- State: email, password, confirmPassword, displayName, loading, errors
- Manejo de errores: mostrar mensajes específicos por campo
- Submit: POST /api/v1/auth/register con { email, password, displayName }
- Success: guardar token/user en AuthContext + redirect a /challenges
- Loading: deshabilitar inputs y botón mientras se procesa
- Error: mostrar banner con mensaje de error
```

### register/page.tsx (Página Existente)
```typescript
// Debe tener:
- Importar RegisterForm
- useRouter para redirect si ya autenticado
- useAuth para verificar isAuthenticated
- Layout responsivo con gradient background
- Link a /login para usuarios con cuenta
- Metadata: title, description
```

---

## Comportamientos Esperados (Testing)

### RegisterForm Component Tests

#### Rendering (5 tests)
1. **Should render all form inputs**
   - Input email, password, confirmPassword, displayName deben estar presentes
   - Verificar placeholders y labels

2. **Should render submit button**
   - Botón "Create Account" debe estar visible
   - Debe ser de tipo submit

3. **Should render login link**
   - Link a /login con texto "Sign in" o "Already have an account?"
   - href debe ser "/login"

4. **Should render form title and subtitle**
   - Heading "Sign Up" o "Create Account"
   - Subtítulo descriptivo

5. **Should have accessible labels**
   - Todos los inputs deben tener label asociado con htmlFor
   - ARIA labels correctos

#### Validation (8 tests)
1. **Should reject empty email**
   - Submit sin email → error message
   - API no debe ser llamada

2. **Should reject invalid email format**
   - Email "notanemail" → error message
   - Email "test@" → error message

3. **Should reject short password**
   - Password < 8 caracteres → error message
   - Ejemplo: "Pass1!" (7 chars)

4. **Should reject weak password** (sin mayúscula O sin minúscula O sin número)
   - "PASSWORD123" (sin minúscula) → error
   - "password123" (sin mayúscula) → error
   - "Passwordabc" (sin número) → error

5. **Should reject mismatched passwords**
   - password: "Secure123"
   - confirmPassword: "Secure124"
   - Error: "Passwords do not match"

6. **Should reject short displayName**
   - displayName < 2 caracteres → error

7. **Should reject long displayName**
   - displayName > 50 caracteres → error

8. **Should not submit invalid form**
   - Llenar con datos inválidos + submit
   - Verificar que api.post NO fue llamada

#### Submission (6 tests)
1. **Should submit valid form**
   - Todos los campos válidos + submit
   - Verificar que api.post fue llamada con POST /auth/register

2. **Should pass correct data to API**
   - Request body debe contener: { email, password, displayName }
   - No debe incluir confirmPassword o campos extra

3. **Should save auth on success**
   - API retorna { token, user }
   - setAuth debe ser llamado con (token, user)

4. **Should redirect on success**
   - Después de éxito → router.push("/challenges")

5. **Should disable button while loading**
   - Durante esperanza del API → botón deshabilitado
   - Inputs también deshabilitados

6. **Should clear form on success**
   - Después de submit exitoso → inputs deben estar vacíos

#### Error Handling (5 tests)
1. **Should display server error on 400**
   - API retorna error 400 con mensaje
   - Banner de error debe mostrar el mensaje

2. **Should display error on 409 (conflict)**
   - Email ya está registrado → error específico
   - Mensaje: "This email is already registered"

3. **Should display generic error on 500**
   - Server error → mensaje genérico

4. **Should clear error on retry**
   - Mostrar error → cambiar input → error debe desaparecer
   - Submit nuevamente

5. **Should clear password on error**
   - Después de error → campos password y confirmPassword deben estar vacíos

#### Accessibility (4 tests)
1. **Should have proper form structure**
   - form element, fieldset (opcional), inputs con type correcto

2. **Should be keyboard navigable**
   - Tab entre email → password → confirmPassword → displayName → submit
   - Order correcto

3. **Should support screen readers**
   - ARIA labels, role="form", error messages con aria-live

4. **Should have sufficient color contrast**
   - Verificar que labels, inputs, botones sean legibles

### RegisterPage Tests

#### Rendering (4 tests)
1. **Should render RegisterForm component**
   - Componente debe estar presente

2. **Should render page title**
   - "Sign Up" o "Create Account"

3. **Should render layout**
   - Gradient background
   - Centered content
   - Responsive padding

4. **Should render login link**
   - Link a /login page

#### Authentication Redirect (2 tests)
1. **Should redirect to /challenges if already authenticated**
   - isAuthenticated = true → router.push("/challenges")

2. **Should not redirect if not authenticated**
   - isAuthenticated = false → stay on page
   - router.push should not be called

#### Layout & Styling (2 tests)
1. **Should have responsive design**
   - Mobile, tablet, desktop breakpoints
   - Tailwind classes aplicadas

2. **Should have proper semantics**
   - main, article, section tags si aplica

---

## Escenarios de Test Esperados

| # | Categoría | Comportamiento | Entrada | Salida Esperada |
|----|-----------|---|---|---|
| 1 | Rendering | Form rendered | N/A | 5 inputs + 1 button + link visible |
| 2 | Rendering | Labels accesibles | N/A | Todos inputs tienen labels con htmlFor |
| 3 | Validation | Email requerido | submit() | Error: "Email required" |
| 4 | Validation | Email inválido | "notanemail" → submit | Error: "Invalid email format" |
| 5 | Validation | Password corta | "Pass1" → submit | Error: "Password too short" |
| 6 | Validation | Password débil (no mayús) | "password123" → submit | Error |
| 7 | Validation | Passwords no coinciden | pwd1="Secure1" pwd2="Different1" | Error: "Passwords must match" |
| 8 | Validation | DisplayName corto | "A" → submit | Error: "Name too short" |
| 9 | Submission | Form válida | Todos campos ok → submit | api.post("/auth/register", {...}) |
| 10 | Submission | API success | API retorna token+user | setAuth llamado + redirect /challenges |
| 11 | Submission | Botón deshabilitado | Durante loading | disabled=true |
| 12 | ErrorHandling | Email ya existe | API 409 conflict | Error: "Email already registered" |
| 13 | ErrorHandling | Server error | API 500 | Error: "Server error" |
| 14 | ErrorHandling | Clear on retry | Error → cambiar input → submit | Error desaparece |
| 15 | Accessibility | Keyboard nav | Tab keys | Orden correcto entre inputs |
| 16 | PageRedirect | Already auth | isAuthenticated=true | Redirect a /challenges |
| 17 | PageLayout | Responsive | Mobile/tablet/desktop | Adapta correctamente |

---

## Componentes y Archivos

### RegisterForm.tsx (Existente, aplicar testing)
```
frontend/src/components/RegisterForm.tsx
- 'use client' directive
- useState for form state
- useRouter for redirect
- useAuth for setAuth
- Form validation logic
- API call to /auth/register
- Error messages display
- Loading state
```

### register/page.tsx (Existente, aplicar testing)
```
frontend/src/app/register/page.tsx
- export const metadata
- useRouter
- useAuth with isAuthenticated
- useEffect for redirect
- RegisterForm component
- Layout with gradient background
- Link to /login
```

### RegisterForm.test.tsx (NUEVO)
```
frontend/src/components/RegisterForm.test.tsx
- 20 tests siguiendo patrón: {Behavior}_Given{Condition}_Should{Result}
- Mock useRouter, useAuth, api
- Usar React Testing Library + userEvent
- Assertions nativas (no FluentAssertions)
- Coverage: rendering, validation, submission, errors, accessibility
```

### register/page.test.tsx (NUEVO)
```
frontend/src/app/register/page.test.tsx
- 6 tests
- Mock dependencies (useRouter, useAuth)
- Test rendering, redirect logic, layout
```

---

## Dependencias

- **Backend endpoint**: ✅ POST `/api/v1/auth/register` (ya existe)
- **AuthContext**: ✅ useAuth hook (ya existe, Phase 4.1)
- **ApiClient**: ✅ api module con interceptores (ya existe, Phase 4.1)
- **Testing library**: ✅ Jest + React Testing Library (configurado en Phase 4.2.1)

---

## Criterios de Éxito

- ✅ 26/26 tests en verde (20 component + 6 page)
- ✅ npm test --passWithNoTests ejecuta sin errores
- ✅ Validación client-side funciona
- ✅ API integration calls POST /auth/register
- ✅ Redirect a /challenges after success
- ✅ Error messages descriptivos
- ✅ Accesibilidad (labels, keyboard, ARIA)
- ✅ Código commitido a main branch

---

## Notas

- La funcionalidad ya existe en componentes; esta spec **formaliza SDD+TDD** con tests
- Los tests deben seguir el mismo patrón que Phase 4.2.1 (LoginForm.test.tsx)
- Usar patrón de nombre: `{Comportamiento}_Given{Condicion}_Should{Resultado}`
- No modificar componentes existentes a menos que se encuentren bugs; enfoque: agregar tests


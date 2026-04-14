# Spec: Phase 4.2.1 — Login Page

**Tipo**: Frontend Page (Next.js + React)  
**Ubicación**: `frontend/src/app/login/page.tsx`  
**Versión**: 1.0  
**Componentes**: LoginForm.tsx (refactorizado), LoginPage component  

---

## Qué es

Página de login para DevBrain Trainer que permite a los usuarios autenticarse con email y contraseña. Integra con `AuthContext` y `ApiClient` para:
- Validación de credenciales contra backend (`POST /api/v1/auth/login`)
- Gestión segura del JWT (almacenamiento + recuperación)
- Redirección automática post-login a `/challenges`
- Manejo de errores específicos (email inválido, contraseña incorrecta, servidor no disponible)

---

## Contrato HTTP

### Backend Endpoint: POST /api/v1/auth/login ✅ (ya existe)

**Request**:
```json
{
  "email": "string",
  "password": "string"
}
```

**Response (200 OK)**:
```json
{
  "token": "eyJhbGc...",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "displayName": "User Name"
  }
}
```

**Response (400 Bad Request)**:
```json
{
  "error": "Invalid email or password"
}
```

**Response (500 Server Error)**:
```json
{
  "error": "Server error"
}
```

---

## Comportamientos funcionales

### 1. Rendering inicial

- Página accesible en ruta `/login`
- Si usuario ya está autenticado (`AuthContext.isAuthenticated = true`):
  - Redirige automáticamente a `/challenges` usando `useRouter().push()`
  - No muestra el formulario de login
- Si usuario NO está autenticado:
  - Muestra formulario de login con:
    - Campo email (type="email")
    - Campo password (type="password")
    - Botón "Sign In" (deshabilitado mientras se procesa)
    - Link "Don't have an account? Register here" → `/register`
  - Estilos: Tailwind CSS, layout centrado vertically/horizontally, responsive (mobile-first)

### 2. Validación del formulario (client-side)

Antes de enviar al backend:
- **Email**: No vacío + válido (regex simple: `^[^\s@]+@[^\s@]+\.[^\s@]+$`)
- **Password**: No vacío, mínimo 3 caracteres (no validar contra policy; el backend lo hace)
- Si validation falla: mostrar error inline (color rojo, ícono ✗)

### 3. Envío de credenciales

Al hacer click en "Sign In":
1. Deshabilitar botón + mostrar loading spinner
2. Llamar a `ApiClient.post("/auth/login", { email, password })`
3. Si éxito (200):
   - Almacenar JWT: `AuthContext.login(response.token, response.user)`
   - Redirigir a `/challenges`
4. Si error (400):
   - Mostrar mensaje de error genérico: "Invalid email or password"
   - Limpiar campo password
   - Mantener email visible para reintentar
   - Re-habilitar botón
5. Si error (500 o timeout):
   - Mostrar: "Server error. Try again later."
   - Re-habilitar botón

### 4. Manejo de errores

| Error | Mensaje en UI | Acción |
|-------|---------------|--------|
| Email vacío | Red border + "Email required" | Foca en campo email |
| Email inválido | Red border + "Invalid email format" | Foca en campo email |
| Password vacío | Red border + "Password required" | Foca en campo password |
| 400 Invalid creds | Banner rojo: "Invalid email or password" | N/A |
| 500/timeout | Banner rojo: "Server error. Try again later." | Retry button |
| Network error | Banner rojo: "No internet connection" | Retry button |

### 5. Accesibilidad

- Labels `<label htmlFor="email">` y `<label htmlFor="password">`
- ARIA roles correctos
- Botón con `disabled` state
- Error messages con `role="alert"`
- Loading spinner con `aria-label="Signing in"`

---

## Invariantes (reglas que nunca se rompen)

1. **JWT siempre en localStorage**: Si login exitoso, `localStorage.setItem('devbrain_token', token)`
2. **User data en AuthContext**: Tras login exitoso, `AuthContext.user` contiene { id, email, displayName }
3. **Redirección post-login**: Nunca quedarse en `/login` tras login exitoso
4. **No guardar password**: Password NUNCA se almacena localmente (solo en tránsito a backend)
5. **Validación server-side primero**: Client-side validation es UX, pero servidor siempre valida

---

## Componentes a crear/refactorizar

### 1. `src/components/LoginForm.tsx` (nuevo/refactorizado)

**Responsabilidades**:
- Form markup + Tailwind styles
- Client-side validation
- State del formulario (email, password, loading, error)
- Integración con `ApiClient` y `AuthContext`

**Props**:
```typescript
interface LoginFormProps {
  onSuccess?: () => void;  // Callback post-login (opcional)
}
```

**State interno**:
```typescript
const [email, setEmail] = useState('');
const [password, setPassword] = useState('');
const [loading, setLoading] = useState(false);
const [error, setError] = useState<string | null>(null);
```

**Métodos**:
- `validate()` — retorna true/false y setta errores inline
- `handleSubmit()` — llama backend, maneja respuesta/error
- `handleEmailChange(), handlePasswordChange()` — actualiza state + limpia errores anteriores

### 2. `src/app/login/page.tsx` (nuevo)

**Responsabilidades**:
- Verificar autenticación (si ya logged in → redirect)
- Renderizar `LoginForm`
- Link a register page
- Layout general

**Estructura**:
```typescript
export default function LoginPage() {
  const router = useRouter();
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    if (isAuthenticated) {
      router.push('/challenges');
    }
  }, [isAuthenticated, router]);

  return (
    <div className="flex items-center justify-center min-h-screen bg-gradient-to-br from-slate-900 to-slate-800">
      <div className="w-full max-w-md p-8 bg-slate-800 rounded-lg shadow-lg border border-slate-700">
        <h1 className="text-2xl font-bold text-white mb-6">DevBrain Trainer</h1>
        <LoginForm />
        <p className="text-center text-sm text-slate-400 mt-6">
          Don't have an account?{' '}
          <Link href="/register" className="text-blue-400 hover:underline">
            Register here
          </Link>
        </p>
      </div>
    </div>
  );
}
```

---

## Tests a crear

**Ubicación**: `frontend/src/app/login` (o `__tests__/login` según estructura)

### 1. Rendering Tests
- ✅ Page renders loginform component
- ✅ Page shows "Sign In" button
- ✅ Page shows "Register here" link
- ✅ Page redirects if already authenticated

### 2. Form Validation Tests
- ✅ Email field shows error if empty
- ✅ Email field shows error if invalid format
- ✅ Password field shows error if empty
- ✅ Errors clear when user corrects input

### 3. Form Submission Tests
- ✅ Form submits with valid credentials
- ✅ Button disables while loading
- ✅ Success: JWT stored in localStorage
- ✅ Success: User info in AuthContext
- ✅ Success: Router redirects to /challenges

### 4. Error Handling Tests
- ✅ Invalid credentials (400) shows error message
- ✅ Server error (500) shows retry message
- ✅ Network error shows offline message
- ✅ Error message clears on retry

### 5. Accessibility Tests
- ✅ Labels have htmlFor attributes
- ✅ Error messages have role="alert"
- ✅ Loading spinner has aria-label

---

## Estilo y diseño

- **Color scheme**: Basado en `globals.css` y Tailwind config del proyecto
- **Base colors**: slate-900 (fondo), slate-800 (card), slate-400 (texto gris)
- **Accent colors**: blue-500 (botones), red-500 (errores)
- **Responsive**:
  - Mobile: full-width con padding
  - Tablet/Desktop: centrado, max-width: 400px
- **Dark mode**: Por defecto (no requiere toggle)

---

## Qué NO es esta página

- ❌ No tiene forgot password flow (futuro Phase 4.X)
- ❌ No tiene 2FA (futuro Phase X)
- ❌ No tiene social login (Google, GitHub — futuro Phase X)
- ❌ No valida contra políticas complejas de password (lo hace backend)

---

## Integración con sistemas existentes

### 1. `AuthContext` (`src/components/AuthContext.tsx`)
- Lee `isAuthenticated` para detectar redirección
- Llama `login(token, user)` tras credenciales válidas

### 2. `ApiClient` (`src/lib/api.ts`)
- Usa `POST /api/v1/auth/login` vía `apiClient.post()`
- Automáticamente agrega JWT a headers en requests futuras

### 3. Router (`next/router`)
- `push('/challenges')` post-login exitoso
- `push('/register')` en link "Register"

---

## Aceptación

✅ Spec listado para implementación cuando se ejecute `spec-implement`

**Próximo paso**: Ejecutar skill `spec-implement` para generar:
1. LoginForm.tsx (componente)
2. page.tsx (página)
3. page.test.tsx (tests)

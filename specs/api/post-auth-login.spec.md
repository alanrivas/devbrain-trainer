# Spec: POST /auth/login

**Tipo**: Endpoint de API  
**Ubicación**: `DevBrain.Api/Endpoints/AuthEndpoints.cs`  
**Dependencias**: `IUserRepository`, `IPasswordHashService`, `IJwtTokenService`  
**Versión**: 1.0

---

## Qué es

El endpoint `POST /auth/login` permite que usuarios registrados se autentiquen enviando su email y contraseña. Retorna un JWT token para usarlo en requests posteriores protegidos, más los datos básicos del usuario.

---

## Contrato HTTP

### Request

```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123"
}
```

**Estructura `LoginRequestDto`**:
| Campo | Tipo | Reglas |
|-------|------|--------|
| `email` | `string` | Obligatorio, formato email válido (mismo validador que registro) |
| `password` | `string` | Obligatorio, 1+ caracteres (sin validar longitud/complejidad, solo si existe en BD) |

### Response (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "displayName": "John Doe"
  }
}
```

**Estructura `LoginResponseDto`**:
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `token` | `string` | JWT firmado con secret, incluye `userId` en claim, expira en 24h |
| `user` | `UserResponseDto` | ID, email, displayName (idéntico a POST /auth/register) |

### Error Responses

| Status | Escenario | Response |
|--------|-----------|----------|
| **400** | Email no es válido (ej: "notanemail") | `{ "error": "Email format is invalid" }` |
| **400** | Email vacío o password vacío | `{ "error": "Email and password are required" }` |
| **401** | Email no existe en BD | `{ "error": "Invalid email or password" }` |
| **401** | Password es incorrecto (no coincide con hash) | `{ "error": "Invalid email or password" }` |

---

## Comportamientos

### Flujo happy path

1. Cliente envía `{ "email": "user@example.com", "password": "SecurePass123" }`
2. Endpoint valida formato email (`using System.ComponentModel.DataAnnotations.EmailAddressAttribute`)
3. Busca usuario en BD por email (case-insensitive via `EFUserRepository.GetByEmailAsync()`)
4. Si no existe → retorna **401 "Invalid email or password"**
5. Si existe → verifica password contra hash con `IPasswordHashService.VerifyAsync(plaintext, hash)`
6. Si no coincide → retorna **401 "Invalid email or password"**
7. Si coincide → genera JWT token con:
   - Claim `sub` (subject) = `userId` (Guid como string)
   - Claim `email` = email del usuario
   - Expiration = ahora + 24 horas
   - Algoritmo: HS256 (HMAC SHA256)
   - Secret: desde appsettings.json `Jwt:Secret` (min 32 chars)
8. Retorna **200 OK** con token + user data

### Validación de email

- Mismo validador que POST /auth/register
- Case-insensitive en lookup

### Verificación de password

- Usa `IPasswordHashService.VerifyAsync(plaintext, storedHash)`
- Compara PBKDF2 hash de la entrada contra hash guardado en `User.PasswordHash`
- No introduce timing attacks (use constant-time comparison)
- No loguea el password en ningún lugar

### Token JWT

- **Header**: `{ "alg": "HS256", "typ": "JWT" }`
- **Payload**:
  ```json
  {
    "sub": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "exp": 1712793600,
    "iat": 1712707200
  }
  ```
- **Signature**: HMAC-SHA256(base64url(header) + "." + base64url(payload), secret)
- Usado en requests futuros: `Authorization: Bearer {token}`

---

## Invariantes (reglas que nunca se rompen)

1. **Fallo silencioso**: No revelar si email existe o password es incorrecto (ambos → 401 "Invalid email or password")
2. **No sin contraseña**: No se puede hacer login si el usuario no tiene password hash (edge case, muy raro)
3. **Token único por sesión**: Cada login genera nuevo token, anteriores siguen siendo válidos hasta expiration
4. **Email case-insensitive**: `User@Example.com` y `user@example.com` son el mismo usuario
5. **Password nunca se loguea**: Logs y exceptions nunca contienen plaintext password

---

## Test Scenarios (11 tests totales)

### Validación de entrada (4 tests)

1. **LoginWithMissingEmail_ShouldReturn400**
   - Request: `{ "password": "Pass123" }`
   - Expected: 400, error = "Email and password are required"

2. **LoginWithMissingPassword_ShouldReturn400**
   - Request: `{ "email": "user@example.com" }`
   - Expected: 400, error = "Email and password are required"

3. **LoginWithInvalidEmailFormat_ShouldReturn400**
   - Request: `{ "email": "notanemail", "password": "Pass123" }`
   - Expected: 400, error = "Email format is invalid"

4. **LoginWithEmptyBothFields_ShouldReturn400**
   - Request: `{ "email": "", "password": "" }`
   - Expected: 400, error = "Email and password are required"

### Autenticación (4 tests)

5. **LoginWithNonexistentEmail_ShouldReturn401**
   - Setup: No user con ese email en BD
   - Request: `{ "email": "nonexistent@example.com", "password": "Pass123" }`
   - Expected: 401, error = "Invalid email or password", sin revelar que no existe

6. **LoginWithIncorrectPassword_ShouldReturn401**
   - Setup: User con email "user@example.com" y password hash de "CorrectPass123"
   - Request: `{ "email": "user@example.com", "password": "WrongPass123" }`
   - Expected: 401, error = "Invalid email or password"

7. **LoginWithCorrectCredentials_ShouldReturn200WithToken**
   - Setup: User registrado: email "john@example.com", password "SecurePass123", displayName "John"
   - Request: `{ "email": "john@example.com", "password": "SecurePass123" }`
   - Expected: 200 OK
   - Response: 
     - `token` presente, válido, decodificable
     - `user.id` = userId correcto (Guid)
     - `user.email` = "john@example.com"
     - `user.displayName` = "John"
     - Token contiene claim `sub` con userId

8. **LoginWithCaseInsensitiveEmail_ShouldReturn200**
   - Setup: User con email "User@Example.com"
   - Request: `{ "email": "user@example.com", "password": "SecurePass123" }`
   - Expected: 200 OK (case-insensitive match)

### Token validación (3 tests)

9. **LoginTokenHasCorrectExpiration_ShouldBe24Hours**
   - Setup: Login exitoso a las 10:00 AM
   - Expected: Token exp claim = 10:00 AM + 24h (±1 min tolerance)

10. **LoginTokenCanBeDecoded_ShouldHaveValidClaims**
    - Setup: Login exitoso
    - Expected: Token decodificable, contiene `sub`, `email`, `exp`, `iat`

11. **LoginTokenWithValidAuthHeader_ShouldAuthenticateUser**
    - Setup: Login exitoso, obtener token
    - Expected: Token puede ser usado en Authorization header para requests futuros

---

## Qué NO es esta entidad

- **No es "remember me"** — Si expira el token, usuario debe loguear de nuevo (no cookies persistentes por ahora)
- **No es OAuth/OIDC** — Autenticación local, no delegada a proveedores externos (Supabase vendrá después)
- **No es 2FA** — Sin factor doble (puede venir post-MVP)
- **No verifica email confirmado** — Se asume que si está registrado, email es válido

---

## Dependencias

**Servicios necesarios**:
- `IUserRepository.GetByEmailAsync(email)` — retorna `User` o `null`
- `IPasswordHashService.VerifyAsync(plaintext, hash)` — retorna `bool`
- `IJwtTokenService` (nuevo) — genera JWT tokens con claims y expiration

**DTOs**:
- `LoginRequestDto` — input validation
- `LoginResponseDto` + `UserResponseDto` (reutilizar de /auth/register)

**Config (appsettings.json)**:
```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-characters-long",
    "ExpirationHours": 24
  }
}
```

---

## Aceptación

Tests pasan ✅ cuando:
- Validación de entrada rechaza datos inválidos (400)
- Email inexistente retorna 401 sin revelar existencia
- Password incorrecto retorna 401 sin revelar error específico
- Credenciales correctas retornan 200 + token válido + user data
- Token es válido JWT con claims correctos
- Email es case-insensitive en lookup

# Spec: POST /auth/register — User Registration

**Tipo**: Endpoint API  
**Ubicación**: `DevBrain.Api`  
**Versión**: 1.0  

---

## Qué es

Endpoint para registrar un nuevo usuario en la plataforma. Valida credenciales, crea la entidad `User` en Domain, persiste en base de datos, y retorna el perfil del usuario creado con status 201 Created.

Este es el **punto de entrada** para nuevos usuarios a la plataforma. Sin registro exitoso, el usuario no puede crear intentos ni medir su progreso.

---

## Contract

### Route
- **Method**: `POST`
- **Path**: `/api/v1/auth/register`
- **Content-Type**: `application/json`
- **Authentication**: None (anyone can register)
- **Returns**: `201 Created`

### Request Body

```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "displayName": "John Developer"
}
```

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `email` | string | Yes | Valid email format, unique across system | User's email address for login |
| `password` | string | Yes | Min 8 chars, min 1 uppercase, min 1 number | Hashed with bcrypt, never stored plaintext |
| `displayName` | string | Yes | 3-50 chars | Public name shown in leaderboards |

### Response (201 Created)

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "displayName": "John Developer",
  "createdAt": "2026-04-09T12:00:00Z"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `userId` | UUID | Server-generated, immutable |
| `email` | string | Lowercased email (canonical form) |
| `displayName` | string | As provided by user |
| `createdAt` | ISO 8601 DateTime | Server timestamp in UTC |

### Error Responses

| Status | Code | Scenario |
|--------|------|----------|
| `400` | `BadRequest` | Email invalid format (not `name@domain.ext`) |
| `400` | `BadRequest` | Password too short (< 8 chars) |
| `400` | `BadRequest` | Password has no uppercase letter |
| `400` | `BadRequest` | Password has no digit |
| `400` | `BadRequest` | DisplayName too short (< 3 chars) |
| `400` | `BadRequest` | DisplayName too long (> 50 chars) |
| `400` | `BadRequest` | DisplayName contains invalid characters (not alphanumeric + spaces) |
| `409` | `Conflict` | Email already registered in system (case-insensitive) |

**Error Response Format**:
```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "Password must contain at least one uppercase letter"
}
```

---

## Comportamientos

### Creación de usuario

- Endpoint recibe JSON con email, password, displayName
- Email se normaliza a minúscula (canonical form) — `John@EXAMPLE.com` → `john@example.com`
- Password se valida según reglas de seguridad
- Usuario se crea vía `User.Create(email, password, displayName)` factory en Domain
- Usuario se persiste en base de datos vía `IUserRepository.AddAsync(user)`
- Retorna 201 Created con UserResponseDto

### Validaciones de Email

1. Formato válido (debe contener `@` y `.`) — regex: `^[^@\s]+@[^@\s]+\.[^@\s]+$`
2. Longitud máxima 255 caracteres
3. Unicidad: no puede existir otro usuario con mismo email (case-insensitive check)

### Validaciones de Password

1. Longitud mínima: 8 caracteres
2. Debe contener al menos 1 letra mayúscula (A-Z)
3. Debe contener al menos 1 dígito (0-9)
4. Caracteres permitidos: letras, dígitos, y symbols especiales `!@#$%^&*`
5. **Nunca** se almacena en plaintext — se hashea con bcrypt antes de persistencia

### Validaciones de DisplayName

1. Longitud mínima: 3 caracteres
2. Longitud máxima: 50 caracteres
3. Caracteres permitidos: letras, dígitos, espacios, guion (`-`), punto (`.`)
4. Se trimea (elimina espacios al inicio/final)

### Duplicado Detection

- Búsqueda en DB es **case-insensitive** para email
- Ejemplo: `john@example.com` y `JOHN@EXAMPLE.COM` son consideradas duplicadas
- Retorna 409 Conflict sin exponer qué email ya existe (por seguridad)

---

## Invariantes (reglas que nunca se rompen)

1. **Cada usuario tiene email único** — No pueden existir dos usuarios con mismo email (case-insensitive)
2. **UserId es generado por servidor** — Cliente NO puede especificar userId
3. **Password nunca se almacena plaintext** — Siempre hasheado con bcrypt
4. **Email siempre en minúscula en DB** — Canonical form is lowercase
5. **DisplayName nunca vacío o solo espacios** — Min 3 chars después de trim
6. **CreatedAt siempre en UTC** — Timestamp inmutable del lado servidor
7. **Usuario creado = usuario registrado** — No hay paso adicional de validación (email pre-validado)

---

## Test Scenarios (13 expected)

1. ✅ **Happy Path — Valid Request**  
   - Request: valid email, strong password, valid displayName  
   - Expected: 201, complete UserResponseDto with userId, email (lowercase), displayName, createdAt

2. ✅ **Invalid Email Format**  
   - Request: `email: "not-an-email"`  
   - Expected: 400 BadRequest with detail about email format

3. ✅ **Email Missing @ Sign**  
   - Request: `email: "userexample.com"`  
   - Expected: 400 BadRequest

4. ✅ **Email Missing Domain**  
   - Request: `email: "user@"`  
   - Expected: 400 BadRequest

5. ✅ **Password Too Short**  
   - Request: `email: "user@example.com", password: "Pass1"`  
   - Expected: 400 BadRequest (must be 8+ chars)

6. ✅ **Password No Uppercase Letter**  
   - Request: `password: "password123"`  
   - Expected: 400 BadRequest

7. ✅ **Password No Digit**  
   - Request: `password: "Password"`  
   - Expected: 400 BadRequest

8. ✅ **DisplayName Too Short**  
   - Request: `displayName: "ab"`  
   - Expected: 400 BadRequest (min 3 chars)

9. ✅ **DisplayName Too Long**  
   - Request: `displayName: "a".repeat(51)`  
   - Expected: 400 BadRequest (max 50 chars)

10. ✅ **DisplayName Invalid Characters**  
    - Request: `displayName: "User@Name!"`  
    - Expected: 400 BadRequest

11. ✅ **Duplicate Email (Case-Insensitive)**  
    - Scenario: User registered as `john@example.com`, second attempt with `JOHN@EXAMPLE.COM`  
    - Expected: 409 Conflict

12. ✅ **Email Normalized to Lowercase**  
    - Request: `email: "John.Doe@EXAMPLE.COM"`  
    - Result: persisted and returned as `john.doe@example.com`  
    - Expected: email field in response is lowercase

13. ✅ **DisplayName Trimmed**  
    - Request: `displayName: "  John Developer  "`  
    - Result: stored and returned as `"John Developer"` (spaces trimmed)  
    - Expected: response shows trimmed value

---

## Qué NO es esta entidad

- **No es login** — Registración es crear cuenta nueva, login es autenticación con contraseña existente
- **No es confirmación de email** — Registración es inmediata, sin pasos adicionales
- **No es integración con Supabase** — UserId es generado por nuestro sistema (Guid), no por Supabase
- **No es actualización de perfil** — Registración es crear usuario, después hay PATCH /user/profile
- **No gestiona JWT tokens** — Registración solo persiste usuario, login genera tokens

---

## Relaciones con otras entidades

### Domain: `User` Entity
- Endpoint utiliza `User.Create(email, password, displayName)` factory
- Factory valida y hashea password
- Domain garantiza invariantes

### Infrastructure: `IUserRepository`
- `AddAsync(user)` persiste en DB
- `GetByEmailAsync(email)` para validar duplicados (caso-insensitive)

### API: Response DTOs
- `RegisterRequestDto` — deserializa JSON request
- `UserResponseDto` — serializa User entity para response

---

## Mapeos

### Request → Domain
```
RegisterRequestDto.Email → User.Email (lowercase)
RegisterRequestDto.Password → User.PasswordHash (bcrypt)
RegisterRequestDto.DisplayName → User.DisplayName (trimmed)
```

### Domain → Response
```
User.Id → UserResponseDto.UserId
User.Email → UserResponseDto.Email (already lowercase)
User.DisplayName → UserResponseDto.DisplayName
User.CreatedAt → UserResponseDto.CreatedAt (ISO 8601 UTC)
```

---

## Implementation Notes

### Password Hashing
- Use bcrypt library (System.Linq.Expressions.Compiler or BCrypt.Net-Next NuGet)
- Cost factor: 12 (standard for 2026)
- Never log passwords, use [Redacted] in audit logs

### Email Validation
- Use regex or built-in EmailAddressAttribute
- Perform database uniqueness check AFTER format validation
- Case-insensitive comparison in DB

### Error Messages
- **Be specific** about validation errors (which field, why)
- **Don't expose** whether email exists in system (409 without detail)
- Use ProblemDetails RFC format for consistency

### Idempotency
- **NOT idempotent** — calling endpoint twice creates two users
- If duplicate email, second call returns 409 Conflict (client detects)

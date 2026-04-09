# Spec: JWT Authentication Middleware

**Tipo**: Middleware / Configuración de API  
**Ubicación**: `DevBrain.Api` (`Program.cs` + endpoints afectados)  
**Dependencias**: `IJwtTokenService`, `Microsoft.AspNetCore.Authentication.JwtBearer`  
**Versión**: 1.0

---

## Qué es

Middleware de autenticación JWT que protege endpoints que requieren identidad de usuario. Usa el pipeline nativo de ASP.NET Core (`UseAuthentication` + `UseAuthorization`) con el scheme `JwtBearer`, y la misma configuración de firma y validación que `JwtTokenService`.

Los endpoints protegidos rechazan requests sin token válido con `401 Unauthorized`. Los endpoints públicos no se ven afectados.

---

## Endpoints protegidos (requieren `Authorization: Bearer <token>`)

| Endpoint | Razón |
|----------|-------|
| `POST /api/v1/challenges/{id}/attempt` | Necesita saber qué usuario envía la respuesta |

## Endpoints públicos (sin cambio)

| Endpoint | Razón |
|----------|-------|
| `GET /api/v1/challenges` | Lectura pública |
| `GET /api/v1/challenges/{id}` | Lectura pública |
| `POST /api/v1/auth/register` | Flujo de alta |
| `POST /api/v1/auth/login` | Flujo de auth |

---

## Configuración en `Program.cs`

```csharp
// AddAuthentication + AddJwtBearer con mismos parámetros que JwtTokenService.ValidateToken
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// Después de builder.Build():
app.UseAuthentication();
app.UseAuthorization();
```

Los parámetros de validación son idénticos a los de `JwtTokenService.ValidateToken` para garantizar consistencia.

---

## Protección de endpoints

Los endpoints protegidos se marcan con `.RequireAuthorization()` en su registro minimal API:

```csharp
group.MapPost("/{id}/attempt", PostAttempt)
    .RequireAuthorization();
```

---

## Claims disponibles en endpoints protegidos

Una vez autenticado, el `HttpContext.User` contiene:

| Claim | Valor |
|-------|-------|
| `ClaimTypes.NameIdentifier` / `sub` | Guid del usuario (string) |
| `ClaimTypes.Email` / `email` | Email del usuario |

Para obtener el userId en el endpoint:

```csharp
var userId = Guid.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
```

---

## Comportamientos

### Request sin token

- El cliente no envía `Authorization` header
- ASP.NET Core retorna **401 Unauthorized**
- Body: vacío (comportamiento por defecto de JwtBearer)
- El endpoint handler **no se ejecuta**

### Request con token malformado

- Header: `Authorization: Bearer no-es-un-jwt`
- ASP.NET Core retorna **401 Unauthorized**
- El endpoint handler no se ejecuta

### Request con token expirado

- Header: `Authorization: Bearer <token-expirado>`
- ASP.NET Core retorna **401 Unauthorized** (porque `ClockSkew = TimeSpan.Zero`)
- El endpoint handler no se ejecuta

### Request con token válido

- Header: `Authorization: Bearer <token-valido>`
- ASP.NET Core valida firma + expiración
- Popula `HttpContext.User` con los claims del token
- El endpoint handler se ejecuta normalmente con identidad disponible

### Request con token firmado con secret diferente

- Header: `Authorization: Bearer <token-firma-invalida>`
- ASP.NET Core retorna **401 Unauthorized**
- El endpoint handler no se ejecuta

---

## Invariantes

1. Los parámetros de `TokenValidationParameters` en el middleware son idénticos a los de `JwtTokenService.ValidateToken` — misma firma, misma lógica
2. Endpoints públicos no se ven afectados: `GET /challenges`, `GET /challenges/{id}`, `POST /auth/register`, `POST /auth/login` siguen respondiendo sin token
3. El middleware **no genera tokens** — eso sigue siendo responsabilidad de `POST /auth/login`
4. El claim `sub` siempre contiene el `Guid` del usuario como string parseable
5. `ClockSkew = TimeSpan.Zero` — tokens expirados son rechazados inmediatamente, sin margen

---

## Qué NO es este middleware

- No implementa refresh tokens (post-MVP)
- No implementa roles ni permisos (solo autenticación, no autorización de roles)
- No blacklistea tokens (sin revocación — el token es válido hasta que expira)
- No implementa rate limiting por usuario
- No gestiona sesiones (stateless — el servidor no almacena estado)

---

## Escenarios de test esperados (9 tests)

### Endpoints públicos no afectados (2 tests)

| Escenario | Resultado |
|-----------|-----------|
| `GET /challenges` sin token | 200 OK — endpoint público no afectado |
| `GET /challenges/{id}` sin token | 200 OK — endpoint público no afectado |

### Endpoint protegido — sin token (2 tests)

| Escenario | Resultado |
|-----------|-----------|
| `POST /challenges/{id}/attempt` sin `Authorization` header | 401 Unauthorized |
| `POST /challenges/{id}/attempt` con header vacío `Authorization: Bearer ` | 401 Unauthorized |

### Endpoint protegido — token inválido (3 tests)

| Escenario | Resultado |
|-----------|-----------|
| Token malformado (no es JWT) | 401 Unauthorized |
| Token con firma inválida (secret diferente) | 401 Unauthorized |
| Token expirado (exp en el pasado) | 401 Unauthorized |

### Endpoint protegido — token válido (2 tests)

| Escenario | Resultado |
|-----------|-----------|
| Token válido obtenido de `POST /auth/login` | El endpoint se ejecuta (200 o la respuesta del handler) |
| Claims del token disponibles en `HttpContext.User` | `sub` y `email` presentes y correctos |

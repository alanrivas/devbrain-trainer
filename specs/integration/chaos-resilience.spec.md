# Spec: Chaos/Resilience Integration Tests

**Tipo**: Pruebas de integración (comportamiento resiliente ante fallos de servicios externos)  
**Ubicación**: `DevBrain.Api.Tests`, `DevBrain.Integration.Tests`  
**Versión**: 1.0  

---

## Qué es

Suite de tests que validan que DevBrain Trainer se comporta de forma resiliente y graceful cuando servicios externos (Redis, PostgreSQL, etc.) fallan, están lentos o tienen problemas.

El propósito es:
- **No crashear**: La app no debe morir si Redis/PostgreSQL no responden
- **Fallar gracefully**: Endpoints devuelven errores informativos (4xx/5xx) en lugar de excepciones sin manejar
- **Recuperación**: Cuando el servicio se recupera, la app vuelve a funcionar
- **Observabilidad**: Eventos de fallo se loguean claramente para debugging

---

## Escenarios de caos a probar

### 1. Redis no disponible (Streak service down)

**Setup**: Iniciar app sin que Redis esté corriendo o desconectar Redis mid-request

**Comportamiento esperado**:
- `POST /api/v1/challenges/{id}/attempt` → `500 Internal Server Error` con mensaje claro
- Attempt se crea en DB (PostgreSQL funciona)
- ELO se actualiza correctamente
- **Streak falla** pero no bloquea el flujo (es un "nice to have", no crítico)
- Log: `⚠️ [Warning] Streak service unavailable. Proceeding without streak.`

**No debe pasar**:
- ❌ App crash/exception no capturada
- ❌ Attempt perdido
- ❌ Usuario no recibe respuesta HTTP

### 2. PostgreSQL lento (latencia artificial 5s+)

**Setup**: Inyectar latencia en PostgreSQL (TestContainers delay, network throttling, etc.)

**Comportamiento esperado**:
- `GET /api/v1/challenges` demora 5+ segundos pero devuelve `200 OK` con todos los resultados
- `POST /api/v1/challenges/{id}/attempt` demora 5+ segundos pero se completa sin timeout
- Timeout en client (opcional): si configuramos timeout de 10s en HttpClient, no debe dispararse (las queries son < 10s)

**Configuración**:
- No cambiamos `Program.cs` (sin timeout fijo en DbContext)
- El test induce latencia artificial solo en la capa DB

**No debe pasar**:
- ❌ Exception de timeout
- ❌ Request incompleto
- ❌ Data corrupción

### 3. JWT secret rotation entre requests

**Setup**: 
1. Request 1 con JWT válido basado en secret v1
2. Durante el procesamiento, rotamos el secret a v2
3. Request 2 con JWT antiguo basado en secret v1
4. Request 3 con JWT nuevo basado en secret v2

**Comportamiento esperado**:
- Request 1 → `200 OK` (JWT válido con secret v1)
- Request 2 → `401 Unauthorized` (JWT basado en secret v1 es inválido post-rotación)
- Request 3 → `200 OK` (JWT válido con secret v2)

**Nota**: La rotación es limitada (out of scope para MVP). Este test valida que JWT validation falla correctamente cuando el secret cambia.

**No debe pasar**:
- ❌ Request 1 o 3 rechazados
- ❌ Request 2 aceptado (security issue)

### 4. PostgreSQL no disponible (inicialmente)

**Setup**: Intentar que la app arrange sin PostgreSQL

**Comportamiento esperado**:
- App arranca correctamente (no crash)
- Endpoints que requieren DB devuelven `503 Service Unavailable` (no configured correctly)
- Endpoints que no requieren DB funcionan (ej: `/health`)
- Log: `❌ [Fatal] PostgreSQL connection failed. App will function with degraded capabilities.`

**Nota**: Esta es una prueba de robustez de startup, no de recuperación (no esperamos que la app se recupere sin intervención manual).

**No debe pasar**:
- ❌ App crash al arrancar
- ❌ Endpoints hanged/timeout indefinidamente

---

## Tests a escribir

### Tests de Redis no disponible

| Test Name | Input | Expected Output |
|---|---|---|
| `PostAttempt_RedisDown_StillCreatesAttempt_Returns500WithMessage` | POST /challenges/{id}/attempt sin Redis | 500 + message "Streak service unavailable" |
| `PostAttempt_RedisReconnects_StreakWorksAgain` | Redis vuelve a disponible después de error | Siguiente request success con streak |

### Tests de PostgreSQL lento

| Test Name | Input | Expected Output |
|---|---|---|
| `GetChallenges_WithPostgresDelayedResponse_ReturnsSuccessfullyAfterDelay` | GET /challenges con 5s latency en DB | 200 OK después de 5+ segundos |
| `PostAttempt_WithPostgresDelayedResponse_CompletesSuccessfully` | POST /attempt con 5s latency en DB | 200 OK after 5+ segundos, Attempt creado |

### Tests de JWT rotation

| Test Name | Input | Expected Output |
|---|---|---|
| `SecureEndpoint_WithValidJWT_Returns200` | POST con JWT válido | 200 OK |
| `SecureEndpoint_AfterSecretRotation_OldJWTFails_Returns401` | POST con JWT antiguo post-rotación | 401 Unauthorized |
| `SecureEndpoint_AfterSecretRotation_NewJWTWorks_Returns200` | POST con JWT nuevo post-rotación | 200 OK |

### Tests de PostgreSQL no disponible

| Test Name | Input | Expected Output |
|---|---|---|
| `AppStartup_WithoutPostgres_StartsWithoutCrash` | App arranque sin PostgreSQL | Success, but DB endpoints return 503 |
| `HealthEndpoint_WithoutPostgres_Returns200` | GET /health sin Database | 200 OK (health check no depende de DB) |
| `DbEndpoint_WithoutPostgres_Returns503` | GET /challenges sin Database | 503 Service Unavailable |

---

## Invariantes (reglas que nunca se rompen)

1. **App no crashea**: Ningún fallo externo debe causar unhandled exception
2. **Graceful degradation**: Si un servicio optional (Redis) falla, otros (DB) siguen funcionando
3. **Clear errors**: El cliente recibe HTTP status code significativo (500, 503, 401, etc.) + message explicativo
4. **Observabilidad**: Fallos se loguean con contexto (timestamp, servicio, error message)
5. **Data integrity**: Si PostgreSQL falla, datos no se corrompen; si Redis falla pero DB funciona, Attempt se crea anyway

---

## Qué NO es esta suite

- No es performance testing (no medimos response times)
- No es load testing (no hacemos thousands of requests)
- No es security audit (JWT rotation es basic test, no comprehensive rotation strategy)
- No es data migration testing
- No es frontend/UX testing

---

## Referencias

- TestContainers docs: https://testcontainers.com/
- xUnit Assert.Throws pattern: https://xunit.net/docs/getting-started/netfx
- HttpClient timeout: https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout
- JWT validation: https://docs.microsoft.com/en-us/dotnet/fundamentals/networking/http/overview
- PostgreSQL error codes: https://www.postgresql.org/docs/current/errcodes-appendix.html

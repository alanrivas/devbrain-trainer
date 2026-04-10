# Spec: Integración de Logging en Endpoints

**Tipo**: Comportamiento de API  
**Ubicación**: `DevBrain.Api`  
**Versión**: 1.0  
**Relacionado con**: `specs/infrastructure/serilog-logging.spec.md`

---

## Qué es

Integración de logging estructurado en todos los endpoints de la API mediante inyección de `ILogger<T>` en Controllers y Middleware. El propósito es registrar entrada/salida de solicitudes, errores y eventos relevantes del negocio para auditoría, diagnóstico y análisis de comportamiento de usuarios.

La infraestructura Serilog ya está configurada (Phase 3.2). Esta fase agrega las **llamadas reales** de logging en cada endpoint y middleware.

---

## Alcance

### Endpoints a logear (8 totales)

| Endpoint | Método | Ubicación | Eventos a logar |
|----------|--------|-----------|-----------------|
| `/api/v1/challenges` | GET | `ChallengesController` | Filtros aplicados, count total, duración |
| `/api/v1/challenges/{id}` | GET | `ChallengesController` | ID solicitado, found/not found |
| `/api/v1/challenges/{id}/attempt` | POST | `ChallengesController` | UserID, ChallengeID, resultado, ELO delta |
| `/api/v1/auth/register` | POST | `AuthController` | Email, displayName, success/duplicate error |
| `/api/v1/auth/login` | POST | `AuthController` | Email, success/auth fail, token generated |
| `/api/v1/users/me` | GET | `UsersController` | JWT validation, user found |
| `/api/v1/users/me/stats` | GET | `UsersController` | User ELO, accuracy, streak |
| `/api/v1/users/me/badges` | GET | `UsersController` | Badge count, types awarded |

### Middleware adicional

| Middleware | Ubicación | Eventos |
|-----------|-----------|---------|
| JWT Validation | `JwtMiddleware` | Token valid/expired/invalid, UserID extracted |
| Error Handling | Global exception handler | Exception type, message, stacktrace (en Development solo) |

---

## Comportamientos esperados

### 1. Inyección de ILogger<T> en Controllers

```
- Cada Controller debe tener:
  private readonly ILogger<NombreController> _logger;
  
  public NombreController(ILogger<NombreController> logger)
  {
      _logger = logger;
  }
```

**Invariante**: No usar `ILoggerFactory` ni `Log.ForContext()` directamente—siempre usar inyección en constructor.

### 2. Logging por tipo de evento

#### a) **Request Entry** (inicio de endpoint)

```
Log.Information(
    "GetChallenges called with filters: {@Filters}, Skip: {Skip}, Take: {Take}",
    filters,
    skip,
    take
);
```

**Estructura**:
- Nivel: `Information`
- Contexto: método + parámetros input
- Datos sensibles: **NUNCA** logar passwords, tokens, email completo

#### b) **Business Logic Event** (dentro de la lógica)

```
Log.Information(
    "Attempt created: ChallengeID={ChallengeID}, UserID={UserID}, IsCorrect={IsCorrect}, ELODelta={ELODelta}",
    attempt.ChallengeId,
    attempt.UserId,
    attempt.IsCorrect,
    eloDelta
);
```

#### c) **Response/Success**

```
Log.Information(
    "GetChallenges completed: {Count} challenges returned",
    challenges.Count()
);
```

#### d) **Error/Exception** (pertenece a global error handler)

```
Log.Error(
    exception,
    "Unexpected error in GetChallenges: {Message}",
    exception.Message
);
```

### 3. Contexto estructurado (LogContext)

Todos los endpoints deben enriquecer el contexto con datos del usuario actual:

```csharp
using (LogContext.PushProperty("UserId", userId))
using (LogContext.PushProperty("RequestId", HttpContext.TraceIdentifier))
{
    // Todas las llamadas a Log dentro de estos bloques
    // llevarán automáticamente UserId y RequestId
}
```

### 4. Niveles de log por nivel de negocio

| Evento | Nivel | Ejemplo |
|--------|-------|---------|
| Solicitud de endpoint | `Information` | "GetChallenges called" |
| Resultado del negocio | `Information` | "ELO updated: +25" |
| Warning (validación que falla) | `Warning` | "Challenge not found: ID={id}" |
| Error no manejado | `Error` | "Database connection failed" |
| Información de debug | `Debug` | "Query parameters parsed: {...}" |

---

## Invariantes (reglas que nunca se rompen)

1. **Sin passwords**: Nunca logear `password`, `passwordHash`, tokens JWT o credenciales
2. **Sin emails completos en logs de usuarios**: Logear `email.Split('@')[0]` o solo el ID del usuario
3. **Idempotencia de logs**: Si se llama el endpoint 2 veces, esperamos 2 registros idénticos en log (sin duplicación de líneas de código)
4. **LogContext siempre limpio**: Usar `using` para `PushProperty` de manera que se desapile automáticamente
5. **Sin bloat**: No logear el objeto completo `{@Challenge}` excepto en escenarios de debug/error—usar propiedades específicas
6. **Structured logging**: Usar structured logging (`{@variable}` para objetos, `{variable}` para escalares) en lugar de string concatenation
7. **Duración de request**: Registrar duración total del endpoint (middleware o controller logging)

---

## Tests esperados

### Patrón de test: `{Comportamiento}_Given{Condicion}_Should{Resultado}`

```
✅ GetChallengesLogging_GivenValidFilters_ShouldLogWithFilters
✅ PostAttemptLogging_GivenCorrectAnswer_ShouldLogELODelta
✅ PostAttemptLogging_GivenUserNotFound_ShouldLogWarning
✅ JwtMiddlewareLogging_GivenInvalidToken_ShouldLogUnauthorized
✅ RegisterLogging_GivenDuplicateEmail_ShouldLogWarning
✅ LoginLogging_GivenWrongPassword_ShouldLogLoginFailure
✅ GetUserStatsLogging_GivenValidUser_ShouldLogStats
✅ GetBadgesLogging_GivenUserWithBadges_ShouldLogBadgeCount
```

### Setup de tests

Usar `ITestSink` de Serilog para capturar logs en memoria durante tests:

```csharp
var testSink = new TestSink();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(testSink)
    .CreateLogger();

// Ejecutar endpoint
// Verificar: testSink.Writes.Count > 0
// Verificar: testSink.Writes[0].MessageTemplate.Text.Contains("expected text")
```

---

## Qué NO es esta fase

- No es cambiar la configuración de Serilog (ya hecho en Phase 3.2)
- No es refactorizar todos los métodos Repository para añadir logging (eso es future)
- No es agregar distributed tracing (future: Azure Application Insights correlation IDs)
- No es Performance optimization (logging debe ser async, pero eso es infraestructura Serilog)

---

## Fases subsecuentes

Tras esta fase 3.3:
- **Phase 3.4**: Dashboard real-time de logs en Azure Application Insights
- **Phase 3.5**: Auditoría de cambios (quién, cuándo, qué) en User/Challenge/Attempt
- **Phase 4.0**: Frontend Next.js (completar flujo de usuario)

---

## Verificación de completitud

Al terminar esta phase:

- [ ] Todos los 8 endpoints usan `ILogger<T>` vía inyección
- [ ] Cada endpoint tiene 2-4 llamadas `_logger.Information()`
- [ ] JwtMiddleware loguea tokens inválidos
- [ ] Global error handler loguea todas las excepciones
- [ ] Tests incluyen validación de logs vía TestSink
- [ ] 100% de tests siguen pasando (212+nuevos tests de logging)
- [ ] `context.md` actualizado con Phase 3.3 completada
- [ ] Push a GitHub completado

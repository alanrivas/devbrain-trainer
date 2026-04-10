# Spec: Serilog Structured Logging Infrastructure

**Tipo**: Infraestructura (Servicio de logging estructurado)  
**Ubicación**: `DevBrain.Api`, `DevBrain.Infrastructure`  
**Versión**: 1.0  

---

## Qué es

Sistema de logging estructurado basado en Serilog que captura eventos de aplicación en formato JSON, permitiendo búsqueda, análisis y debugging en producción (Azure Application Insights).

El logging proporciona:
- **Tracer de eventos críticos**: registro, login, intentos, errores
- **Contexto enriquecido**: usuario, IP, request ID, duración
- **Persistencia multi-sink**: console (dev), file (backup), Application Insights (prod)
- **Debugging productivo**: ver qué sucedió exactamente en producción sin acceso directo

---

## Objetivos

1. **Observabilidad en producción**: Detectar y debuggear issues sin reiniciar app
2. **Structured data**: JSON logs parseables automáticamente (no strings sin formato)
3. **Zero-breaking-changes**: Agregar logging sin cambiar lógica de negocio existente
4. **Azure-native**: Integración directa con Application Insights (portal.azure.com)

---

## Arquitectura

### Stack de tecnologías
- **Serilog v4.x**: Framework de logging estructurado
- **Serilog.AspNetCore**: Integración ASP.NET Core
- **Serilog.Sinks.Console**: Output a console (Dev + Railway)
- **Serilog.Sinks.File**: Persistencia local en `logs/` (backup 30 días)
- **Serilog.Sinks.ApplicationInsights**: Telemetría a Azure Application Insights
- **Serilog.Enrichers.Environment**: Agregar contexto de máquina (username, pthread, environment)

### Configuración por ambiente

| Ambiente | Comportamiento |
|----------|---|
| **Development** | Console (colorido) + File local |
| **Testing** | Silent (no ruido en test output) |
| **Production (Azure)** | Console (capturado por App Service) + Application Insights |

---

## Niveles de log (en orden de severidad)

| Nivel | Cuándo usar | Ejemplo |
|-------|---|---|
| **Verbose** | Eventos muy detallados (solo dev) | "Reading config key..." ← No usar por defecto |
| **Debug** | Información útil durante debugging | "Challenge loaded from cache" |
| **Information** | Eventos normales de negocio | "✅ User registered", "📝 Attempt submitted" |
| **Warning** | Algo inusual pero recuperable | "⚠️ Email duplicate", "Cache miss, querying DB" |
| **Error** | Fallo que afecta funcionalidad | "❌ Password validation failed", "DB timeout" |
| **Fatal** | App no puede funcionar | "❌ PostgreSQL offline", "Redis connection lost" |

**Configuración por defecto**:
- Development: `Debug`
- Production: `Information`
- Warnings y Errors siempre activos

---

## Temas a loguear (por endpoint/servicio)

### 1. Autenticación (AuthEndpoints)

```
📝 [Info]  Register attempt - Email: {Email}
✅ [Info]  User registered successfully - UserId: {UserId}, Email: {Email}
⚠️  [Warn] Register failed - Email already exists: {Email}
❌ [Error] Register failed - {ErrorMessage}
```

### 2. Challenges (ChallengeEndpoints)

```
📚 [Info]  Get challenges - Category: {Category}, Difficulty: {Difficulty}, Page: {Page}
✅ [Info]  Get challenges success - Count: {Count}, Total: {Total}
❌ [Error] Get challenges failed - {ErrorMessage}
```

### 3. Intentos (AttemptEndpoints)

```
📋 [Info]  Attempt submitted - UserId: {UserId}, ChallengeId: {ChallengeId}, Answer: {Answer}
✅ [Info]  Attempt correct - UserId: {UserId}, ChallengeId: {ChallengeId}, ELO: +{ELOGain}
❌ [Info]  Attempt incorrect - UserId: {UserId}, ChallengeId: {ChallengeId}
🏆 [Info]  Badge awarded - UserId: {UserId}, Badge: {BadgeName}
```

### 4. Estadísticas (UserEndpoints)

```
👤 [Info]  Get user stats - UserId: {UserId}
✅ [Info]  User stats retrieved - UserId: {UserId}, TotalAttempts: {Total}, Accuracy: {Accuracy}%
```

### 5. Servicios (Infrastructure)

```
🔑 [Info]  JWT token generated - UserId: {UserId}, ExpiresIn: {Hours}h
🔓 [Info]  JWT token validated - UserId: {UserId}
⚠️  [Warn] JWT token invalid - Reason: {Reason}
🎯 [Info]  ELO rating updated - UserId: {UserId}, Old: {Old}, New: {New}
🏅 [Info]  Streak recorded - UserId: {UserId}, Streak: {Streak}
⚠️  [Warn] Streak reset - UserId: {UserId}, Reason: {Reason}
```

### 6. Startup/Shutdown

```
🚀 [Info]  Starting DevBrain API - Environment: {Environment}
✅ [Info]  Application started successfully
⚠️  [Warn] Redis connection failed - Continuing without cache
⚠️  [Warn] Database migration pending
❌ [Fatal] Application crashed - {Exception}
```

---

## Propiedades enriquecidas (contexto automático)

Cada log contiene automáticamente:

```json
{
  "Timestamp": "2026-04-10T14:32:15.1234567Z",
  "Level": "Information",
  "MessageTemplate": "User registered successfully - UserId: {UserId}, Email: {Email}",
  "Properties": {
    "UserId": "550e8400-e29b-41d4-a716-446655440000",
    "Email": "alan@example.com",
    "Environment": "Production",
    "SourceContext": "DevBrain.Api.Endpoints.AuthEndpoints",
    "MachineName": "devbrain-production",
    "UserName": "app-service-user",
    "ThreadId": 12,
    "RequestId": "0HN3ELT1ELT1E:00000001"
  }
}
```

### Contexto automático (via Enrichers)

| Propiedad | Fuente | Uso |
|-----------|--------|-----|
| `Timestamp` | Serilog | Cuándo ocurrió |
| `Level` | Logger | Severidad |
| `SourceContext` | ASP.NET Core | Clase que loguea (ej: AuthEndpoints) |
| `MachineName` | Serilog.Enrichers.Environment | Máquina (prod: app-service-xxx) |
| `UserName` | Serilog.Enrichers.Environment | Usuario SO (prod: app-service-user) |
| `ThreadId` | Serilog.Enrichers.Environment | Thread que executa |
| `RequestId` | ASP.NET Core | Trace de request completo (correlación) |
| `UserId` | **Manual en logs** | Usuario que ejecuta acción |

---

## Configuración en Program.cs

### Inicialización

```csharp
// ANTES de BuildWebApplication
var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentUserName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        path: "logs/devbrain-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30
    )
    .WriteTo.ApplicationInsights(
        new TelemetryClient(),
        TelemetryConverter.Traces
    )
    .CreateLogger();

Log.Logger = logger;

try
{
    Log.Information("🚀 Starting DevBrain API - Environment: {Environment}", builder.Environment.EnvironmentName);
    
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(logger);
    builder.Services.AddApplicationInsightsTelemetry();
    
    // ... resto del setup
    
    var app = builder.Build();
    Log.Information("✅ Application started successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application crashed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
```

---

## Patrones de logging en endpoints

### Patrón: Inyectar ILogger<T>

```csharp
public class AuthEndpoints
{
    private readonly ILogger<AuthEndpoints> _logger;
    
    public AuthEndpoints(ILogger<AuthEndpoints> logger)
    {
        _logger = logger;
    }
    
    public async Task Register(RegisterRequest request)
    {
        using var activity = _logger.BeginScope(new { request.Email });
        
        _logger.LogInformation("📝 Register attempt - Email: {Email}", request.Email);
        
        try
        {
            var existingUser = await _userRepo.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("⚠️ Register failed - Email already exists: {Email}", request.Email);
                return Results.BadRequest("Email already registered");
            }
            
            var user = User.Create(request.Email, request.Password, request.DisplayName);
            await _userRepo.AddAsync(user);
            
            _logger.LogInformation("✅ User registered successfully - UserId: {UserId}, Email: {Email}", 
                user.Id, user.Email);
            
            return Results.Created($"/api/v1/users/{user.Id}", new { user.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Register failed - Email: {Email}", request.Email);
            return Results.StatusCode(500);
        }
    }
}
```

### Patrón: Scope para correlación

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["ChallengeId"] = challengeId
}))
{
    _logger.LogInformation("Submitting attempt...");
    // Cualquier log dentro de este scope incluye UserId y ChallengeId
}
```

---

## Búsquedas en Application Insights (Kusto Query Language)

### Buscar todos los rechazos de registro
```kusto
traces
| where message contains "Email already exists"
| project timestamp, message, email = tostring(customDimensions.Email)
| order by timestamp desc
```

### Buscar errores por usuario
```kusto
traces
| where customDimensions.UserId == "550e8400-e29b-41d4-a716-446655440000"
| where severity_level >= 2  // Error + Fatal
| order by timestamp desc
```

### Buscar requests lentos (> 2 segundos)
```kusto
requests
| where duration > 2000
| project timestamp, name, duration, responseCode
| order by duration desc
```

### Dashboard: Tasa de error en últimas 24h
```kusto
exceptions
| where timestamp > ago(24h)
| summarize ErrorCount = count() by type
| order by ErrorCount desc
```

---

## Invariantes

1. **Nunca loguear información sensible**: No passwords, no tokens, no secrets
2. **Logs siempre estructurados**: JSON para parsing automático (nunca string concatenation)
3. **RequestId por defecto**: ASP.NET Core auto-agrega para correlación
4. **Levels consistentes**: Mismo evento siempre usa mismo nivel
5. **No duplicación**: Si ya lo logueó un middleware, no repetir

---

## Qué NO es esta especificación

- **Metrics**: CPU, memoria, requests/sec (eso es Application Insights Metrics, diferente)
- **Tracing distribuido**: Distributed Tracing con OpenTelemetry (future enhancement)
- **Alertas**: Configurar alerts en Azure (eso es ops, no logging)
- **Analyzers**: Procesar logs para generar reportes (eso es BI/analytics)

---

## Archivos a crear/modificar

| Archivo | Acción | Responsabilidad |
|---------|--------|---|
| `Program.cs` | Modificar | Inicializar Serilog, Application Insights DI |
| `AuthEndpoints.cs` | Modificar | Agregar logs de registro/login |
| `ChallengeEndpoints.cs` | Modificar | Agregar logs de queries |
| `AttemptEndpoints.cs` | Modificar | Agregar logs de intentos, badges, ELO |
| `UserEndpoints.cs` | Modificar | Agregar logs de estadísticas |
| `appsettings.json` | Modificar | Configurar Serilog via config (opcional) |

---

## Testing del logging

Los tests deben:
1. **Verificar que Serilog está configurado** (inyectable)
2. **Verificar logs en tests de integración** (capturar output)
3. **NO bloquear tests por logging** (fail-safe)

---

## Versi
ónes de paquetes

```xml
<PackageReference Include="Serilog" Version="4.1.0" />
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.0" />
```

---

**Versión**: 1.0  
**Autor**: DevBrain Trainer Team  
**Fecha**: 2026-04-10

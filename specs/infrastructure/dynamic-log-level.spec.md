# Spec: Dynamic Log Level Configuration

**Tipo**: Infraestructura (Configuración de logging sin redeploy)  
**Ubicación**: `DevBrain.Infrastructure`, `DevBrain.Api`  
**Versión**: 1.0  

---

## Qué es

Capacidad de cambiar el nivel mínimo de Serilog (`MinimumLevel`) en tiempo de ejecución mediante variable de entorno `SERILOG__MINIMUMLEVEL`, sin necesidad de redeploy.

Permite operaciones en producción (Azure App Service) ajustar logging dinámicamente:
- **Reducir ruido**: Cambiar de `Debug` a `Information` cuando hay mucho tráfico
- **Aumentar detalle**: Cambiar a `Debug` para debuggear un issue en producción
- **Responder rápido**: Sin esperar redeploy, solo actualizar variable de entorno + restart

---

## Comportamiento esperado

### 1. Al arrancar la app

- Lee variable de entorno: `SERILOG__MINIMUMLEVEL`
- Parsea el valor a tipo `Serilog.Events.LogEventLevel` enum
- Si válido (Debug|Information|Warning|Error|Fatal): aplica a `LoggerConfiguration().MinimumLevel.Is(logLevel)`
- Si inválido o ausente: defaultea a `Information` (OWASP: no revelar demasiado en prod)

### 2. Valores soportados

| Valor (case-insensitive) | Enum | Comportamiento |
|---|---|---|
| `Debug` | `LogEventLevel.Debug` | Captura Debug + Information + Warning + Error + Fatal |
| `Information` | `LogEventLevel.Information` | Captura Information + Warning + Error + Fatal (default) |
| `Warning` | `LogEventLevel.Warning` | Captura Warning + Error + Fatal |
| `Error` | `LogEventLevel.Error` | Captura Error + Fatal |
| `Fatal` | `LogEventLevel.Fatal` | Captura Fatal solamente |
| `Verbose` | `LogEventLevel.Verbose` | Captura todo (Verbose + Debug + ...) |

### 3. Ejemplos de uso

```bash
# En Azure App Service Configuration (Portal) o en .env local:

# Caso 1: Debugging de issue en producción
SERILOG__MINIMUMLEVEL=Debug
→ App arranca con logging verboso

# Caso 2: Operación normal
SERILOG__MINIMUMLEVEL=Information
→ App arranca con nivel standard (por defecto)

# Caso 3: Error inválido
SERILOG__MINIMUMLEVEL=InvalidLevel
→ DefaultLog = Information (sin crash)

# Caso 4: Variable no existe
(no SERILOG__MINIMUMLEVEL)
→ DefaultLog = Information (sin crash)
```

---

## Cambios en código

### 1. Actualización de `Program.cs`

En la sección donde se configura Serilog (`var logger = new LoggerConfiguration()...`):

```csharp
// Leer env var
var minLevelStr = Environment.GetEnvironmentVariable("SERILOG__MINIMUMLEVEL") ?? "Information";

// Parsear a enum (case-insensitive)
// Si falla → defaultear a Information
var minLevel = Enum.TryParse<Serilog.Events.LogEventLevel>(
    minLevelStr, 
    ignoreCase: true, 
    out var parsedLevel
) 
    ? parsedLevel 
    : Serilog.Events.LogEventLevel.Information;

var logger = new LoggerConfiguration()
    .MinimumLevel.Is(minLevel)  // ← Aplicar el nivel parseado
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentUserName()
    // ... rest of configuration
```

### 2. No se modifica:
- `LoggerConfiguration` (estructura existente)
- Sinks (Console, File, Application Insights)
- Enrichers
- Nivel mínimo por defecto (`Information`)

---

## Invariantes (reglas que nunca se rompen)

1. **Fallback a Information**: Un valor inválido o variable no presente siempre defaultea a `Information`, nunca a nada más
2. **Sin crash por config**: Parsing inválido no mata la app, solo usa default
3. **Case-insensitive**: Aceptar `debug`, `DEBUG`, `Debug`, `INFORMATION`, etc.
4. **Aplica al arranque**: El nivel se fija cuando la app inicia; cambios posteriores a la env var requieren restart de app (limitación técnica de Serilog)
5. **Sin permisos especiales**: No requiere roles/auth especiales en Azure; solo actualizar app setting en Portal

---

## Qué NO es esta funcionalidad

- **Cambio en hot redeploy**: No es cambio de nivel sin restart (Serilog lo lee una sola vez en `Program.cs`)
- **Logging selectivo**: No es "log solo para ciertos usuarios" (este spec es global)
- **Sink dinámico**: No es cambiar qué sinks están activos (console, file, Application Insights siempre activos)
- **Rotación de secretos**: No es cambiar secretos/credenciales en tiempo de ejecución

---

## Escenarios de test

| Escenario | Entrada | Resultado esperado |
|---|---|---|
| Env var válido: `Debug` | `SERILOG__MINIMUMLEVEL=Debug` | App arranca con `LogEventLevel.Debug` ✅ |
| Env var válido: `Information` | `SERILOG__MINIMUMLEVEL=Information` | App arranca con `LogEventLevel.Information` ✅ |
| Env var válido: `Error` | `SERILOG__MINIMUMLEVEL=Error` | App arranca con `LogEventLevel.Error` ✅ |
| Case-insensitive: minúscula | `SERILOG__MINIMUMLEVEL=debug` | App arranca con `LogEventLevel.Debug` ✅ |
| Case-insensitive: mixto | `SERILOG__MINIMUMLEVEL=DeBuG` | App arranca con `LogEventLevel.Debug` ✅ |
| Valor inválido | `SERILOG__MINIMUMLEVEL=InvalidLevel` | App arranca con default `Information` ✅ |
| Variable no existe | (sin variable de entorno) | App arranca con default `Information` ✅ |
| String vacío | `SERILOG__MINIMUMLEVEL=` | App arranca con default `Information` ✅ |
| Parsing exitoso: verifica logs | `SERILOG__MINIMUMLEVEL=Debug` a Program.cs | Debug logs aparecen en output ✅ |
| Parsing fallido: sin crash | `SERILOG__MINIMUMLEVEL=BadValue` | App inicia correctamente (no excepción) ✅ |

---

## Tests a escribir

### Unit Tests (DevBrain.Infrastructure.Tests)

1. **Parsing válido a LogEventLevel**
   - Input: `"Debug"` → Output: `LogEventLevel.Debug`
   - Input: `"Information"` → Output: `LogEventLevel.Information`
   - Input: `"Warning"` → Output: `LogEventLevel.Warning`
   - Input: `"Error"` → Output: `LogEventLevel.Error`
   - Input: `"Fatal"` → Output: `LogEventLevel.Fatal`

2. **Case-insensitive parsing**
   - Input: `"debug"` → Output: `LogEventLevel.Debug`
   - Input: `"DEBUG"` → Output: `LogEventLevel.Debug`
   - Input: `"InFoRmAtIoN"` → Output: `LogEventLevel.Information`

3. **Fallback para valores inválidos**
   - Input: `"InvalidLevel"` → Output: `LogEventLevel.Information` (default)
   - Input: `""` (empty) → Output: `LogEventLevel.Information` (default)
   - Input: `"123"` → Output: `LogEventLevel.Information` (default)

### Integration Tests (DevBrain.Api.Tests)

4. **App respeta env var al arrancar**
   - Setup: `SERILOG__MINIMUMLEVEL=Debug` en TestFactory
   - Request: `GET /health`
   - Verify: Debug logs en output (simulados o capturados)

5. **Default sin env var**
   - Setup: sin variable `SERILOG__MINIMUMLEVEL`
   - Request: `GET /health`
   - Verify: Solo Information+ logs, no Debug (logging level check)

---

## Referencias

- Serilog LogEventLevel docs: https://github.com/serilog/serilog/wiki/Configuration-Basics#minimum-level
- Azure App Service Configuration: https://learn.microsoft.com/en-us/azure/app-service/configure-common
- Environment variables .NET: https://learn.microsoft.com/en-us/dotnet/api/system.environment.getenvironmentvariable

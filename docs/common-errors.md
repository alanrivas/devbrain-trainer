# Errores Comunes y Fixes — DevBrain Trainer

Registro de bugs no triviales que costaron tiempo de diagnóstico: cómo se manifestaron, qué conclusiones erróneas se sacaron primero y cómo se llegó al fix real.

---

## Error 001 — App crashea en startup al conectarse a Neon (PostgreSQL)

**Fecha**: Abril 2026  
**Contexto**: Primer deploy a Azure App Service usando Neon como base de datos PostgreSQL en la nube.

---

### Síntoma

La aplicación arrancaba en Azure App Service y caía de inmediato. Los logs de Azure mostraban que el proceso terminaba con código de salida no cero durante el startup. Desde afuera se veía un error 503 genérico.

Al intentar reproducirlo localmente apuntando a Neon (en lugar de PostgreSQL local), el proceso también caía con una excepción en el startup antes de levantar ningún endpoint.

---

### Falsa conclusión inicial

Como el problema aparecía primero en Azure, la investigación inicial apuntó al entorno de deploy:

- Se revisó el `Dockerfile` y el `railway.json` (artefactos del deploy anterior a Railway que quedaron en el repo)
- Se sospechó que el Azure App Service no tenía configuradas correctamente las variables de entorno
- Se pensó que el problema era el plan F1 de Azure (free tier) teniendo restricciones de memoria o red
- Se revisó si el GitHub Actions workflow estaba publicando el binario correcto
- Se probaron distintos formatos de connection string en el portal de Azure

Ninguna de esas hipótesis resultó ser el problema. El deploy en sí era correcto — el binario llegaba bien, las variables estaban seteadas.

---

### Causa raíz real

**Npgsql no soporta el formato URI de PostgreSQL.**

Neon (y la mayoría de los proveedores PaaS de PostgreSQL) entrega la connection string en formato URI estándar de PostgreSQL:

```
postgresql://neondb_owner:npg_xxx@ep-soft-thunder-a8tssc3k.eastus2.azure.neon.tech/neondb?sslmode=require
```

Sin embargo, el driver de .NET para PostgreSQL (`Npgsql`) solo acepta el formato ADO.NET (key=value):

```
Host=ep-soft-thunder-a8tssc3k.eastus2.azure.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_xxx;SSL Mode=Require;Trust Server Certificate=true
```

Cuando `DbContext` intentaba conectarse con el formato URI, Npgsql lanzaba una excepción durante la inicialización del pool de conexiones — antes de que la app terminara de arrancar. Como el `MigrateAsync()` y el `ConnectionMultiplexer.Connect()` de Redis estaban fuera de un try-catch en ese momento, cualquier excepción en esos puntos mataba el proceso entero.

El error en los logs (una vez que se logró ver) decía algo como:

```
Unhandled exception: Npgsql.NpgsqlException: No connection could be made because the target machine actively refused it.
```

o bien un `FormatException` al parsear la URI, dependiendo de la versión de Npgsql.

---

### Cómo se llegó a la solución

El punto de quiebre fue reproducir el error **localmente** apuntando a Neon. Al ver que el mismo crash ocurría en local (donde el entorno de Azure no podía ser el culpable), quedó descartado que el problema fuera de infraestructura.

Con la excepción visible en la consola local se pudo buscar específicamente "Npgsql URI format not supported" y confirmar en la documentación de Npgsql que el driver nunca implementó soporte para URIs de PostgreSQL — es una limitación conocida y deliberada del driver.

---

### Fix aplicado

**1. Convertir el connection string al formato ADO.NET** en todas las variables de entorno y configuración local:

```
# MAL (formato URI — Neon lo entrega así por defecto)
postgresql://user:pass@host/db?sslmode=require

# BIEN (formato ADO.NET — lo que Npgsql acepta)
Host=host;Database=db;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=true
```

**2. Envolver el startup en try-catch** para que fallos en Redis o en las migraciones no maten el proceso:

```csharp
// Redis — no crashear si no está disponible
try
{
    var redis = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddScoped<IStreakService, RedisStreakService>();
}
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP] Redis connection failed: {ex.Message}. Streak service disabled.");
}

// Migraciones — no crashear si la DB no está disponible
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DevBrainDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP] Database migration failed: {ex.Message}");
}
```

**3. Aplicar las migraciones manualmente a Neon** con el formato correcto:

```bash
dotnet ef database update \
  --connection "Host=<host>;Database=<db>;Username=<user>;Password=<pass>;SSL Mode=Require;Trust Server Certificate=true" \
  --project src/DevBrain.Infrastructure \
  --startup-project src/DevBrain.Api
```

---

### Lección aprendida

Cuando una app crashea en un entorno cloud pero no localmente, la primera hipótesis suele ser "algo del entorno". Eso es cierto muchas veces, pero si el crash ocurre en el startup antes de que levante cualquier endpoint, vale la pena **intentar reproducirlo localmente apuntando a los servicios cloud** (DB, Redis, etc.). Si el crash se reproduce localmente, el problema es del código — no del entorno.

En este caso el error estaba en el formato del connection string, que era invisible mientras se usaba la DB local (que no tiene ese problema). Recién apareció cuando se apuntó a un proveedor externo.

---

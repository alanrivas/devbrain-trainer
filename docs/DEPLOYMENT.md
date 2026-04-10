# Deployment Guide — DevBrain Trainer

Instrucciones para deployar el backend a Azure y el frontend a GitHub Pages/Vercel.

---

## Backend: Azure App Service

### Prerequisites

- Azure subscription (free tier available)
- `az` CLI installed
- Git configured
- `.env` file with secrets (never commit)

### Step 1: Create Azure Resources

```bash
# Login to Azure
az login

# Create resource group
az group create --name devbrain-rg --location eastus

# Create App Service Plan (Standard tier for production)
az appservice plan create \
  --name devbrain-plan \
  --resource-group devbrain-rg \
  --sku S1 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group devbrain-rg \
  --plan devbrain-plan \
  --name devbrain-trainer \
  --runtime "DOTNET|10.0"
```

### Step 2: Configure Environment Variables

In Azure Portal (App Service → Configuration → Application settings):

```
APPLICATIONINSIGHTS_INSTRUMENTATIONKEY = <key from Application Insights>
ConnectionStrings__DefaultConnection = Host=xxx.neon.tech;Database=devbrain;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=true
REDIS_CONNECTION = redis-cloud-xxx:6379,password=xxx
JWT_SECRET = your-secret-key-min-32-chars
ASPNETCORE_ENVIRONMENT = Production
```

Or via CLI:

```bash
az webapp config appsettings set \
  --resource-group devbrain-rg \
  --name devbrain-trainer \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    JWT_SECRET=$JWT_SECRET \
    ConnectionStrings__DefaultConnection=$DB_CONNECTION \
    REDIS_CONNECTION=$REDIS_CONNECTION
```

### Step 3: Setup GitHub Actions CI/CD

Create `.github/workflows/deploy-azure.yml`:

```yaml
name: Deploy Backend to Azure

on:
  push:
    branches: ['main']
    paths:
      - 'src/**'
      - 'tests/**'
      - '.github/workflows/deploy-azure.yml'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Build
        run: dotnet build src/DevBrain.Api/ -c Release
      
      - name: Test
        run: dotnet test -c Release --no-build
        continue-on-error: false
      
      - name: Publish
        run: dotnet publish src/DevBrain.Api/ -c Release -o ./publish
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: devbrain-trainer
          package: ./publish
```

### Step 4: Deploy

```bash
# Trigger via git push to main (CI/CD runs automatically)
git push origin main

# Or manual deployment
az webapp deployment source config-zip \
  --resource-group devbrain-rg \
  --name devbrain-trainer \
  --src ./publish.zip
```

### Step 5: Verify Deployment

```bash
# Check status
az webapp show --resource-group devbrain-rg --name devbrain-trainer

# View logs
az webapp log tail --resource-group devbrain-rg --name devbrain-trainer

# Test endpoint
curl https://devbrain-trainer.azurewebsites.net/api/v1/challenges
```

---

## Database: PostgreSQL (Neon.tech)

### Step 1: Create Neon Project

1. Go to https://console.neon.tech
2. Create new project: `devbrain-trainer`
3. Copy connection string (ADO.NET format):
   ```
   Host=xxx.neon.tech;Database=devbrain;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=true
   ```

### Step 2: Apply Migrations

```bash
# From local machine or Azure Cloud Shell
cd c:\dev\devbrain-trainer

# Set connection string
$env:CONNECTIONSTRING="Host=xxx.neon.tech;Database=devbrain;..." 

# Run migrations
dotnet ef database update -p src/DevBrain.Infrastructure/ -c DevBrainDbContext

# Verify tables created
psql -h xxx.neon.tech -U user -d devbrain -c "\dt"
```

### Step 3: Seed Data

```bash
# Run seed scripts
psql -h xxx.neon.tech -U user -d devbrain < database/setup/init-postgres.sql
psql -h xxx.neon.tech -U user -d devbrain < database/setup/setup-devbrain.sql
```

---

## Caching: Redis (Redis Cloud)

### Step 1: Create Redis Cloud Database

1. Go to https://app.rediscloud.com
2. Create new database: 256MB, no persistence
3. Copy connection string:
   ```
   redis-cloud-xxx.c12345.ng.0001.use1.cache.amazonaws.com:6379
   password=xxx
   ```

### Step 2: Configure in App

In `Program.cs`:

```csharp
var redisConnection = builder.Configuration["REDIS_CONNECTION"];
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "devbrain:";
});
```

Or in `appsettings.json`:

```json
{
  "CacheOptions": {
    "Enabled": true,
    "RedisConnection": "redis-cloud-xxx:6379,password=xxx"
  }
}
```

---

## Monitoring: Application Insights

### Step 1: Create Application Insights Resource

```bash
az monitor app-insights component create \
  --app devbrain-insights \
  --location eastus \
  --resource-group devbrain-rg \
  --application-type web
```

### Step 2: Get Instrumentation Key

```bash
az monitor app-insights component show \
  --app devbrain-insights \
  --resource-group devbrain-rg \
  --query instrumentationKey -o tsv
```

### Step 3: Configure in App

Set in Azure App Service settings:
```
APPLICATIONINSIGHTS_INSTRUMENTATIONKEY=<key>
```

### Step 4: View Logs & Metrics

In Azure Portal → Application Insights:
- **Live Metrics Stream** — Real-time requests, CPU, memory
- **Logs (KQL)** — Query all telemetry
- **Performance** — Slow endpoints, dependencies
- **Failures** — Exceptions and 5xx responses

**KQL Example - Find slow requests**:
```kusto
requests
| where duration > 1000
| project timestamp, name, duration, client_IP
| order by duration desc
```

---

## Frontend: GitHub Pages / Vercel

### Option 1: GitHub Pages (Free)

#### Step 1: Build Next.js

```bash
cd frontend  # or your frontend directory
npm run build
```

#### Step 2: Configure GitHub Pages

1. Push built site to `gh-pages` branch:
   ```bash
   npm run deploy  # Requires gh-pages package
   ```

2. In GitHub repo settings → Pages:
   - Source: `gh-pages` branch
   - Domain: `alanrivas.me/devbrain-trainer` (with CNAME)

#### Step 3: Add CNAME Record

In Cloudflare DNS:
```
Subdomain: devbrain-trainer
Type: CNAME
Value: alanrivas.github.io
```

### Option 2: Vercel (Recommended)

#### Step 1: Connect Repository

1. Go to https://vercel.com
2. Import GitHub repo: `alanrivas/devbrain-trainer`
3. Select `frontend/` as root directory

#### Step 2: Set Environment Variables

In Vercel Dashboard → Settings → Environment Variables:

```
NEXT_PUBLIC_API_URL = https://devbrain-trainer.azurewebsites.net/api/v1
JWT_SECRET_CLIENT = your-secret  # If needed for client-side auth
```

#### Step 3: Deploy

```bash
git push origin main  # Automatic deploy triggered
```

---

## Environment Configuration

### Local Development

Create `.env.local` in `src/DevBrain.Api/`:

```env
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=localhost;Database=devbrain;Username=postgres;Password=postgres;Port=5433
REDIS_CONNECTION=localhost:6379
JWT_SECRET=dev-secret-key-min-32-characters-long
APPLICATIONINSIGHTS_INSTRUMENTATIONKEY=  # Optional for dev
```

### Production (Azure)

Set via Azure App Service Configuration (above).

### Testing

Create `appsettings.Testing.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=:memory:"
  }
}
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

```
┌─ Push to main
│
├─ Build (.NET)
│  └─ dotnet build -c Release
│
├─ Test (xUnit)
│  └─ dotnet test -c Release (212 tests)
│
├─ Publish
│  └─ dotnet publish -c Release
│
└─ Deploy to Azure App Service
   └─ az webapp deployment source config-zip
```

### Manual Deployment Checklist

- [ ] All tests passing locally
- [ ] `context.md` updated
- [ ] No temporary files (.log, .txt)
- [ ] Environment variables configured in Azure
- [ ] Database migrations applied to Neon
- [ ] Redis connection tested
- [ ] Application Insights key set
- [ ] Push to main branch
- [ ] CI/CD succeeds (check GitHub Actions)
- [ ] Verify endpoint: `https://devbrain-trainer.azurewebsites.net/scalar`

---

## Troubleshooting

### Issue: Build fails in CI/CD

**Symptoms**:
```
dotnet build failed: CS0246 namespace not found
```

**Solution**:
```bash
# Locally verify
dotnet clean
dotnet restore
dotnet build -c Release

# Commit and retry push
```

### Issue: Database connection fails

**Symptoms**:
```
Npgsql.NpgsqlException: No connection could be made
```

**Solution**:
1. Verify connection string format (must be ADO.NET, not URI)
2. Check Neon IP whitelist (should allow all IPs for Azure)
3. Test connection locally:
   ```bash
   psql -h xxx.neon.tech -U user -d devbrain
   ```

### Issue: Redis connection timeout

**Symptoms**:
```
StackExchange.Redis: SocketFailure on PING
```

**Solution**:
```bash
# Test Redis connection
redis-cli -h redis-cloud-xxx.aws.cloud.redislabs.com -p 6379 -a password PING
# Should return: PONG
```

### Issue: Application Insights not logging

**Symptoms**:
```
No telemetry showing in Azure Portal
```

**Solution**:
1. Verify `APPLICATIONINSIGHTS_INSTRUMENTATIONKEY` is set
2. Check Serilog is configured to use ApplicationInsights sink
3. Restart app service:
   ```bash
   az webapp restart --resource-group devbrain-rg --name devbrain-trainer
   ```

---

## Rollback to Previous Version

```bash
# List deployment history
az webapp deployment list --resource-group devbrain-rg --name devbrain-trainer

# Swap slots (if using deployment slots)
az webapp deployment slot swap \
  --resource-group devbrain-rg \
  --name devbrain-trainer \
  --slot staging
```

---

## Cost Estimation (Monthly)

| Service | Tier | Cost |
|---|---|---|
| Azure App Service | Standard (S1) | $73.00 |
| PostgreSQL (Neon) | Serverless | ~$5-15 |
| Redis Cloud | 256MB | $12.00 |
| Application Insights | 1GB logs | ~$5.00 |
| GitHub Pages | Free | $0.00 |
| **Total** | | ~$95-103 |

**Cost Saving**: Use `B1` (Basic) tier for dev: $13/month

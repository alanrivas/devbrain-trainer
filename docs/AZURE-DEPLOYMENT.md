# 🚀 DevBrain Frontend Deployment to Azure

## Opción 1: Despliegue Manual (Recomendado para primero)

### Prerequisitos
- CLI de Azure: `az login`
- Node.js 20+
- Git

### Pasos

#### 1. Preparar la aplicación
```bash
cd frontend

# Instalar dependencias
npm ci

# Build para producción
npm run build
```

#### 2. Crear Resource Group (una sola vez)
```bash
az group create \
  --name devbrain-rg \
  --location eastus
```

#### 3. Crear App Service Plan (una sola vez)
```bash
az appservice plan create \
  --name devbrain-plan \
  --resource-group devbrain-rg \
  --sku B1 \
  --is-linux
```

#### 4. Crear Web App
```bash
az webapp create \
  --resource-group devbrain-rg \
  --plan devbrain-plan \
  --name devbrain-frontend \
  --runtime "node|20-lts"
```

#### 5. Configurar variables de entorno
```bash
az webapp config appsettings set \
  --resource-group devbrain-rg \
  --name devbrain-frontend \
  --settings NEXT_PUBLIC_API_URL="https://devbrain-trainer.azurewebsites.net/api/v1"
```

#### 6. Desplegar desde repositorio local
```bash
# Opción A: ZIP deployment
az webapp deployment source config-zip \
  --resource-group devbrain-rg \
  --name devbrain-frontend \
  --src devbrain-frontend.zip

# Opción B: Git deployment (automático)
az webapp deployment source config-local-git \
  --resource-group devbrain-rg \
  --name devbrain-frontend

# Luego push a Azure Git
git remote add azure $(az webapp deployment source config-local-git \
  --resource-group devbrain-rg \
  --name devbrain-frontend \
  --query url --output tsv)

git push azure main
```

### 7. Verificar despliegue
```bash
# Ver status
az webapp show \
  --resource-group devbrain-rg \
  --name devbrain-frontend \
  --query "defaultHostName"

# Ver logs
az webapp log tail \
  --resource-group devbrain-rg \
  --name devbrain-frontend
```

La aplicación estará disponible en: `https://devbrain-frontend.azurewebsites.net`

---

## Opción 2: Despliegue con CI/CD (GitHub Actions)

### Pasos

1. Crear service principal para Azure
```bash
az ad sp create-for-rbac \
  --name devbrain-deployment \
  --role contributor \
  --scopes /subscriptions/{SUBSCRIPTION_ID}/resourceGroups/devbrain-rg
```

2. Agregar secret a GitHub:
- Settings → Secrets → New repository secret
- Name: `AZURE_CREDENTIALS`
- Value: Output JSON del comando anterior

3. El workflow en `.azure/ci-cd.yml` se ejecutará automáticamente en cada push a main

---

## Configuración de Dominio Personalizado (Opcional)

```bash
az webapp config hostname add \
  --resource-group devbrain-rg \
  --webapp-name devbrain-frontend \
  --hostname tudominio.com
```

---

## Ambiente Production

| Variable | Valor |
|----------|-------|
| `NEXT_PUBLIC_API_URL` | `https://devbrain-trainer.azurewebsites.net/api/v1` |
| `NODE_ENV` | `production` |

---

## Troubleshooting

### Problema: CORS errors en browser
**Solución**: Verificar que el backend tiene CORS habilitado para `https://devbrain-frontend.azurewebsites.net`

### Problema: 404 Not Found en rutas
**Solución**: `web.config` está configurado para reescribir todas las rutas a Next.js

### Problema: Build fails
```bash
# Ver logs detallados
az webapp log tail --resource-group devbrain-rg --name devbrain-frontend --provider AzsWebAppProvider
```

---

## Versión Actual
- Fecha: 13 de Abril 2026
- Frontend: Next.js 16.2.3
- Backend: Azure (https://devbrain-trainer.azurewebsites.net)
- Database: PostgreSQL (Neon conexión desde Azure)

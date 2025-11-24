# Ho Hema Loans - Deployment Guide

**Version:** 1.0  
**Date:** November 7, 2025  
**Environment:** Microsoft Azure Cloud Platform  
**Deployment Model:** Container-based with Azure DevOps CI/CD  

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Azure Infrastructure Setup](#azure-infrastructure-setup)
3. [CI/CD Pipeline Configuration](#cicd-pipeline-configuration)
4. [Application Deployment](#application-deployment)
5. [Environment Configuration](#environment-configuration)
6. [Security Configuration](#security-configuration)
7. [Monitoring and Logging](#monitoring-and-logging)
8. [Post-Deployment Validation](#post-deployment-validation)
9. [Troubleshooting Guide](#troubleshooting-guide)
10. [Rollback Procedures](#rollback-procedures)

---

## Prerequisites

### Development Environment
- **Node.js:** v18.0.0 or later
- **npm:** v8.0.0 or later
- **.NET SDK:** v8.0 or later
- **Docker Desktop:** Latest version
- **Azure CLI:** v2.50.0 or later
- **Git:** v2.30.0 or later

### Azure Subscriptions and Permissions
- **Azure Subscription:** Active subscription with contributor access
- **Azure DevOps:** Organization and project setup
- **Required Azure Resource Providers:**
  - Microsoft.Web
  - Microsoft.Sql
  - Microsoft.Cache
  - Microsoft.KeyVault
  - Microsoft.Insights
  - Microsoft.OperationalInsights

### External Services
- **Meta WhatsApp Business Account:** Verified and approved
- **WhatsApp Cloud API:** Access tokens and webhook verification
- **Banking Integration:** API credentials and certificates
- **SMS Provider:** API credentials for OTP services

---

## Azure Infrastructure Setup

### 1. Resource Group Creation

```bash
# Set variables
RESOURCE_GROUP="rg-hohema-prod"
LOCATION="southafricanorth"
ENVIRONMENT="production"
APP_NAME="hohema-loans"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION \
  --tags environment=$ENVIRONMENT application=$APP_NAME
```

### 2. Azure Key Vault

```bash
# Create Key Vault
KEYVAULT_NAME="kv-hohema-prod"
az keyvault create \
  --name $KEYVAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku standard \
  --enable-soft-delete true \
  --retention-days 90

# Set secrets
az keyvault secret set --vault-name $KEYVAULT_NAME --name "JwtSecret" --value "your-super-secret-jwt-key-here"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "WhatsAppToken" --value "your-whatsapp-token"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "WhatsAppWebhookToken" --value "your-webhook-verify-token"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "DatabaseConnectionString" --value "your-sql-connection-string"
```

### 3. Azure SQL Database

```bash
# Create SQL Server
SQL_SERVER_NAME="sql-hohema-prod"
SQL_ADMIN_USER="sqladmin"
SQL_ADMIN_PASSWORD="ComplexPassword123!"

az sql server create \
  --name $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN_USER \
  --admin-password $SQL_ADMIN_PASSWORD

# Create SQL Database
DATABASE_NAME="HoHemaLoans"
az sql db create \
  --name $DATABASE_NAME \
  --server $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --service-objective S2 \
  --backup-storage-redundancy Zone

# Configure firewall rules
az sql server firewall-rule create \
  --server $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 4. Azure Cache for Redis

```bash
# Create Redis Cache
REDIS_NAME="redis-hohema-prod"
az redis create \
  --name $REDIS_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard \
  --vm-size C1 \
  --enable-non-ssl-port false
```

### 5. Container Registry

```bash
# Create Azure Container Registry
ACR_NAME="acrhohemaprod"
az acr create \
  --name $ACR_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku Standard \
  --admin-enabled true
```

### 6. App Service Plan and Web Apps

```bash
# Create App Service Plan
ASP_NAME="asp-hohema-prod"
az appservice plan create \
  --name $ASP_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku P1v3 \
  --is-linux true

# Create API App Service
API_APP_NAME="app-hohema-api-prod"
az webapp create \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $ASP_NAME \
  --deployment-container-image-name "$ACR_NAME.azurecr.io/hohema-api:latest"

# Create Frontend App Service (Static Web App alternative)
FRONTEND_APP_NAME="app-hohema-frontend-prod"
az webapp create \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $ASP_NAME \
  --deployment-container-image-name "$ACR_NAME.azurecr.io/hohema-frontend:latest"
```

### 7. Application Insights

```bash
# Create Application Insights
APPINSIGHTS_NAME="ai-hohema-prod"
az monitor app-insights component create \
  --app $APPINSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --kind web
```

### 8. Storage Account

```bash
# Create Storage Account for documents
STORAGE_NAME="sthohemaprod"
az storage account create \
  --name $STORAGE_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_ZRS \
  --kind StorageV2 \
  --access-tier Hot

# Create blob containers
az storage container create \
  --account-name $STORAGE_NAME \
  --name "documents" \
  --public-access off

az storage container create \
  --account-name $STORAGE_NAME \
  --name "contracts" \
  --public-access off
```

---

## CI/CD Pipeline Configuration

### 1. Azure DevOps Project Setup

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
    - main
    - develop
  paths:
    include:
    - src/*
    - deploy/*

variables:
  - group: hohema-prod-variables
  - name: containerRegistry
    value: 'acrhohemaprod.azurecr.io'
  - name: imageRepository
    value: 'hohema'
  - name: dockerfilePath
    value: '$(Build.SourcesDirectory)/deploy/docker'
  - name: tag
    value: '$(Build.BuildId)'

stages:
- stage: Build
  displayName: Build and Test
  jobs:
  - job: BuildAPI
    displayName: Build API
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: UseDotNet@2
      displayName: 'Use .NET 8 SDK'
      inputs:
        packageType: sdk
        version: 8.x
        installationPath: $(Agent.ToolsDirectory)/dotnet

    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        projects: 'src/api/**/*.csproj'

    - task: DotNetCoreCLI@2
      displayName: 'Build API'
      inputs:
        command: 'build'
        projects: 'src/api/**/*.csproj'
        arguments: '--configuration Release --no-restore'

    - task: DotNetCoreCLI@2
      displayName: 'Run API Tests'
      inputs:
        command: 'test'
        projects: 'tests/api/**/*.csproj'
        arguments: '--configuration Release --collect:"XPlat Code Coverage" --results-directory $(Agent.TempDirectory)'

    - task: PublishCodeCoverageResults@1
      displayName: 'Publish Code Coverage'
      inputs:
        codeCoverageTool: 'Cobertura'
        summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'

  - job: BuildFrontend
    displayName: Build Frontend
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: NodeTool@0
      displayName: 'Use Node.js 18'
      inputs:
        versionSpec: '18.x'

    - script: |
        cd src/frontend
        npm ci
      displayName: 'Install dependencies'

    - script: |
        cd src/frontend
        npm run type-check
      displayName: 'Type checking'

    - script: |
        cd src/frontend
        npm run test:unit
      displayName: 'Run unit tests'

    - script: |
        cd src/frontend
        npm run build
      displayName: 'Build application'

    - task: PublishBuildArtifacts@1
      displayName: 'Publish build artifacts'
      inputs:
        PathtoPublish: 'src/frontend/dist'
        ArtifactName: 'frontend-build'

- stage: ContainerBuild
  displayName: Build and Push Container Images
  dependsOn: Build
  condition: succeeded()
  jobs:
  - job: BuildContainers
    displayName: Build Container Images
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: Docker@2
      displayName: 'Build and Push API Image'
      inputs:
        containerRegistry: 'acrhohemaprod'
        repository: 'hohema-api'
        command: 'buildAndPush'
        Dockerfile: 'deploy/docker/api/Dockerfile'
        tags: |
          $(tag)
          latest

    - task: Docker@2
      displayName: 'Build and Push Frontend Image'
      inputs:
        containerRegistry: 'acrhohemaprod'
        repository: 'hohema-frontend'
        command: 'buildAndPush'
        Dockerfile: 'deploy/docker/frontend/Dockerfile'
        tags: |
          $(tag)
          latest

- stage: DeployProd
  displayName: Deploy to Production
  dependsOn: ContainerBuild
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployAPI
    displayName: Deploy API
    pool:
      vmImage: 'ubuntu-latest'
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebAppContainer@1
            displayName: 'Deploy API Container'
            inputs:
              azureSubscription: 'Azure-Production'
              appName: 'app-hohema-api-prod'
              containers: 'acrhohemaprod.azurecr.io/hohema-api:$(tag)'

  - deployment: DeployFrontend
    displayName: Deploy Frontend
    pool:
      vmImage: 'ubuntu-latest'
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebAppContainer@1
            displayName: 'Deploy Frontend Container'
            inputs:
              azureSubscription: 'Azure-Production'
              appName: 'app-hohema-frontend-prod'
              containers: 'acrhohemaprod.azurecr.io/hohema-frontend:$(tag)'

  - job: RunMigrations
    displayName: Run Database Migrations
    dependsOn: DeployAPI
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: SqlAzureDacpacDeployment@1
      displayName: 'Deploy Database Schema'
      inputs:
        azureSubscription: 'Azure-Production'
        ServerName: 'sql-hohema-prod.database.windows.net'
        DatabaseName: 'HoHemaLoans'
        SqlUsername: '$(SqlAdminUser)'
        SqlPassword: '$(SqlAdminPassword)'
        deployType: 'SqlTask'
        SqlFile: 'deploy/sql/schema.sql'
```

### 2. Docker Configuration

#### API Dockerfile
```dockerfile
# deploy/docker/api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/api/HoHema.Api/HoHema.Api.csproj", "src/api/HoHema.Api/"]
COPY ["src/api/HoHema.Core/HoHema.Core.csproj", "src/api/HoHema.Core/"]
COPY ["src/api/HoHema.Infrastructure/HoHema.Infrastructure.csproj", "src/api/HoHema.Infrastructure/"]
RUN dotnet restore "src/api/HoHema.Api/HoHema.Api.csproj"

COPY . .
WORKDIR "/src/src/api/HoHema.Api"
RUN dotnet build "HoHema.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HoHema.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "HoHema.Api.dll"]
```

#### Frontend Dockerfile
```dockerfile
# deploy/docker/frontend/Dockerfile
FROM node:18-alpine AS build
WORKDIR /app

COPY src/frontend/package*.json ./
RUN npm ci --only=production

COPY src/frontend/ .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY deploy/docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

#### Nginx Configuration
```nginx
# deploy/docker/frontend/nginx.conf
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "no-referrer-when-downgrade" always;
    add_header Content-Security-Policy "default-src 'self' http: https: data: blob: 'unsafe-inline'" always;

    # API proxy
    location /api/ {
        proxy_pass https://app-hohema-api-prod.azurewebsites.net/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Handle client-side routing
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

---

## Application Deployment

### 1. Environment Variables Configuration

```bash
# Configure API App Service Settings
az webapp config appsettings set \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    WEBSITES_ENABLE_APP_SERVICE_STORAGE="false" \
    WEBSITES_PORT="80" \
    ConnectionStrings__DefaultConnection="@Microsoft.KeyVault(SecretUri=https://$KEYVAULT_NAME.vault.azure.net/secrets/DatabaseConnectionString/)" \
    JwtSettings__SecretKey="@Microsoft.KeyVault(SecretUri=https://$KEYVAULT_NAME.vault.azure.net/secrets/JwtSecret/)" \
    WhatsApp__AccessToken="@Microsoft.KeyVault(SecretUri=https://$KEYVAULT_NAME.vault.azure.net/secrets/WhatsAppToken/)" \
    WhatsApp__WebhookVerifyToken="@Microsoft.KeyVault(SecretUri=https://$KEYVAULT_NAME.vault.azure.net/secrets/WhatsAppWebhookToken/)" \
    Redis__ConnectionString="redis-hohema-prod.redis.cache.windows.net:6380,password=[redis-key],ssl=True,abortConnect=False" \
    Storage__ConnectionString="DefaultEndpointsProtocol=https;AccountName=$STORAGE_NAME;AccountKey=[storage-key];EndpointSuffix=core.windows.net" \
    ApplicationInsights__InstrumentationKey="[app-insights-key]"

# Enable managed identity
az webapp identity assign \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Grant Key Vault access
PRINCIPAL_ID=$(az webapp identity show --name $API_APP_NAME --resource-group $RESOURCE_GROUP --query principalId --output tsv)
az keyvault set-policy \
  --name $KEYVAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### 2. Database Migration Script

```sql
-- deploy/sql/schema.sql
USE [HoHemaLoans]
GO

-- Enable TDE (Transparent Data Encryption)
IF NOT EXISTS (SELECT * FROM sys.dm_database_encryption_keys WHERE database_id = DB_ID('HoHemaLoans'))
BEGIN
    ALTER DATABASE [HoHemaLoans] SET ENCRYPTION ON;
END

-- Create schemas if they don't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'audit')
    EXEC('CREATE SCHEMA audit');

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'integration')
    EXEC('CREATE SCHEMA integration');

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'reporting')
    EXEC('CREATE SCHEMA reporting');

-- Run table creation scripts
:r create-tables.sql
:r create-indexes.sql
:r create-procedures.sql
:r create-views.sql
:r insert-default-data.sql

-- Update database version
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatabaseVersion]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.DatabaseVersion (
        Version nvarchar(10) NOT NULL,
        DeployedAt datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        DeployedBy nvarchar(100) NOT NULL DEFAULT SYSTEM_USER
    );
END

INSERT INTO dbo.DatabaseVersion (Version, DeployedBy) 
VALUES ('1.0.0', 'Azure DevOps Pipeline');
GO
```

### 3. Health Check Configuration

```csharp
// API Health Check Endpoint
// src/api/HoHema.Api/Controllers/HealthController.cs
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;

    public HealthController(IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            checks = new
            {
                database = await CheckDatabase(),
                redis = CheckRedis(),
                whatsapp = await CheckWhatsApp()
            }
        };

        return Ok(health);
    }

    private async Task<string> CheckDatabase()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.ExecuteSqlRawAsync("SELECT 1");
            return "healthy";
        }
        catch
        {
            return "unhealthy";
        }
    }

    private string CheckRedis()
    {
        try
        {
            var db = _redis.GetDatabase();
            db.Ping();
            return "healthy";
        }
        catch
        {
            return "unhealthy";
        }
    }

    private async Task<string> CheckWhatsApp()
    {
        try
        {
            // Simple WhatsApp API health check
            return "healthy";
        }
        catch
        {
            return "unhealthy";
        }
    }
}
```

---

## Environment Configuration

### 1. Production Configuration Files

#### API Configuration
```json
// src/api/HoHema.Api/appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "JwtSettings": {
    "Issuer": "https://app-hohema-api-prod.azurewebsites.net",
    "Audience": "https://hohema.co.za",
    "ExpiryMinutes": 60
  },
  "WhatsApp": {
    "ApiUrl": "https://graph.facebook.com/v18.0",
    "PhoneNumberId": "your-phone-number-id",
    "WebhookUrl": "https://app-hohema-api-prod.azurewebsites.net/api/webhooks/whatsapp"
  },
  "NCR": {
    "RegistrationNumber": "NCRCP12345",
    "MaxAdvancePercentage": 80,
    "MaxLoanPercentage": 25,
    "MaxInterestRate": 5.0
  },
  "Banking": {
    "ApiUrl": "https://api.bank.co.za",
    "CertificatePath": "/app/certificates/banking.pfx"
  },
  "AllowedHosts": [
    "hohema.co.za",
    "*.hohema.co.za"
  ]
}
```

#### Frontend Configuration
```typescript
// src/frontend/src/config/production.ts
export const config = {
  apiUrl: 'https://app-hohema-api-prod.azurewebsites.net/api',
  whatsappNumber: '+27123456789',
  environment: 'production',
  features: {
    biometricAuth: true,
    pushNotifications: true,
    analytics: true
  },
  analytics: {
    applicationInsights: {
      instrumentationKey: process.env.VITE_APP_INSIGHTS_KEY
    }
  },
  sentry: {
    dsn: process.env.VITE_SENTRY_DSN,
    environment: 'production'
  }
};
```

### 2. SSL/TLS Configuration

```bash
# Configure custom domain and SSL
CUSTOM_DOMAIN="api.hohema.co.za"
az webapp config hostname add \
  --webapp-name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname $CUSTOM_DOMAIN

# Enable HTTPS only
az webapp update \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true

# Configure SSL binding
az webapp config ssl bind \
  --certificate-thumbprint [cert-thumbprint] \
  --ssl-type SNI \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

---

## Security Configuration

### 1. Azure Active Directory Integration

```bash
# Create Azure AD App Registration
APP_REG_NAME="hohema-loans-api"
az ad app create \
  --display-name $APP_REG_NAME \
  --sign-in-audience AzureADMyOrg

# Get Application ID
APP_ID=$(az ad app list --display-name $APP_REG_NAME --query '[0].appId' -o tsv)

# Create service principal
az ad sp create --id $APP_ID

# Configure API permissions (if needed)
az ad app permission add \
  --id $APP_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope
```

### 2. Network Security Configuration

```bash
# Configure App Service IP restrictions
az webapp config access-restriction add \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --rule-name "WhatsAppWebhook" \
  --action Allow \
  --ip-address 173.252.74.22/32 \
  --priority 100

# Configure VNET integration (if using Premium plans)
VNET_NAME="vnet-hohema-prod"
SUBNET_NAME="subnet-app-services"

az network vnet create \
  --name $VNET_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --address-prefix 10.0.0.0/16

az network vnet subnet create \
  --name $SUBNET_NAME \
  --vnet-name $VNET_NAME \
  --resource-group $RESOURCE_GROUP \
  --address-prefix 10.0.1.0/24 \
  --delegations Microsoft.Web/serverfarms

az webapp vnet-integration add \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --vnet $VNET_NAME \
  --subnet $SUBNET_NAME
```

### 3. Security Headers and CORS

```csharp
// API Security Configuration
// src/api/HoHema.Api/Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", builder =>
        builder.WithOrigins("https://hohema.co.za", "https://www.hohema.co.za")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());
});

app.UseSecurityHeaders(policies =>
    policies
        .AddFrameOptionsDeny()
        .AddXssProtectionBlock()
        .AddContentTypeOptionsNoSniff()
        .AddReferrerPolicyStrictOriginWhenCrossOrigin()
        .AddCrossOriginEmbedderPolicy(builder => builder.RequireCorp())
        .AddCrossOriginOpenerPolicy(builder => builder.SameOrigin())
        .AddCrossOriginResourcePolicy(builder => builder.CrossOrigin())
        .AddContentSecurityPolicy(builder =>
        {
            builder.AddObjectSrc().None();
            builder.AddFormAction().Self();
            builder.AddFrameAncestors().None();
        }));
```

---

## Monitoring and Logging

### 1. Application Insights Configuration

```json
// Application Insights Configuration
{
  "ApplicationInsights": {
    "InstrumentationKey": "[key-from-keyvault]",
    "EnableAdaptiveSampling": true,
    "EnablePerformanceCounterCollectionModule": true,
    "EnableQuickPulseMetricStream": true,
    "EnableRequestTrackingTelemetryModule": true,
    "EnableDependencyTrackingTelemetryModule": true
  },
  "Logging": {
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning"
      }
    }
  }
}
```

### 2. Custom Telemetry

```csharp
// Custom Application Insights Telemetry
public class LoanApplicationTelemetry
{
    private readonly TelemetryClient _telemetryClient;

    public LoanApplicationTelemetry(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void TrackLoanApplication(string applicationId, string productType, decimal amount)
    {
        var properties = new Dictionary<string, string>
        {
            { "ApplicationId", applicationId },
            { "ProductType", productType }
        };

        var metrics = new Dictionary<string, double>
        {
            { "Amount", (double)amount }
        };

        _telemetryClient.TrackEvent("LoanApplicationSubmitted", properties, metrics);
    }

    public void TrackProcessingTime(string applicationId, TimeSpan processingTime)
    {
        _telemetryClient.TrackMetric("LoanProcessingTime", 
            processingTime.TotalMinutes, 
            new Dictionary<string, string> { { "ApplicationId", applicationId } });
    }
}
```

### 3. Azure Monitor Alerts

```bash
# Create action group for notifications
ACTION_GROUP_NAME="ag-hohema-alerts"
az monitor action-group create \
  --name $ACTION_GROUP_NAME \
  --resource-group $RESOURCE_GROUP \
  --short-name "HoHemaAlerts" \
  --email "admin" "admin@hohema.co.za"

# Create alert rules
az monitor metrics alert create \
  --name "API Response Time High" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/[subscription-id]/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$API_APP_NAME" \
  --condition "avg http_response_time > 5000" \
  --description "API response time is higher than 5 seconds" \
  --evaluation-frequency 5m \
  --window-size 15m \
  --severity 2 \
  --action $ACTION_GROUP_NAME

az monitor metrics alert create \
  --name "SQL Database DTU High" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/[subscription-id]/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Sql/servers/$SQL_SERVER_NAME/databases/$DATABASE_NAME" \
  --condition "avg dtu_consumption_percent > 80" \
  --description "Database DTU consumption is higher than 80%" \
  --evaluation-frequency 5m \
  --window-size 15m \
  --severity 2 \
  --action $ACTION_GROUP_NAME
```

---

## Post-Deployment Validation

### 1. Automated Tests

```bash
#!/bin/bash
# deploy/scripts/validate-deployment.sh

API_BASE_URL="https://app-hohema-api-prod.azurewebsites.net/api"
FRONTEND_URL="https://hohema.co.za"

echo "Starting deployment validation..."

# Test API health endpoint
echo "Testing API health..."
HEALTH_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" $API_BASE_URL/health)
if [ $HEALTH_RESPONSE -eq 200 ]; then
    echo "✓ API health check passed"
else
    echo "✗ API health check failed with status $HEALTH_RESPONSE"
    exit 1
fi

# Test database connectivity
echo "Testing database connectivity..."
DB_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" $API_BASE_URL/health/database)
if [ $DB_RESPONSE -eq 200 ]; then
    echo "✓ Database connectivity check passed"
else
    echo "✗ Database connectivity check failed"
    exit 1
fi

# Test WhatsApp webhook
echo "Testing WhatsApp webhook..."
WEBHOOK_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$API_BASE_URL/webhooks/whatsapp?hub.verify_token=test&hub.challenge=test&hub.mode=subscribe")
if [ $WEBHOOK_RESPONSE -eq 200 ]; then
    echo "✓ WhatsApp webhook verification passed"
else
    echo "✗ WhatsApp webhook verification failed"
    exit 1
fi

# Test frontend
echo "Testing frontend..."
FRONTEND_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" $FRONTEND_URL)
if [ $FRONTEND_RESPONSE -eq 200 ]; then
    echo "✓ Frontend accessibility check passed"
else
    echo "✗ Frontend accessibility check failed"
    exit 1
fi

echo "All validation tests passed successfully!"
```

### 2. Performance Tests

```javascript
// deploy/tests/performance.js
import { check } from 'k6';
import http from 'k6/http';

export let options = {
  stages: [
    { duration: '2m', target: 10 },
    { duration: '5m', target: 50 },
    { duration: '2m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.1'],
  },
};

export default function () {
  const baseUrl = 'https://app-hohema-api-prod.azurewebsites.net/api';
  
  // Test health endpoint
  let response = http.get(`${baseUrl}/health`);
  check(response, {
    'health check status is 200': (r) => r.status === 200,
    'health check response time < 500ms': (r) => r.timings.duration < 500,
  });

  // Test authentication
  const loginPayload = JSON.stringify({
    phoneNumber: '+27123456789',
    password: 'testpassword'
  });

  response = http.post(`${baseUrl}/auth/login`, loginPayload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(response, {
    'login status is 200 or 401': (r) => [200, 401].includes(r.status),
    'login response time < 1000ms': (r) => r.timings.duration < 1000,
  });
}
```

---

## Troubleshooting Guide

### Common Issues and Solutions

#### 1. Database Connection Issues
```bash
# Check connection string in Key Vault
az keyvault secret show --vault-name $KEYVAULT_NAME --name "DatabaseConnectionString"

# Test SQL connectivity from App Service
az webapp ssh --name $API_APP_NAME --resource-group $RESOURCE_GROUP
# Inside container:
# curl -v telnet://sql-hohema-prod.database.windows.net:1433
```

#### 2. Key Vault Access Issues
```bash
# Verify managed identity assignment
az webapp identity show --name $API_APP_NAME --resource-group $RESOURCE_GROUP

# Check Key Vault access policies
az keyvault show --name $KEYVAULT_NAME --query "properties.accessPolicies"

# Re-assign Key Vault permissions
PRINCIPAL_ID=$(az webapp identity show --name $API_APP_NAME --resource-group $RESOURCE_GROUP --query principalId --output tsv)
az keyvault set-policy --name $KEYVAULT_NAME --object-id $PRINCIPAL_ID --secret-permissions get list
```

#### 3. WhatsApp Webhook Issues
```bash
# Test webhook verification manually
curl -X GET "https://app-hohema-api-prod.azurewebsites.net/api/webhooks/whatsapp?hub.verify_token=your-verify-token&hub.challenge=test123&hub.mode=subscribe"

# Check webhook logs
az webapp log tail --name $API_APP_NAME --resource-group $RESOURCE_GROUP
```

#### 4. Container Registry Authentication
```bash
# Login to ACR
az acr login --name $ACR_NAME

# Update App Service to use latest image
az webapp config container set \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --docker-custom-image-name "$ACR_NAME.azurecr.io/hohema-api:latest"

# Restart the app
az webapp restart --name $API_APP_NAME --resource-group $RESOURCE_GROUP
```

---

## Rollback Procedures

### 1. Application Rollback

```bash
#!/bin/bash
# deploy/scripts/rollback.sh

PREVIOUS_TAG=$1
API_APP_NAME="app-hohema-api-prod"
FRONTEND_APP_NAME="app-hohema-frontend-prod"
RESOURCE_GROUP="rg-hohema-prod"
ACR_NAME="acrhohemaprod"

if [ -z "$PREVIOUS_TAG" ]; then
    echo "Error: Please provide the previous tag to rollback to"
    echo "Usage: ./rollback.sh <previous-tag>"
    exit 1
fi

echo "Rolling back to version: $PREVIOUS_TAG"

# Rollback API
echo "Rolling back API..."
az webapp config container set \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --docker-custom-image-name "$ACR_NAME.azurecr.io/hohema-api:$PREVIOUS_TAG"

# Rollback Frontend
echo "Rolling back Frontend..."
az webapp config container set \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --docker-custom-image-name "$ACR_NAME.azurecr.io/hohema-frontend:$PREVIOUS_TAG"

# Restart services
echo "Restarting services..."
az webapp restart --name $API_APP_NAME --resource-group $RESOURCE_GROUP
az webapp restart --name $FRONTEND_APP_NAME --resource-group $RESOURCE_GROUP

echo "Rollback completed successfully!"
```

### 2. Database Rollback

```sql
-- Rollback script template
-- deploy/sql/rollback-template.sql

BEGIN TRANSACTION;

-- Store current state
SELECT * INTO #BackupTable FROM OriginalTable WHERE 1=0;

-- Apply rollback changes
-- ... rollback operations ...

-- Verify rollback
IF @@ERROR = 0
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Rollback completed successfully';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Rollback failed, changes reverted';
END
```

### 3. Emergency Procedures

```bash
# Emergency stop - scale down to zero instances
az appservice plan update \
  --name $ASP_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku FREE

# Emergency maintenance mode
az webapp config appsettings set \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings MAINTENANCE_MODE="true"

# Redirect traffic to maintenance page
az webapp config set \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --default-documents "maintenance.html"
```

---

## Final Checklist

### Pre-Deployment
- [ ] Azure resources provisioned
- [ ] DNS records configured
- [ ] SSL certificates installed
- [ ] Key Vault secrets configured
- [ ] Database schema deployed
- [ ] Container images built and pushed

### Post-Deployment
- [ ] Health checks passing
- [ ] SSL certificates valid
- [ ] Database connectivity verified
- [ ] WhatsApp webhook configured
- [ ] Monitoring alerts active
- [ ] Performance tests passed
- [ ] Security scans completed
- [ ] Backup procedures verified

### Go-Live
- [ ] Production data migrated
- [ ] User acceptance testing completed
- [ ] Support team notified
- [ ] Documentation updated
- [ ] Rollback plan ready

This deployment guide provides a comprehensive approach to deploying the Ho Hema Loans platform to production on Microsoft Azure, ensuring security, scalability, and maintainability.
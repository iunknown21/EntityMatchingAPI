# Azure Deployment Guide

Complete guide for deploying EntityMatchingAPI to Azure.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Security Status](#security-status)
3. [Deployment Steps](#deployment-steps)
4. [Post-Deployment Verification](#post-deployment-verification)
5. [Troubleshooting](#troubleshooting)

---

## Prerequisites

✅ **Already Complete:**
- GitHub repository created and renamed to EntityMatchingAPI
- Code fully renamed from ProfileMatching to EntityMatching
- Local API keys secured (never committed to git)
- Azure CLI installed and logged in

❌ **Still Needed:**
- Azure infrastructure (will create in this guide)
- Deployed Function App
- Published SDKs (optional)

---

## Security Status

### ✅ Protected (Good!)

All sensitive files are protected and were never committed:
- `dont check in.txt` - contains API keys (in .gitignore)
- `AZURE_CREDENTIALS.json` - GitHub Actions credentials (in .gitignore)
- `azure-functions-publish-profile.xml` - publish profile (in .gitignore)
- `local.settings.json` - local development settings (in .gitignore)

### ⚠ Important Notes

- API keys will be stored in **Azure Key Vault** (secure)
- Function App will use **Managed Identity** to access Key Vault (no credentials in code)
- All secrets referenced via `@Microsoft.KeyVault(...)` syntax in app settings

---

## Deployment Steps

### Step 1: Deploy Azure Infrastructure (15-20 minutes)

Run the deployment script to create all Azure resources:

```powershell
# Test run (see what will be created without actually creating)
.\deploy-azure-infrastructure.ps1 -WhatIf $true

# Actual deployment
.\deploy-azure-infrastructure.ps1

# With APIM (adds 45+ minutes, optional)
.\deploy-azure-infrastructure.ps1 -CreateApim $true
```

**What this creates:**
- Resource Group: `entitymatchingai`
- Cosmos DB: `entitymatchingdb` with 6 containers
- Storage Account: `entitymatchstore`
- Function App: `entityaiapi`
- Key Vault: `entitymatching-kv`
- Managed Identity and permissions
- All secrets stored securely

**Resources Created:**

| Resource Type | Name | Purpose |
|--------------|------|---------|
| Resource Group | `entitymatchingai` | Container for all resources |
| Cosmos DB Account | `entitymatchingdb` | NoSQL database |
| Database | `EntityMatchingDB` | Main database |
| Containers | `entities`, `conversations`, `embeddings`, `ratings`, `reputations`, `matches` | Data storage |
| Storage Account | `entitymatchstore` | Function App storage |
| Function App | `entityaiapi` | API endpoints |
| Key Vault | `entitymatching-kv` | Secret storage |
| Managed Identity | (auto-created) | Secure access to Key Vault |

---

### Step 2: Deploy Function App Code (10 minutes)

Build and deploy your Function App:

```powershell
# Navigate to Functions project
cd EntityMatching.Functions

# Deploy using Azure Functions Core Tools
func azure functionapp publish entityaiapi

# OR using dotnet publish + zip deploy
dotnet publish --configuration Release --output ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..
az functionapp deployment source config-zip `
  --resource-group entitymatchingai `
  --name entityaiapi `
  --src deploy.zip
```

**Expected output:**
```
Getting site publishing info...
Creating archive for current directory...
Uploading 15.2 MB...
Upload completed successfully.
Deployment completed successfully.
```

---

### Step 3: Verify Deployment (5 minutes)

Test your endpoints:

```powershell
# Health check
curl https://entityaiapi.azurewebsites.net/api/version

# Expected response:
# {"version":"1.0.0","environment":"Production"}

# List all functions
func azure functionapp list-functions entityaiapi

# Check logs
func azure functionapp logstream entityaiapi
```

**Run integration tests:**

```powershell
cd D:\Development\Main\EntityMatchingAPI

# Set test environment variables
$env:AZURE_FUNCTION_BASE_URL = "https://entityaiapi.azurewebsites.net"
$env:COSMOS_CONNECTION_STRING = "your-connection-string"

# Run tests
dotnet test --filter "FullyQualifiedName~Integration"
```

---

### Step 4: Clean Up Old Resources (Optional, 5 minutes)

If you have old ProfileMatching resources, clean them up:

```powershell
# Export configuration first (backup)
.\cleanup-old-azure-resources.ps1 -SkipExport $false

# Dry run to see what will be deleted
.\cleanup-old-azure-resources.ps1 -WhatIf $true

# Actually delete old resources
.\cleanup-old-azure-resources.ps1
# Type 'DELETE' when prompted
```

---

### Step 5: Configure GitHub Actions (5 minutes)

Update GitHub secrets for automated deployments:

```powershell
# Get Function App publish profile
az functionapp deployment list-publishing-profiles `
  --name entityaiapi `
  --resource-group entitymatchingai `
  --xml > publish-profile.xml

# Add as GitHub secret
gh secret set AZURE_FUNCTIONAPP_PUBLISH_PROFILE < publish-profile.xml

# Verify GitHub Actions workflow
gh workflow list
gh workflow run azure-functions-deploy.yml
```

---

## Post-Deployment Verification

### Checklist

- [ ] Resource group `entitymatchingai` exists
- [ ] Function App `entityaiapi` is running
- [ ] Health endpoint responds: `https://entityaiapi.azurewebsites.net/api/version`
- [ ] Cosmos DB has all 6 containers
- [ ] Key Vault contains all secrets
- [ ] Managed Identity has Key Vault access
- [ ] Function App settings reference Key Vault
- [ ] GitHub Actions workflows execute successfully
- [ ] Integration tests pass

### Verification Commands

```powershell
# Check all resources
az resource list --resource-group entitymatchingai --output table

# Check Function App status
az functionapp show --name entityaiapi --resource-group entitymatchingai --query "state"

# Check Cosmos DB containers
az cosmosdb sql container list `
  --account-name entitymatchingdb `
  --resource-group entitymatchingai `
  --database-name EntityMatchingDB `
  --query "[].name"

# Check Key Vault secrets
az keyvault secret list --vault-name entitymatching-kv --query "[].name"

# Test Function App
curl https://entityaiapi.azurewebsites.net/api/version
```

---

## Troubleshooting

### Function App won't start

**Check logs:**
```powershell
az functionapp log tail --name entityaiapi --resource-group entitymatchingai
```

**Common issues:**
- Missing Key Vault permissions → Re-run managed identity setup
- Invalid connection strings → Check Key Vault secret values
- Wrong runtime version → Verify .NET 8 isolated worker

### Cosmos DB connection fails

**Verify connection string:**
```powershell
az cosmosdb keys list `
  --name entitymatchingdb `
  --resource-group entitymatchingai `
  --type connection-strings
```

**Check Key Vault:**
```powershell
az keyvault secret show `
  --vault-name entitymatching-kv `
  --name CosmosDbConnectionString
```

### Key Vault access denied

**Re-configure managed identity:**
```powershell
# Get Function App principal ID
$principalId = az functionapp identity show `
  --name entityaiapi `
  --resource-group entitymatchingai `
  --query principalId -o tsv

# Grant Key Vault access
az keyvault set-policy `
  --name entitymatching-kv `
  --object-id $principalId `
  --secret-permissions get list
```

### Deployment fails

**Clear deployment cache:**
```powershell
az functionapp restart --name entityaiapi --resource-group entitymatchingai
```

**Redeploy:**
```powershell
func azure functionapp publish entityaiapi --force
```

---

## Quick Reference

### Resource Names
```
Resource Group:  entitymatchingai
Cosmos DB:       entitymatchingdb
Database:        EntityMatchingDB
Function App:    entityaiapi
Storage:         entitymatchstore
Key Vault:       entitymatching-kv
```

### Endpoints
```
Function App:    https://entityaiapi.azurewebsites.net
Health Check:    https://entityaiapi.azurewebsites.net/api/version
APIM (optional): https://entitymatching-apim.azure-api.net
```

### Common Commands
```powershell
# Deploy functions
func azure functionapp publish entityaiapi

# View logs
func azure functionapp logstream entityaiapi

# List functions
func azure functionapp list-functions entityaiapi

# Restart app
az functionapp restart --name entityaiapi --resource-group entitymatchingai

# Get connection strings
az cosmosdb keys list --name entitymatchingdb --resource-group entitymatchingai --type connection-strings
```

---

## Next Steps

After successful deployment:

1. **Monitor Performance**
   - Set up Application Insights
   - Configure alerts for errors
   - Monitor Cosmos DB RU consumption

2. **Publish SDKs** (optional)
   - EntityMatching.SDK to NuGet
   - @entitymatching/sdk to npm

3. **Documentation**
   - Update API documentation with new URLs
   - Create developer guide for SDK usage

4. **Testing**
   - Run load tests
   - Verify all endpoints
   - Test error handling

---

## Cost Estimates

### Monthly Costs (approximate)

| Resource | Tier | Est. Cost |
|----------|------|-----------|
| Function App | Consumption | $0-20 |
| Cosmos DB | Serverless | $10-50 |
| Storage Account | Standard | $1-5 |
| Key Vault | Standard | $0.03/secret |
| APIM (optional) | Consumption | $0.50/10k calls |

**Total:** ~$15-80/month depending on usage

---

## Support

- **Azure Issues:** Check Azure Portal → Resource Health
- **Deployment Issues:** Review deployment logs in Azure Portal
- **Code Issues:** Check Function App logs via `func azure functionapp logstream`
- **GitHub Actions:** Check workflow runs at https://github.com/iunknown21/EntityMatchingAPI/actions

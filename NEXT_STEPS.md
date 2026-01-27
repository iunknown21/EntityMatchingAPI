# Next Steps - Quick Start Guide

## Current Status: Phases 1-3 Complete ✅

Your code is fully renamed and ready to deploy. Here's what to do next:

---

## Option 1: Complete Deployment (Full Rename)

### Step 1: Rename GitHub Repository (15 minutes)
```bash
# 1. Go to GitHub: https://github.com/iunknown21/ProfileMatchingAPI/settings
# 2. Change repository name: ProfileMatchingAPI → EntityMatchingAPI
# 3. Update local git remote:

cd D:\Development\Main\EntityMatchingAPI
git remote add origin https://github.com/iunknown21/EntityMatchingAPI.git
git push -u origin master
```

### Step 2: Create Azure Resources (3-4 hours)

**Export old config first (backup):**
```bash
az login

# Export APIM config
az apim api list --resource-group profilesai --service-name profilematching-apim > old-apim-config.json

# Export Function App settings
az functionapp config appsettings list --name profileaiapi --resource-group profilesai > old-function-settings.json
```

**Delete old resources:**
```bash
az group delete --name profilesai --yes --no-wait
```

**Create new resources:**
```bash
# Resource Group
az group create --name entitymatchingai --location eastus

# Cosmos DB
az cosmosdb create \
  --name entitymatchingdb \
  --resource-group entitymatchingai \
  --locations regionName=eastus \
  --capabilities EnableServerless

az cosmosdb sql database create \
  --account-name entitymatchingdb \
  --resource-group entitymatchingai \
  --name EntityMatchingDB

# Create containers
for container in entities conversations embeddings ratings reputations matches; do
  az cosmosdb sql container create \
    --account-name entitymatchingdb \
    --resource-group entitymatchingai \
    --database-name EntityMatchingDB \
    --name $container \
    --partition-key-path "/id"
done

# Storage Account
az storage account create \
  --name entitymatchingstorage \
  --resource-group entitymatchingai \
  --location eastus \
  --sku Standard_LRS

# Function App
az functionapp create \
  --resource-group entitymatchingai \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --name entityaiapi \
  --storage-account entitymatchingstorage

# Key Vault
az keyvault create \
  --name entitymatching-kv \
  --resource-group entitymatchingai \
  --location eastus

# Configure Managed Identity
az functionapp identity assign \
  --name entityaiapi \
  --resource-group entitymatchingai

PRINCIPAL_ID=$(az functionapp identity show \
  --name entityaiapi \
  --resource-group entitymatchingai \
  --query principalId -o tsv)

az keyvault set-policy \
  --name entitymatching-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# APIM (takes 45 minutes)
az apim create \
  --name entitymatching-apim \
  --resource-group entitymatchingai \
  --publisher-name "ByStorm" \
  --publisher-email "admin@bystorm.com" \
  --sku-name Consumption \
  --location eastus
```

**Migrate secrets to Key Vault:**
```bash
# Get Cosmos connection string
COSMOS_CONN=$(az cosmosdb keys list \
  --name entitymatchingdb \
  --resource-group entitymatchingai \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv)

# Store in Key Vault
az keyvault secret set \
  --vault-name entitymatching-kv \
  --name CosmosDbConnectionString \
  --value "$COSMOS_CONN"

# Add other secrets (OpenAI, Groq, etc.)
az keyvault secret set --vault-name entitymatching-kv --name OpenAIKey --value "YOUR_KEY"
az keyvault secret set --vault-name entitymatching-kv --name GroqApiKey --value "YOUR_KEY"
```

**Configure Function App:**
```bash
az functionapp config appsettings set \
  --name entityaiapi \
  --resource-group entitymatchingai \
  --settings \
    "CosmosDb__ConnectionString=@Microsoft.KeyVault(SecretUri=https://entitymatching-kv.vault.azure.net/secrets/CosmosDbConnectionString/)" \
    "CosmosDb__DatabaseId=EntityMatchingDB" \
    "CosmosDb__EntitiesContainerId=entities" \
    "CosmosDb__ConversationsContainerId=conversations" \
    "CosmosDb__EmbeddingsContainerId=embeddings" \
    "CosmosDb__RatingsContainerId=ratings" \
    "CosmosDb__ReputationsContainerId=reputations" \
    "CosmosDb__MatchesContainerId=matches"
```

### Step 3: Deploy Functions (30 minutes)
```bash
cd D:\Development\Main\EntityMatchingAPI\EntityMatching.Functions
dotnet publish --configuration Release --output ./publish

cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force

az functionapp deployment source config-zip \
  --resource-group entitymatchingai \
  --name entityaiapi \
  --src ../deploy.zip
```

### Step 4: Test Deployment (1 hour)
```bash
# Health check
curl https://entityaiapi.azurewebsites.net/api/version

# Run integration tests
cd D:\Development\Main\EntityMatchingAPI
dotnet test EntityMatching.Tests/EntityMatching.Tests.csproj \
  --filter "FullyQualifiedName~Integration"
```

### Step 5: Publish SDKs (1 hour)
```bash
# Unpublish old packages (no users, safe)
nuget delete ProfileMatching.SDK 1.0.0 -Source https://api.nuget.org/v3/index.json -NonInteractive
npm unpublish @profilematching/sdk --force

# Publish new packages
cd EntityMatching.SDK
dotnet pack --configuration Release /p:Version=1.0.0
dotnet nuget push bin/Release/EntityMatching.SDK.1.0.0.nupkg \
  --api-key [NUGET_KEY] \
  --source https://api.nuget.org/v3/index.json

cd ../EntityMatching.SDK.JS
npm version 1.0.0
npm run build
npm publish --access public
```

### Step 6: Cleanup (30 minutes)
```bash
# Once everything is verified working:
cd D:\Development\Main
Remove-Item -Path ProfileMatchingAPI -Recurse -Force
```

---

## Option 2: Continue with ProfileMatchingAPI (Rollback)

If you decide NOT to proceed with the rename:

```bash
# Simply delete the EntityMatchingAPI folder
cd D:\Development\Main
Remove-Item -Path EntityMatchingAPI -Recurse -Force

# Continue using ProfileMatchingAPI as before
cd ProfileMatchingAPI
```

**Impact:** ZERO - nothing was deployed, original code untouched

---

## Option 3: Parallel Operation (Test First)

Keep both systems running temporarily:

1. **Keep old Azure resources** (`profileaiapi`) running
2. **Deploy EntityMatchingAPI to new resources** (`entityaiapi`)
3. **Test new system** while old one still works
4. **Switch over** when confident
5. **Delete old resources** and ProfileMatchingAPI folder

---

## Verification Checklist

After deployment, verify:

### Azure Resources
- [ ] Resource group `entitymatchingai` exists
- [ ] Function App `entityaiapi` is running
- [ ] Cosmos DB `entitymatchingdb` has all containers
- [ ] Key Vault `entitymatching-kv` has all secrets
- [ ] APIM `entitymatching-apim` is provisioned

### Functionality
- [ ] Health endpoint responds: `https://entityaiapi.azurewebsites.net/api/version`
- [ ] Can create entity via API
- [ ] Can upload embedding
- [ ] Can search entities
- [ ] Integration tests pass

### GitHub
- [ ] Repository renamed to EntityMatchingAPI
- [ ] GitHub Actions workflows execute
- [ ] Auto-deployment works on push to master

### SDKs
- [ ] EntityMatching.SDK published on NuGet
- [ ] @entitymatching/sdk published on npm
- [ ] Old packages unpublished

### Cleanup
- [ ] ProfileMatchingAPI folder deleted
- [ ] Old Azure resources (`profilesai`) deleted

---

## Quick Commands Reference

```bash
# Build and test locally
cd D:\Development\Main\EntityMatchingAPI
dotnet restore
dotnet build
dotnet test --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~Demo"

# Check Azure resources
az group list --output table
az functionapp list --resource-group entitymatchingai --output table

# Deploy manually
cd EntityMatching.Functions
func azure functionapp publish entityaiapi

# Test endpoints
curl https://entityaiapi.azurewebsites.net/api/version
curl https://entitymatching-apim.azure-api.net/v1/entities
```

---

## Files to Reference

- **Detailed status:** `RENAME_COMPLETION_STATUS.md`
- **Implementation summary:** `IMPLEMENTATION_SUMMARY.md`
- **Original plan:** Implementation plan from plan mode

---

## Need Help?

1. **Build issues:** Check `dotnet build` output
2. **Azure issues:** Check `az` command output, verify credentials
3. **Test failures:** Check `dotnet test --verbosity detailed`
4. **Git issues:** Check `git status`, verify remote URL

---

**Ready to proceed?** Start with Step 1 (GitHub rename) - it's quick and has no dependencies!

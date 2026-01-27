# EntityMatchingAPI - Azure Deployment Guide

## Architecture Overview

```
Internet → Azure API Management → Azure Functions → Cosmos DB
           (api.bystorm.com)      (EntityMatching)   (Serverless)
```

## Deployment Steps

### Phase 1: Deploy Azure Functions (Backend)

#### 1.1 Create Azure Resources

```bash
# Login to Azure
az login

# Create resource group
az group create --name EntityMatchingRG --location eastus

# Create storage account (required for Functions)
az storage account create \
  --name EntityMatchingstorage \
  --resource-group EntityMatchingRG \
  --location eastus \
  --sku Standard_LRS

# Create Cosmos DB account (SERVERLESS mode)
az cosmosdb create \
  --name EntityMatchingdb \
  --resource-group EntityMatchingRG \
  --locations regionName=eastus \
  --capabilities EnableServerless

# Create database
az cosmosdb sql database create \
  --account-name EntityMatchingdb \
  --resource-group EntityMatchingRG \
  --name EntityMatchingDB

# Create containers (no throughput for serverless!)
az cosmosdb sql container create \
  --account-name EntityMatchingdb \
  --resource-group EntityMatchingRG \
  --database-name EntityMatchingDB \
  --name profiles \
  --partition-key-path "/id"

az cosmosdb sql container create \
  --account-name EntityMatchingdb \
  --resource-group EntityMatchingRG \
  --database-name EntityMatchingDB \
  --name conversations \
  --partition-key-path "/profileId"

az cosmosdb sql container create \
  --account-name EntityMatchingdb \
  --resource-group EntityMatchingRG \
  --database-name EntityMatchingDB \
  --name embeddings \
  --partition-key-path "/profileId"

# Create Azure Function App
az functionapp create \
  --resource-group EntityMatchingRG \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --name EntityMatchingapi \
  --storage-account EntityMatchingstorage
```

#### 1.2 Setup Azure KeyVault for Secrets Management

**IMPORTANT**: Always use KeyVault to store API keys and secrets. Never store them directly in application settings.

```bash
# Create KeyVault
az keyvault create \
  --name EntityMatching-kv \
  --resource-group EntityMatchingRG \
  --location eastus \
  --enable-rbac-authorization false

# Enable System-Assigned Managed Identity on Function App
az functionapp identity assign \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG

# Get the Managed Identity Principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG \
  --query principalId -o tsv)

echo "Managed Identity Principal ID: $PRINCIPAL_ID"

# Grant Function App access to KeyVault
az keyvault set-policy \
  --name EntityMatching-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Add secrets to KeyVault
az keyvault secret set \
  --vault-name EntityMatching-kv \
  --name OpenAI-ApiKey \
  --value "<YOUR_OPENAI_API_KEY>"

az keyvault secret set \
  --vault-name EntityMatching-kv \
  --name Groq-ApiKey \
  --value "<YOUR_GROQ_API_KEY>"

az keyvault secret set \
  --vault-name EntityMatching-kv \
  --name UsCensus-ApiKey \
  --value "<YOUR_USCENSUS_API_KEY>"

az keyvault secret set \
  --vault-name EntityMatching-kv \
  --name WalkScore-ApiKey \
  --value "<YOUR_WALKSCORE_API_KEY>"

# Get Cosmos DB connection string and store in KeyVault
COSMOS_CONNECTION=$(az cosmosdb keys list \
  --name EntityMatchingdb \
  --resource-group EntityMatchingRG \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv)

az keyvault secret set \
  --vault-name EntityMatching-kv \
  --name CosmosConnectionString \
  --value "$COSMOS_CONNECTION"
```

#### 1.3 Configure Application Settings with KeyVault References

```bash
# Set application settings to use KeyVault references
az functionapp config appsettings set \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG \
  --settings \
    "CosmosDb__ConnectionString=@Microsoft.KeyVault(VaultName=EntityMatching-kv;SecretName=CosmosConnectionString)" \
    "CosmosDb__DatabaseId=EntityMatchingDB" \
    "CosmosDb__EntitiesContainerId=profiles" \
    "CosmosDb__ConversationsContainerId=conversations" \
    "CosmosDb__EmbeddingsContainerId=embeddings" \
    "OpenAI__ApiKey=@Microsoft.KeyVault(VaultName=EntityMatching-kv;SecretName=OpenAI-ApiKey)" \
    "OpenAI__EmbeddingModel=text-embedding-3-small" \
    "OpenAI__EmbeddingDimensions=1536" \
    "ApiKeys__Groq=@Microsoft.KeyVault(VaultName=EntityMatching-kv;SecretName=Groq-ApiKey)" \
    "UsCensus__ApiKey=@Microsoft.KeyVault(VaultName=EntityMatching-kv;SecretName=UsCensus-ApiKey)" \
    "WalkScore__ApiKey=@Microsoft.KeyVault(VaultName=EntityMatching-kv;SecretName=WalkScore-ApiKey)" \
    "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
    "EMBEDDING_INFRASTRUCTURE_ENABLED=true"

# Restart Function App to apply changes
az functionapp restart \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG
```

**Verify KeyVault References in Azure Portal:**
1. Go to Function App → Settings → Configuration
2. Check that settings show green checkmark (✅) and Source: "Key Vault Reference"
3. If red X (❌), see [KeyVault Configuration Fix Guide](./KEYVAULT_CONFIGURATION_FIX.md)

#### 1.4 Deploy Functions

```bash
# Build and publish
cd D:\Development\Main\EntityMatchingAPI\EntityMatching.Functions
dotnet publish -c Release

# Deploy to Azure (from publish folder)
cd bin\Release\net8.0\publish
func azure functionapp publish EntityMatchingapi
```

#### 1.5 Verify Deployment

```bash
# Get Function App URL
az functionapp show \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG \
  --query "defaultHostName" -o tsv

# Test endpoint (replace with your URL)
curl https://EntityMatchingapi.azurewebsites.net/api/v1/entities?userId=test
```

---

### Phase 2: Setup Azure API Management

#### 2.1 Create APIM Instance

```bash
# Create APIM (Developer tier for testing, Standard/Premium for production)
az apim create \
  --name EntityMatchingapim \
  --resource-group EntityMatchingRG \
  --location eastus \
  --publisher-email admin@bystorm.com \
  --publisher-name "ByStorm" \
  --sku-name Developer

# This takes 30-45 minutes to provision
```

#### 2.2 Import Azure Functions as API

```bash
# Get Functions App resource ID
FUNCTIONS_ID=$(az functionapp show \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG \
  --query id -o tsv)

# Import Functions into APIM
az apim api import \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --path /v1 \
  --api-id profile-matching-api \
  --display-name "Profile Matching API" \
  --protocols https \
  --specification-format OpenApi \
  --specification-url "https://EntityMatchingapi.azurewebsites.net/api/swagger.json"
```

#### 2.3 Configure Custom Domain

```bash
# Upload SSL certificate
az apim certificate create \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --certificate-id api-bystorm-cert \
  --certificate-file /path/to/api.bystorm.com.pfx \
  --certificate-password <cert-password>

# Create custom domain
az apim custom-domain create \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --hostname-type Proxy \
  --hostname api.bystorm.com \
  --certificate-id api-bystorm-cert

# Update DNS (at your domain registrar)
# Create CNAME record: api.bystorm.com -> EntityMatchingapim.azure-api.net
```

#### 2.4 Setup API Policies

**Rate Limiting Policy**:
```xml
<policies>
    <inbound>
        <base />
        <!-- Validate API key -->
        <check-header name="Ocp-Apim-Subscription-Key" failed-check-httpcode="401" />

        <!-- Rate limit: 100 requests per minute per subscription -->
        <rate-limit-by-key calls="100"
                          renewal-period="60"
                          counter-key="@(context.Subscription.Id)" />

        <!-- Monthly quota based on subscription tier -->
        <quota-by-key calls="10000"
                     renewal-period="2592000"
                     counter-key="@(context.Subscription.Id)" />

        <!-- CORS -->
        <cors allow-credentials="true">
            <allowed-origins>
                <origin>https://datenightplanner.com</origin>
                <origin>https://localhost:5001</origin>
            </allowed-origins>
            <allowed-methods>
                <method>GET</method>
                <method>POST</method>
                <method>PUT</method>
                <method>DELETE</method>
                <method>OPTIONS</method>
            </allowed-methods>
            <allowed-headers>
                <header>*</header>
            </allowed-headers>
        </cors>
    </inbound>

    <backend>
        <base />
    </backend>

    <outbound>
        <base />
        <!-- Cache profile responses for 5 minutes -->
        <cache-store duration="300" />
    </outbound>

    <on-error>
        <base />
    </on-error>
</policies>
```

Apply via Azure Portal or CLI:
```bash
# Save policy to file: apim-policy.xml
# Apply to API
az apim api policy create \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --api-id profile-matching-api \
  --policy-file apim-policy.xml
```

---

### Phase 3: Configure Subscriptions & API Keys

#### 3.1 Create Products (Subscription Tiers)

```bash
# Free Tier
az apim product create \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --product-id free-tier \
  --product-name "Free Tier" \
  --description "5,000 requests/month" \
  --subscription-required true \
  --approval-required false \
  --state published

# Premium Tier
az apim product create \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --product-id premium-tier \
  --product-name "Premium Tier" \
  --description "100,000 requests/month" \
  --subscription-required true \
  --approval-required true \
  --state published

# Add API to products
az apim product api add \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --product-id free-tier \
  --api-id profile-matching-api
```

#### 3.2 Create Subscription for Date Night

```bash
# Create subscription for Date Night app
az apim subscription create \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --subscription-id datenightplanner-sub \
  --scope /products/premium-tier \
  --display-name "Date Night Planner"

# Get API keys
az apim subscription show \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --subscription-id datenightplanner-sub \
  --query "[primaryKey, secondaryKey]"
```

---

### Phase 4: Update Date Night to Use APIM

Update Date Night's `ProfileApiClient` configuration:

```csharp
// Before (direct to Azure Functions):
BaseUrl = "https://EntityMatchingapi.azurewebsites.net/api/v1"

// After (via APIM):
BaseUrl = "https://api.bystorm.com/v1"
Headers.Add("Ocp-Apim-Subscription-Key", "<your-api-key>");
```

---

## Testing the Deployment

### Test via APIM

```bash
# Get your subscription key from Azure Portal

# List profiles
curl -X GET "https://api.bystorm.com/v1/profiles?userId=test-user" \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY"

# Create profile
curl -X POST "https://api.bystorm.com/v1/profiles" \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "bio": "Test profile",
    "ownedByUserId": "user-123"
  }'
```

---

## Monitoring & Analytics

### View API Analytics in Azure Portal

1. Navigate to API Management instance
2. Go to "Analytics" blade
3. View:
   - Request count by subscription
   - Response times
   - Error rates
   - Geographic distribution

### Application Insights Integration

```bash
# Create Application Insights
az monitor app-insights component create \
  --app EntityMatching-insights \
  --resource-group EntityMatchingRG \
  --location eastus

# Link to APIM
az apim logger create \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --logger-id appinsights-logger \
  --logger-type applicationInsights \
  --instrumentation-key <insights-key>
```

---

## Cost Estimation

**Azure Resources**:
- Azure Functions (Consumption): ~$10-50/month
- Cosmos DB (Serverless): ~$25-100/month
- Azure API Management (Developer): ~$50/month
- **Total**: ~$85-200/month

**Production (Standard APIM)**:
- APIM (Standard): ~$700/month
- Other resources: ~$100-200/month
- **Total**: ~$800-900/month

---

## Security Best Practices

1. **Never expose Azure Functions URLs directly** - Always go through APIM
2. **Use API keys** - Require subscription keys for all requests
3. **Enable IP filtering** - Whitelist known IPs if possible
4. **Use Azure Key Vault** - Store ALL secrets in KeyVault with Managed Identity
   - ✅ Cosmos connection strings
   - ✅ API keys (OpenAI, Groq, UsCensus, WalkScore)
   - ✅ Google Maps API keys
   - ✅ Any third-party service credentials
   - See [KeyVault Configuration Fix Guide](./KEYVAULT_CONFIGURATION_FIX.md) for setup
5. **Enable HTTPS only** - Redirect HTTP to HTTPS
6. **Monitor rate limits** - Alert on suspicious activity
7. **Rotate secrets regularly** - Update KeyVault secrets every 90 days

---

## Troubleshooting

### Functions not accessible via APIM

```bash
# Check APIM backend configuration
az apim backend show \
  --resource-group EntityMatchingRG \
  --service-name EntityMatchingapim \
  --backend-id EntityMatchingapi

# Verify Functions are running
az functionapp list-functions \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG
```

### CORS Issues

- Ensure CORS policy is configured in APIM (not just in Functions)
- APIM CORS takes precedence over Functions CORS

### Rate Limit Issues

- Check subscription quotas in Azure Portal
- Review APIM policy configuration
- Consider upgrading subscription tier

### KeyVault Configuration Issues

**Error: "OpenAI:ApiKey configuration is required"**

This means the Function App can't access KeyVault secrets. To fix:

```bash
# 1. Enable Managed Identity
az functionapp identity assign \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG

# 2. Grant KeyVault access
PRINCIPAL_ID=$(az functionapp identity show \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG \
  --query principalId -o tsv)

az keyvault set-policy \
  --name EntityMatching-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# 3. Restart Function App
az functionapp restart \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG

# 4. Verify in Portal
# Go to Function App → Configuration
# Check for green checkmarks (✅) next to KeyVault references
```

**Detailed troubleshooting**: See [KeyVault Configuration Fix Guide](./KEYVAULT_CONFIGURATION_FIX.md)

---

## Next Steps

1. ✅ Deploy Functions to Azure
2. ✅ Setup APIM instance
3. ✅ Configure custom domain
4. ✅ Create subscription tiers
5. ⏭️ Setup authentication (OAuth 2.0 / Microsoft Entra ID)
6. ⏭️ Migrate Date Night to use APIM endpoints
7. ⏭️ Launch marketplace portal for other clients

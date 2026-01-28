# EntityMatching Azure Infrastructure Deployment Script
# This script creates all required Azure resources for EntityMatchingAPI

param(
    [string]$ResourceGroup = "entitymatchingai",
    [string]$Location = "eastus",
    [bool]$CreateApim = $false,  # APIM takes 45+ minutes, set to $true if needed
    [bool]$WhatIf = $false       # Set to $true for dry run
)

$ErrorActionPreference = "Stop"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "EntityMatching Infrastructure Setup" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Resource names
$cosmosDbName = "entitymatchingdb"
$databaseName = "EntityMatchingDB"
$storageAccountName = "entitymatchstore"  # Must be lowercase, no hyphens
$functionAppName = "entityaiapi"
$keyVaultName = "entitymatching-kv"
$apimName = "entitymatching-apim"

# Verify Azure login
Write-Host "[1/9] Verifying Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Please run: az login" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "  ✓ Subscription: $($account.name)" -ForegroundColor Green
Write-Host ""

if ($WhatIf) {
    Write-Host "DRY RUN MODE - No resources will be created" -ForegroundColor Yellow
    Write-Host ""
}

# Step 1: Create Resource Group
Write-Host "[2/9] Creating Resource Group..." -ForegroundColor Yellow
if (-not $WhatIf) {
    az group create --name $ResourceGroup --location $Location | Out-Null
}
Write-Host "  ✓ Resource Group: $ResourceGroup" -ForegroundColor Green
Write-Host ""

# Step 2: Create Cosmos DB
Write-Host "[3/9] Creating Cosmos DB (this takes 2-3 minutes)..." -ForegroundColor Yellow
if (-not $WhatIf) {
    az cosmosdb create `
        --name $cosmosDbName `
        --resource-group $ResourceGroup `
        --locations regionName=$Location `
        --capabilities EnableServerless `
        --default-consistency-level Session | Out-Null

    az cosmosdb sql database create `
        --account-name $cosmosDbName `
        --resource-group $ResourceGroup `
        --name $databaseName | Out-Null
}
Write-Host "  ✓ Cosmos DB Account: $cosmosDbName" -ForegroundColor Green
Write-Host "  ✓ Database: $databaseName" -ForegroundColor Green
Write-Host ""

# Step 3: Create Cosmos DB Containers
Write-Host "[4/9] Creating Cosmos DB Containers..." -ForegroundColor Yellow
$containers = @("entities", "conversations", "embeddings", "ratings", "reputations", "matches")
foreach ($container in $containers) {
    if (-not $WhatIf) {
        az cosmosdb sql container create `
            --account-name $cosmosDbName `
            --resource-group $ResourceGroup `
            --database-name $databaseName `
            --name $container `
            --partition-key-path "/id" | Out-Null
    }
    Write-Host "  ✓ Container: $container" -ForegroundColor Green
}
Write-Host ""

# Step 4: Create Storage Account
Write-Host "[5/9] Creating Storage Account..." -ForegroundColor Yellow
if (-not $WhatIf) {
    az storage account create `
        --name $storageAccountName `
        --resource-group $ResourceGroup `
        --location $Location `
        --sku Standard_LRS | Out-Null
}
Write-Host "  ✓ Storage Account: $storageAccountName" -ForegroundColor Green
Write-Host ""

# Step 5: Create Function App
Write-Host "[6/9] Creating Function App..." -ForegroundColor Yellow
if (-not $WhatIf) {
    az functionapp create `
        --resource-group $ResourceGroup `
        --consumption-plan-location $Location `
        --runtime dotnet-isolated `
        --runtime-version 8 `
        --functions-version 4 `
        --name $functionAppName `
        --storage-account $storageAccountName | Out-Null
}
Write-Host "  ✓ Function App: $functionAppName" -ForegroundColor Green
Write-Host "  ✓ Runtime: .NET 8 (isolated)" -ForegroundColor Green
Write-Host ""

# Step 6: Create Key Vault
Write-Host "[7/9] Creating Key Vault..." -ForegroundColor Yellow
if (-not $WhatIf) {
    az keyvault create `
        --name $keyVaultName `
        --resource-group $ResourceGroup `
        --location $Location | Out-Null
}
Write-Host "  ✓ Key Vault: $keyVaultName" -ForegroundColor Green
Write-Host ""

# Step 7: Configure Managed Identity & Key Vault Access
Write-Host "[8/9] Configuring Managed Identity..." -ForegroundColor Yellow
if (-not $WhatIf) {
    az functionapp identity assign `
        --name $functionAppName `
        --resource-group $ResourceGroup | Out-Null

    $principalId = (az functionapp identity show `
        --name $functionAppName `
        --resource-group $ResourceGroup `
        --query principalId -o tsv)

    # Wait a moment for identity to propagate
    Start-Sleep -Seconds 5

    az keyvault set-policy `
        --name $keyVaultName `
        --object-id $principalId `
        --secret-permissions get list | Out-Null
}
Write-Host "  ✓ Managed Identity enabled" -ForegroundColor Green
Write-Host "  ✓ Key Vault access configured" -ForegroundColor Green
Write-Host ""

# Step 8: Store Secrets in Key Vault
Write-Host "[9/9] Storing secrets in Key Vault..." -ForegroundColor Yellow
if (-not $WhatIf) {
    # Get Cosmos connection string
    $cosmosConnString = (az cosmosdb keys list `
        --name $cosmosDbName `
        --resource-group $ResourceGroup `
        --type connection-strings `
        --query "connectionStrings[0].connectionString" -o tsv)

    az keyvault secret set `
        --vault-name $keyVaultName `
        --name "CosmosDbConnectionString" `
        --value $cosmosConnString | Out-Null

    Write-Host "  ✓ CosmosDbConnectionString stored" -ForegroundColor Green

    # Check if API keys exist in local file
    $apiKeysFile = "dont check in.txt"
    if (Test-Path $apiKeysFile) {
        Write-Host "  → Found local API keys file, importing..." -ForegroundColor Cyan
        $apiKeys = Get-Content $apiKeysFile

        foreach ($line in $apiKeys) {
            if ($line -match "^(.+?)\s*:\s*(.+)$") {
                $keyName = $matches[1].Trim() -replace "__", "-" -replace "\.", "-"
                $keyValue = $matches[2].Trim()

                # Skip Key Vault references
                if ($keyValue -notlike "*@Microsoft.KeyVault*") {
                    az keyvault secret set `
                        --vault-name $keyVaultName `
                        --name $keyName `
                        --value $keyValue | Out-Null
                    Write-Host "  ✓ $keyName stored" -ForegroundColor Green
                }
            }
        }
    } else {
        Write-Host "  ⚠ No API keys file found. You'll need to add these manually:" -ForegroundColor Yellow
        Write-Host "    - OpenAIKey" -ForegroundColor Yellow
        Write-Host "    - GroqApiKey" -ForegroundColor Yellow
        Write-Host "    - GoogleMapsApiKey" -ForegroundColor Yellow
        Write-Host "    - UsCensusApiKey" -ForegroundColor Yellow
        Write-Host "    - WalkScoreApiKey" -ForegroundColor Yellow
    }
}
Write-Host ""

# Step 9: Configure Function App Settings
Write-Host "[10/10] Configuring Function App settings..." -ForegroundColor Yellow
if (-not $WhatIf) {
    $kvUri = "https://$keyVaultName.vault.azure.net/secrets"

    az functionapp config appsettings set `
        --name $functionAppName `
        --resource-group $ResourceGroup `
        --settings `
            "CosmosDb__ConnectionString=@Microsoft.KeyVault(SecretUri=$kvUri/CosmosDbConnectionString/)" `
            "CosmosDb__DatabaseId=$databaseName" `
            "CosmosDb__EntitiesContainerId=entities" `
            "CosmosDb__ConversationsContainerId=conversations" `
            "CosmosDb__EmbeddingsContainerId=embeddings" `
            "CosmosDb__RatingsContainerId=ratings" `
            "CosmosDb__ReputationsContainerId=reputations" `
            "CosmosDb__MatchesContainerId=matches" `
            "OpenAI__ApiKey=@Microsoft.KeyVault(SecretUri=$kvUri/OpenAI-ApiKey/)" `
            "ApiKeys__Groq=@Microsoft.KeyVault(SecretUri=$kvUri/ApiKeys-Groq/)" `
            "GoogleMaps__ApiKey=@Microsoft.KeyVault(SecretUri=$kvUri/GoogleMaps-ApiKey/)" `
            "UsCensus__ApiKey=@Microsoft.KeyVault(SecretUri=$kvUri/UsCensus-ApiKey/)" `
            "WalkScore__ApiKey=@Microsoft.KeyVault(SecretUri=$kvUri/WalkScore-ApiKey/)" | Out-Null
}
Write-Host "  ✓ Function App configured with Key Vault references" -ForegroundColor Green
Write-Host ""

# Optional: Create APIM
if ($CreateApim) {
    Write-Host "[OPTIONAL] Creating API Management (this takes 45+ minutes)..." -ForegroundColor Yellow
    if (-not $WhatIf) {
        az apim create `
            --name $apimName `
            --resource-group $ResourceGroup `
            --publisher-name "ByStorm" `
            --publisher-email "admin@bystorm.com" `
            --sku-name Consumption `
            --location $Location | Out-Null
    }
    Write-Host "  ✓ APIM: $apimName" -ForegroundColor Green
    Write-Host ""
}

# Summary
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "✓ Infrastructure Setup Complete!" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resources Created:" -ForegroundColor White
Write-Host "  • Resource Group:  $ResourceGroup" -ForegroundColor White
Write-Host "  • Cosmos DB:       $cosmosDbName" -ForegroundColor White
Write-Host "  • Function App:    $functionAppName" -ForegroundColor White
Write-Host "  • Storage Account: $storageAccountName" -ForegroundColor White
Write-Host "  • Key Vault:       $keyVaultName" -ForegroundColor White
if ($CreateApim) {
    Write-Host "  • APIM:            $apimName" -ForegroundColor White
}
Write-Host ""
Write-Host "Function App URL: https://$functionAppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Deploy the Function App:" -ForegroundColor White
Write-Host "     cd EntityMatching.Functions" -ForegroundColor Gray
Write-Host "     func azure functionapp publish $functionAppName" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Test the deployment:" -ForegroundColor White
Write-Host "     curl https://$functionAppName.azurewebsites.net/api/version" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Run integration tests:" -ForegroundColor White
Write-Host "     dotnet test --filter 'FullyQualifiedName~Integration'" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $apiKeysFile)) {
    Write-Host "⚠ IMPORTANT: Add your API keys to Key Vault:" -ForegroundColor Yellow
    Write-Host "  az keyvault secret set --vault-name $keyVaultName --name OpenAI-ApiKey --value 'your-key'" -ForegroundColor Gray
    Write-Host "  az keyvault secret set --vault-name $keyVaultName --name ApiKeys-Groq --value 'your-key'" -ForegroundColor Gray
    Write-Host ""
}

if ($WhatIf) {
    Write-Host "This was a DRY RUN. Run without -WhatIf parameter to create resources." -ForegroundColor Yellow
}

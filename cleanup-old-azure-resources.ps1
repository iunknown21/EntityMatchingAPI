# Cleanup Old ProfileMatching Azure Resources
# This script safely exports configuration and deletes old resources

param(
    [string]$OldResourceGroup = "profilesai",
    [string]$ExportFolder = "old-resource-backup",
    [bool]$WhatIf = $false,
    [bool]$SkipExport = $false
)

$ErrorActionPreference = "Stop"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "ProfileMatching Cleanup Script" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Verify Azure login
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Please run: az login" -ForegroundColor Red
    exit 1
}

# Check if old resource group exists
Write-Host "Checking for old resource group: $OldResourceGroup" -ForegroundColor Yellow
$rgExists = az group exists --name $OldResourceGroup
if ($rgExists -eq "false") {
    Write-Host "✓ Resource group '$OldResourceGroup' does not exist. Nothing to clean up." -ForegroundColor Green
    exit 0
}
Write-Host "  Found resource group: $OldResourceGroup" -ForegroundColor Yellow
Write-Host ""

# Export configuration before deleting
if (-not $SkipExport -and -not $WhatIf) {
    Write-Host "Exporting configuration to: $ExportFolder" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $ExportFolder -Force | Out-Null

    # Export resource group info
    Write-Host "  → Exporting resource group details..." -ForegroundColor Cyan
    az group show --name $OldResourceGroup > "$ExportFolder/resource-group.json"

    # Export all resources in the group
    Write-Host "  → Exporting resource list..." -ForegroundColor Cyan
    az resource list --resource-group $OldResourceGroup > "$ExportFolder/resources.json"

    # Try to export specific resource configurations
    try {
        Write-Host "  → Exporting Function App settings..." -ForegroundColor Cyan
        $functionApps = az functionapp list --resource-group $OldResourceGroup --query "[].name" -o tsv
        foreach ($app in $functionApps) {
            az functionapp config appsettings list --name $app --resource-group $OldResourceGroup > "$ExportFolder/functionapp-$app-settings.json"
        }
    } catch {
        Write-Host "  ⚠ Could not export Function App settings" -ForegroundColor Yellow
    }

    try {
        Write-Host "  → Exporting APIM configuration..." -ForegroundColor Cyan
        $apimServices = az apim list --resource-group $OldResourceGroup --query "[].name" -o tsv
        foreach ($apim in $apimServices) {
            az apim api list --resource-group $OldResourceGroup --service-name $apim > "$ExportFolder/apim-$apim-apis.json"
        }
    } catch {
        Write-Host "  ⚠ Could not export APIM configuration" -ForegroundColor Yellow
    }

    try {
        Write-Host "  → Exporting Cosmos DB info..." -ForegroundColor Cyan
        $cosmosAccounts = az cosmosdb list --resource-group $OldResourceGroup --query "[].name" -o tsv
        foreach ($cosmos in $cosmosAccounts) {
            az cosmosdb show --name $cosmos --resource-group $OldResourceGroup > "$ExportFolder/cosmosdb-$cosmos-config.json"
        }
    } catch {
        Write-Host "  ⚠ Could not export Cosmos DB configuration" -ForegroundColor Yellow
    }

    Write-Host "  ✓ Configuration exported to: $ExportFolder" -ForegroundColor Green
    Write-Host ""
}

# List resources that will be deleted
Write-Host "Resources that will be deleted:" -ForegroundColor Yellow
$resources = az resource list --resource-group $OldResourceGroup --query "[].{Name:name, Type:type}" -o table
Write-Host $resources
Write-Host ""

# Confirmation prompt
if (-not $WhatIf) {
    Write-Host "⚠ WARNING: This will permanently delete all resources in '$OldResourceGroup'" -ForegroundColor Red
    Write-Host ""
    $confirmation = Read-Host "Type 'DELETE' to confirm deletion"

    if ($confirmation -ne "DELETE") {
        Write-Host "Deletion cancelled." -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""

    # Delete resource group
    Write-Host "Deleting resource group (this may take several minutes)..." -ForegroundColor Yellow
    az group delete --name $OldResourceGroup --yes --no-wait

    Write-Host "✓ Deletion initiated. Resources are being removed in the background." -ForegroundColor Green
    Write-Host ""
    Write-Host "Check deletion status with:" -ForegroundColor Cyan
    Write-Host "  az group show --name $OldResourceGroup" -ForegroundColor Gray
    Write-Host ""
    Write-Host "When deleted, you'll see: 'ResourceGroupNotFound'" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "DRY RUN MODE - No resources were deleted" -ForegroundColor Yellow
    Write-Host "Run without -WhatIf parameter to actually delete resources" -ForegroundColor Yellow
}

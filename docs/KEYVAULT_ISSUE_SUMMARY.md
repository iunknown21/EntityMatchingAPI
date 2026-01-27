# KeyVault Permission Issue - Summary

## The Problem

Your `ProcessPendingEmbeddings` function is failing with this error:

```
System.InvalidOperationException: OpenAI:ApiKey configuration is required
at EntityMatching.Infrastructure.Services.OpenAIEmbeddingService..ctor
```

## Root Cause

The Azure Function App cannot read the `OpenAI__ApiKey` from Azure KeyVault because:

1. ❌ The Function App's **Managed Identity** doesn't have permission to access KeyVault
2. ❌ KeyVault reference in Application Settings fails to resolve
3. ❌ OpenAIEmbeddingService receives `null` for the API key and throws exception

## Quick Fix (5 minutes)

Run these commands to grant access:

```bash
# 1. Enable Managed Identity (if not already enabled)
az functionapp identity assign \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG

# 2. Get the Principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG \
  --query principalId -o tsv)

# 3. Grant KeyVault access
az keyvault set-policy \
  --name EntityMatching-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# 4. Restart Function App
az functionapp restart \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG
```

## Verify the Fix

### Option 1: Azure Portal
1. Go to **Function App → Settings → Configuration**
2. Look for `OpenAI__ApiKey` setting
3. Should show **green checkmark (✅)** and Source: **"Key Vault Reference"**
4. If **red X (❌)**, permissions haven't propagated yet (wait 1-2 minutes and refresh)

### Option 2: Test the Function
1. Go to **Function App → Functions → ProcessPendingEmbeddings**
2. Click **Monitor** tab
3. Check recent invocations - should NOT show the "OpenAI:ApiKey configuration is required" error

## What We Created

I've created comprehensive documentation to prevent this issue in the future:

### 1. KeyVault Configuration Fix Guide
**File**: `docs/KEYVAULT_CONFIGURATION_FIX.md`

Complete troubleshooting guide with:
- Step-by-step fix instructions
- KeyVault setup from scratch
- Verification procedures
- Common issues and solutions
- Security best practices

### 2. Updated Deployment Guide
**File**: `docs/DEPLOYMENT.md` (updated)

Added new sections:
- **Section 1.2**: Setup Azure KeyVault for Secrets Management
- **Section 1.3**: Configure Application Settings with KeyVault References
- **Troubleshooting**: KeyVault Configuration Issues section

Now the deployment guide includes KeyVault setup as part of the initial deployment process.

## Why This Happened

Looking at your configuration:

1. **Local development works fine** because `local.settings.json` has raw API keys
2. **Azure deployment fails** because Application Settings use KeyVault references like:
   ```
   OpenAI__ApiKey=@Microsoft.KeyVault(VaultName=EntityMatching-kv;SecretName=OpenAI-ApiKey)
   ```
3. **Function can't resolve references** without proper permissions

This is a common issue when:
- Redeploying Function Apps
- Recreating resources
- Moving between resource groups
- The Managed Identity gets reset or loses permissions

## Prevention

To prevent this issue in the future:

1. ✅ **Always enable Managed Identity** when creating Function Apps
2. ✅ **Grant KeyVault access** immediately after enabling identity
3. ✅ **Verify green checkmarks** in Configuration before deploying code
4. ✅ **Document KeyVault setup** in deployment scripts
5. ✅ **Monitor KeyVault access logs** for permission denials

## Alternative (Temporary)

If you need a quick temporary fix (NOT recommended for production):

```bash
# Set raw API key directly (less secure)
az functionapp config appsettings set \
  --name EntityMatchingapi \
  --resource-group EntityMatchingRG \
  --settings \
    "OpenAI__ApiKey=<YOUR_OPENAI_API_KEY>"
```

⚠️ **Warning**: This stores the key in plain text in Application Settings. Always use KeyVault for production.

## Next Steps

1. ✅ Run the Quick Fix commands above
2. ✅ Verify green checkmarks in Azure Portal
3. ✅ Test ProcessPendingEmbeddings function
4. ✅ Review [KEYVAULT_CONFIGURATION_FIX.md](./KEYVAULT_CONFIGURATION_FIX.md) for details
5. ✅ Follow updated [DEPLOYMENT.md](./DEPLOYMENT.md) for future deployments

---

**Created**: 2026-01-22
**Issue**: ProcessPendingEmbeddings KeyVault permission error
**Status**: Documented and ready to fix

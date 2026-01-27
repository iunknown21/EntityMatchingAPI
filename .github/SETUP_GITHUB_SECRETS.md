# Setting Up GitHub Secrets for CI/CD

This guide walks you through setting up all required secrets for GitHub Actions workflows.

## Required Secrets

### 1. AZURE_CREDENTIALS (Required for Azure Functions deployment)

This is a service principal with permissions to deploy to your Azure Function App.

**Steps:**

1. **Create a service principal:**

```bash
az ad sp create-for-rbac \
  --name "github-actions-profilematching" \
  --role contributor \
  --scopes /subscriptions/09f915e1-47f8-47c7-809d-cd0e924b928b/resourceGroups/profilesai \
  --sdk-auth
```

2. **Copy the entire JSON output**, which looks like:

```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "09f915e1-47f8-47c7-809d-cd0e924b928b",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

3. **Add to GitHub:**
   - Go to: https://github.com/iunknown21/ProfileMatchingAPI/settings/secrets/actions
   - Click "New repository secret"
   - Name: `AZURE_CREDENTIALS`
   - Value: Paste the entire JSON output

### 2. NPM_TOKEN (Required for npm SDK publishing)

This allows publishing the JavaScript SDK to npm.

**Steps:**

1. **Login to npm:**

```bash
npm login
# Enter your npm credentials
```

2. **Generate an access token:**

```bash
npm token create
```

Or generate via website:
- Go to: https://www.npmjs.com/settings/YOUR_USERNAME/tokens
- Click "Generate New Token"
- Choose "Automation" type
- Copy the token

3. **Add to GitHub:**
   - Name: `NPM_TOKEN`
   - Value: Paste the npm token

### 3. NUGET_API_KEY (Required for NuGet SDK publishing)

This allows publishing the C# SDK to NuGet.org.

**Steps:**

1. **Create API key on NuGet.org:**
   - Go to: https://www.nuget.org/account/apikeys
   - Click "Create"
   - Key Name: "GitHub Actions - ProfileMatching.SDK"
   - Select Scopes: "Push" only
   - Select Packages: "ProfileMatching.SDK" (or leave as *)
   - Glob Pattern: `ProfileMatching.SDK*`
   - Click "Create"
   - **Copy the key immediately** (you won't see it again)

2. **Add to GitHub:**
   - Name: `NUGET_API_KEY`
   - Value: Paste the NuGet API key

### 4. AZURE_STATIC_WEB_APPS_API_TOKEN (Required for demo deployment)

This token is generated when you create an Azure Static Web App.

**Steps:**

**Option A: Create new Static Web App**

```bash
# Create Static Web App
az staticwebapp create \
  --name privatematch-demo \
  --resource-group profilesai \
  --location "Central US" \
  --sku Free

# Get the deployment token
az staticwebapp secrets list \
  --name privatematch-demo \
  --resource-group profilesai \
  --query "properties.apiKey" -o tsv
```

**Option B: Get token from existing Static Web App**

```bash
az staticwebapp secrets list \
  --name YOUR_STATIC_WEB_APP_NAME \
  --resource-group profilesai \
  --query "properties.apiKey" -o tsv
```

**Option C: Via Azure Portal**

1. Go to: Azure Portal → Static Web Apps → your app
2. Click "Manage deployment token"
3. Copy the token

**Add to GitHub:**
- Name: `AZURE_STATIC_WEB_APPS_API_TOKEN`
- Value: Paste the deployment token

---

## Verify Secrets Setup

Once all secrets are added, you should see them listed at:
https://github.com/iunknown21/ProfileMatchingAPI/settings/secrets/actions

### Checklist:

- [ ] `AZURE_CREDENTIALS` - Service principal JSON
- [ ] `NPM_TOKEN` - npm access token
- [ ] `NUGET_API_KEY` - NuGet API key
- [ ] `AZURE_STATIC_WEB_APPS_API_TOKEN` - Static Web Apps deployment token

---

## Testing the Setup

### Test Azure Functions Deployment

1. Make a small change to any file in `ProfileMatching.Functions/`
2. Commit and push to master:
   ```bash
   git add .
   git commit -m "test: trigger CI/CD"
   git push origin master
   ```
3. Watch the workflow: https://github.com/iunknown21/ProfileMatchingAPI/actions

### Test SDK Publishing

1. Go to: https://github.com/iunknown21/ProfileMatchingAPI/actions/workflows/publish-sdks.yml
2. Click "Run workflow"
3. Enter version: `1.0.0`
4. Select both npm and NuGet
5. Click "Run workflow"

### Test Demo Deployment

1. Make a small change to any file in `PrivateMatch.Demo/`
2. Commit and push to master
3. Watch the workflow: https://github.com/iunknown21/ProfileMatchingAPI/actions

---

## Troubleshooting

### Azure Functions Deployment Fails

**Error:** "The client '...' with object id '...' does not have authorization..."

**Solution:** The service principal needs Contributor role:

```bash
az role assignment create \
  --assignee YOUR_SERVICE_PRINCIPAL_CLIENT_ID \
  --role Contributor \
  --scope /subscriptions/09f915e1-47f8-47c7-809d-cd0e924b928b/resourceGroups/profilesai
```

### npm Publish Fails

**Error:** "You must be logged in to publish packages"

**Solution:**
1. Verify NPM_TOKEN is correct
2. Ensure token has "Automation" permissions
3. Token must not be expired

### NuGet Publish Fails

**Error:** "The API key provided is invalid..."

**Solution:**
1. Verify NUGET_API_KEY is correct
2. Check key hasn't expired on https://www.nuget.org/account/apikeys
3. Ensure key has "Push" permission

### Static Web Apps Deployment Fails

**Error:** "App Directory could not be found..."

**Solution:**
1. Verify AZURE_STATIC_WEB_APPS_API_TOKEN is correct
2. Check the Static Web App still exists in Azure
3. Ensure app_location path in workflow is correct

---

## Security Best Practices

1. **Rotate secrets regularly** (every 90 days)
2. **Use minimum required permissions** for service principals
3. **Never commit secrets** to the repository
4. **Audit secret usage** in Actions logs
5. **Revoke unused tokens** immediately

---

## Additional Resources

- [GitHub Actions Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure Service Principal Documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal)
- [npm Token Documentation](https://docs.npmjs.com/about-access-tokens)
- [NuGet API Keys](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)

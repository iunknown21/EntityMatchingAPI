# GitHub Actions Workflows

This directory contains CI/CD workflows for automated building, testing, and deployment of the ProfileMatching API and related components.

## Workflows

### 1. `azure-functions-deploy.yml` - Azure Functions Deployment

**Triggers:**
- Push to `master` or `main` branch (when Functions/Core/Infrastructure/Shared code changes)
- Manual workflow dispatch

**What it does:**
1. Builds the entire .NET solution
2. Runs all integration tests
3. Publishes Azure Functions
4. Deploys to Azure Functions App
5. Verifies deployment with health check

**Required Secrets:**
- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` - Download from Azure Portal → Function App → Get publish profile

### 2. `build-and-test.yml` - Pull Request Validation

**Triggers:**
- Pull requests to `master` or `main`
- Push to feature branches

**What it does:**
1. Builds solution in Release configuration
2. Runs all unit and integration tests
3. Generates test coverage reports
4. Uploads test results and coverage as artifacts

**Required Secrets:** None

### 3. `publish-sdks.yml` - SDK Publishing

**Triggers:**
- Manual workflow dispatch only

**Inputs:**
- `publish_npm` - Publish JavaScript SDK to npm (boolean)
- `publish_nuget` - Publish C# SDK to NuGet (boolean)
- `version` - Version to publish (e.g., "1.0.0")
- `prerelease` - Mark as prerelease (boolean)

**What it does:**
1. Builds and tests both SDKs
2. Publishes JavaScript SDK to npm
3. Publishes C# SDK to NuGet
4. Creates GitHub Release with changelog

**Required Secrets:**
- `NPM_TOKEN` - npm authentication token
- `NUGET_API_KEY` - NuGet API key

### 4. `deploy-demo.yml` - PrivateMatch Demo Deployment

**Triggers:**
- Push to `master` or `main` (when Demo or SDK code changes)
- Manual workflow dispatch

**What it does:**
1. Builds Blazor WebAssembly app
2. Publishes static files
3. Deploys to Azure Static Web Apps

**Required Secrets:**
- `AZURE_STATIC_WEB_APPS_API_TOKEN` - From Azure Static Web Apps deployment token

---

## Setting Up Secrets

### 1. Azure Functions Publish Profile

```bash
# Download from Azure Portal
az functionapp deployment list-publishing-profiles \
  --name profileaiapi \
  --resource-group profilesai \
  --xml > publish-profile.xml
```

Then add to GitHub:
- Go to: Repository → Settings → Secrets and variables → Actions
- Click "New repository secret"
- Name: `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
- Value: Paste contents of `publish-profile.xml`

### 2. NPM Token

```bash
# Login to npm
npm login

# Generate token
npm token create --read-only=false
```

Add to GitHub as secret: `NPM_TOKEN`

### 3. NuGet API Key

1. Go to: https://www.nuget.org/account/apikeys
2. Click "Create"
3. Set permissions to "Push" for ProfileMatching.SDK
4. Copy the key

Add to GitHub as secret: `NUGET_API_KEY`

### 4. Azure Static Web Apps Token

```bash
# Create Static Web App first
az staticwebapp create \
  --name privatematch-demo \
  --resource-group profilesai \
  --location "Central US"

# Get deployment token
az staticwebapp secrets list \
  --name privatematch-demo \
  --resource-group profilesai \
  --query "properties.apiKey" -o tsv
```

Add to GitHub as secret: `AZURE_STATIC_WEB_APPS_API_TOKEN`

---

## Usage Examples

### Deploy to Production

```bash
# Automatic on push to master
git push origin master

# Or trigger manually
# Go to: Actions → Deploy ProfileMatching API to Azure Functions → Run workflow
```

### Publish SDK Release

```bash
# Go to: Actions → Publish SDKs → Run workflow
# Fill in:
#   - Version: 1.0.0
#   - Publish npm: ✓
#   - Publish NuGet: ✓
#   - Prerelease: ✗
```

### Testing Pull Requests

```bash
# Automatic on PR creation
git checkout -b feature/my-feature
git commit -am "Add feature"
git push origin feature/my-feature

# Create PR on GitHub - workflow runs automatically
```

---

## Workflow Status Badges

Add these to your main README.md:

```markdown
[![Deploy to Azure](https://github.com/iunknown21/ProfileMatchingAPI/actions/workflows/azure-functions-deploy.yml/badge.svg)](https://github.com/iunknown21/ProfileMatchingAPI/actions/workflows/azure-functions-deploy.yml)

[![Build and Test](https://github.com/iunknown21/ProfileMatchingAPI/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/iunknown21/ProfileMatchingAPI/actions/workflows/build-and-test.yml)

[![Deploy Demo](https://github.com/iunknown21/ProfileMatchingAPI/actions/workflows/deploy-demo.yml/badge.svg)](https://github.com/iunknown21/ProfileMatchingAPI/actions/workflows/deploy-demo.yml)
```

---

## Troubleshooting

### Build Failures

**Issue:** Tests failing in CI but pass locally

**Solution:**
- Check if test dependencies are properly restored
- Ensure integration tests have proper configuration
- Review test output artifacts

### Deployment Failures

**Issue:** Azure Functions deployment fails

**Solution:**
- Verify publish profile is not expired
- Check Function App is running
- Review Function App logs in Azure Portal

### SDK Publishing Failures

**Issue:** npm/NuGet publish fails

**Solution:**
- Verify tokens are valid and not expired
- Check package version doesn't already exist
- Ensure package.json/csproj has correct metadata

---

## Environment Variables

All workflows use these common environment variables:

- `DOTNET_VERSION`: .NET SDK version (8.0.x)
- `NODE_VERSION`: Node.js version (20.x)
- `AZURE_FUNCTIONAPP_NAME`: Azure Functions app name (profileaiapi)

Update these in workflow files if your environment differs.

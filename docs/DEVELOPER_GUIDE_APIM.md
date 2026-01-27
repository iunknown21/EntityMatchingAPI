# Azure API Management (APIM) Developer Guide

Complete guide for developers working with Azure API Management for EntityMatching API.

## Table of Contents

1. [What is APIM?](#what-is-apim)
2. [Accessing APIM](#accessing-apim)
3. [Understanding the Setup](#understanding-the-setup)
4. [Managing APIs](#managing-apis)
5. [Subscription Keys](#subscription-keys)
6. [Testing APIs](#testing-apis)
7. [Policies](#policies)
8. [Monitoring and Debugging](#monitoring-and-debugging)
9. [Common Tasks](#common-tasks)
10. [Troubleshooting](#troubleshooting)

---

## What is APIM?

**Azure API Management (APIM)** is a gateway that sits in front of your Azure Functions. Think of it as a "smart proxy" that:

- **Authenticates requests** using subscription keys
- **Rate limits** requests per tier (Free: 100/min, Premium: 1000/min)
- **Routes traffic** to your backend (Azure Functions)
- **Caches responses** to improve performance
- **Logs and monitors** all API calls
- **Transforms requests/responses** using policies

### Why We Use APIM

**Without APIM:**
```
User → Azure Functions (direct)
```
- No rate limiting
- No subscription management
- No analytics
- Exposes function URLs directly

**With APIM:**
```
User → APIM Gateway → Azure Functions (backend)
```
- Subscription tiers (Free/Premium/Enterprise)
- Rate limiting per tier
- API analytics and monitoring
- Clean public URLs (api.bystorm.com)
- Backend can change without breaking clients

---

## Accessing APIM

### Azure Portal

1. Navigate to: https://portal.azure.com
2. Search for "API Management services"
3. Click on `EntityMatching-apim`

### APIM URLs

**Management Portal (Developer Portal):**
```
https://EntityMatching-apim.developer.azure-api.net
```
This is where API consumers can:
- Sign up for subscriptions
- Get API keys
- Test APIs interactively
- Read documentation

**Gateway URL (Production):**
```
https://EntityMatching-apim.azure-api.net
```
This is the base URL clients use to call your API.

**Full endpoint example:**
```
https://EntityMatching-apim.azure-api.net/v1/profiles
```

---

## Understanding the Setup

### API Configuration

**API Name:** `EntityMatching API`
**Path:** `/v1`
**Backend:** `https://entityaiapi.azurewebsites.net/api`

**How routing works:**
```
Client calls: https://EntityMatching-apim.azure-api.net/v1/profiles
APIM proxies to: https://entityaiapi.azurewebsites.net/api/v1/entities
```

### Subscription Products

We have 4 subscription products:

| Product | Requests/Month | Rate Limit | Requires Key? | Approval |
|---------|---------------|------------|---------------|----------|
| **Demo** | Unlimited | No limit | No | Auto |
| **Free** | 5,000 | 100/min | Yes | Auto |
| **Premium** | 100,000 | 1,000/min | Yes | Manual |
| **Enterprise** | Unlimited | 10,000/min | Yes | Manual |

**Demo Tier:**
- No subscription key required
- For testing and demo website
- No rate limits (use with caution!)

**Free Tier:**
- Auto-approved subscriptions
- Good for developers getting started
- Limited to 5,000 requests/month

**Premium Tier:**
- Requires manual approval
- For production applications
- 100,000 requests/month

**Enterprise Tier:**
- Requires manual approval and contract
- For high-volume clients
- Custom pricing

---

## Managing APIs

### View All APIs

**Azure Portal:**
1. Go to `EntityMatching-apim`
2. Left menu → **APIs**
3. See: EntityMatching API

### View Operations

**Azure Portal:**
1. APIs → EntityMatching API
2. See all operations (GET, POST, PUT, DELETE)
3. Click operation to see details

**Our wildcard operations:**
- `GET /*` - All GET requests
- `POST /*` - All POST requests
- `PUT /*` - All PUT requests
- `DELETE /*` - All DELETE requests

This means APIM accepts all requests and proxies them to the backend.

### Add New Operation (Manual)

If you need to add a specific operation:

1. APIs → EntityMatching API → **Add operation**
2. Fill in:
   - Display name: "Create Profile"
   - URL: POST `/v1/profiles`
3. Click **Save**

**Note:** We use wildcards, so manual operations are optional.

---

## Subscription Keys

### What Are Subscription Keys?

Subscription keys are API keys that authenticate requests. Each product (Free/Premium/Enterprise) has its own keys.

### Getting a Subscription Key

**As a developer testing:**

1. Go to Developer Portal: `https://EntityMatching-apim.developer.azure-api.net`
2. Click **Sign up** (or **Sign in** if you have an account)
3. Go to **Profile** → **Subscriptions**
4. Click **Add subscription**
5. Select product (e.g., "Free Tier")
6. Click **Subscribe**
7. Copy your **Primary key**

**As an admin:**

1. Azure Portal → `EntityMatching-apim`
2. Left menu → **Subscriptions**
3. Click **Add subscription**
4. Fill in:
   - Display name: "Test Subscription"
   - Product: Free Tier
   - User: (optional)
5. Click **Create**
6. Click the subscription → **Show/hide keys**
7. Copy **Primary key**

### Using Subscription Keys

**Header (Recommended):**
```bash
curl https://EntityMatching-apim.azure-api.net/v1/profiles \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY_HERE"
```

**Query Parameter (Alternative):**
```bash
curl "https://EntityMatching-apim.azure-api.net/v1/profiles?subscription-key=YOUR_KEY_HERE"
```

### Regenerating Keys

If a key is compromised:

1. Azure Portal → Subscriptions → [Your Subscription]
2. Click **Regenerate primary key** or **Regenerate secondary key**
3. Update clients with new key

**Best Practice:** Use secondary key during rotation:
1. Regenerate secondary key
2. Update half of clients to use secondary
3. Regenerate primary key
4. Update remaining clients to use new primary
5. No downtime!

---

## Testing APIs

### Azure Portal Test Console

**Built-in testing:**

1. Azure Portal → APIs → EntityMatching API
2. Select operation (e.g., `GET /*`)
3. Click **Test** tab
4. Fill in:
   - HTTP method: GET
   - URL: `/v1/profiles?userId=test`
   - Headers: `Ocp-Apim-Subscription-Key: [your key]`
5. Click **Send**
6. See response

### Developer Portal Test Console

1. Go to `https://EntityMatching-apim.developer.azure-api.net`
2. Sign in
3. Click **APIs** → EntityMatching API
4. Select operation
5. Click **Try it**
6. Fill in parameters
7. Click **Send**

### cURL

**With subscription key:**
```bash
curl -X GET \
  "https://EntityMatching-apim.azure-api.net/v1/profiles?userId=test" \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY_HERE"
```

**Demo tier (no key required):**
```bash
curl -X GET \
  "https://EntityMatching-apim.azure-api.net/v1/profiles?userId=test"
```

### Postman

**Setup:**

1. Create new request
2. Method: GET
3. URL: `https://EntityMatching-apim.azure-api.net/v1/profiles?userId=test`
4. Headers:
   - Key: `Ocp-Apim-Subscription-Key`
   - Value: `YOUR_KEY_HERE`
5. Send

**Save as collection** for reuse.

---

## Policies

### What Are Policies?

Policies are XML configurations that transform requests/responses. They run at different stages:

```
Inbound (before backend) → Backend → Outbound (after backend) → On-Error
```

### Our Policies

**API-Level Policy** (`apim-policies/api-policy.xml`):
- CORS configuration
- Backend routing
- Response caching (5 minutes)
- Error handling

**Product-Level Policies:**
- `free-tier-policy.xml`: 100 req/min, 5,000/month
- `premium-tier-policy.xml`: 1,000 req/min, 100,000/month
- `enterprise-tier-policy.xml`: 10,000 req/min, unlimited/month

### Uploading Policies

**Via Azure Portal (Manual):**

1. Azure Portal → APIs → EntityMatching API
2. Click **All operations**
3. Click **</>** icon (Code editor) in Inbound/Outbound/Backend section
4. Paste policy XML
5. Click **Save**

**For product policies:**
1. Azure Portal → Products → [Product Name]
2. Click **Policies**
3. Paste policy XML
4. Click **Save**

### Policy Examples

**Rate limiting:**
```xml
<rate-limit calls="100" renewal-period="60" />
```
Limits to 100 requests per 60 seconds.

**Quota:**
```xml
<quota calls="5000" renewal-period="2592000" />
```
Limits to 5,000 requests per 30 days (2,592,000 seconds).

**CORS:**
```xml
<cors allow-credentials="true">
    <allowed-origins>
        <origin>https://yourdomain.com</origin>
    </allowed-origins>
    <allowed-methods>
        <method>GET</method>
        <method>POST</method>
    </allowed-methods>
</cors>
```

**Backend routing:**
```xml
<set-backend-service base-url="https://entityaiapi.azurewebsites.net/api" />
```

### Testing Policy Changes

After uploading policies:

1. Test API call
2. Check response headers for rate limit info:
   ```
   X-RateLimit-Limit: 100
   X-RateLimit-Remaining: 95
   X-RateLimit-Reset: 1704758400
   ```

---

## Monitoring and Debugging

### Application Insights

APIM logs to Application Insights automatically.

**View logs:**

1. Azure Portal → `EntityMatching-apim`
2. Left menu → **Application Insights**
3. Click **View Application Insights data**

**Query logs:**
```kusto
requests
| where timestamp > ago(1h)
| where url contains "profiles"
| project timestamp, url, resultCode, duration
| order by timestamp desc
```

### Analytics Dashboard

**Built-in analytics:**

1. Azure Portal → `EntityMatching-apim`
2. Left menu → **Analytics**
3. See:
   - Request count
   - Response times
   - Error rates
   - Top APIs
   - Geographic distribution

### Trace Requests

**Enable tracing:**

1. Send request with header:
   ```
   Ocp-Apim-Trace: true
   ```
2. Response includes trace ID in header:
   ```
   Ocp-Apim-Trace-Location: https://...
   ```
3. Open trace URL to see detailed execution

**Example:**
```bash
curl -X GET \
  "https://EntityMatching-apim.azure-api.net/v1/profiles?userId=test" \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -H "Ocp-Apim-Trace: true"
```

### Live Metrics

**Real-time monitoring:**

1. Azure Portal → Application Insights
2. Left menu → **Live Metrics**
3. See requests in real-time as they happen

---

## Common Tasks

### Add a New Subscription Tier

**Example: Add "Starter" tier**

1. Azure Portal → Products → **Add**
2. Fill in:
   - Display name: "Starter Tier"
   - ID: `starter-tier`
   - Description: "10,000 requests/month, 200 req/min"
   - Requires subscription: Yes
   - Requires approval: No (auto-approve)
3. Click **Create**
4. Add EntityMatching API to product:
   - Products → Starter Tier → **APIs** → **Add**
   - Select EntityMatching API
5. Add policy:
   - Products → Starter Tier → **Policies**
   - Paste:
     ```xml
     <policies>
         <inbound>
             <base />
             <rate-limit calls="200" renewal-period="60" />
             <quota calls="10000" renewal-period="2592000" />
         </inbound>
     </policies>
     ```
6. Click **Save**

### Change Backend URL

If you deploy to a new Functions app:

1. Azure Portal → APIs → EntityMatching API → **Settings**
2. Change **Web service URL** to new backend
3. Click **Save**

**Or via policy:**
1. APIs → All operations → Policies
2. Update `<set-backend-service>`:
   ```xml
   <set-backend-service base-url="https://new-backend.azurewebsites.net/api" />
   ```

### Add Custom Domain (api.bystorm.com)

**Prerequisites:**
- DNS access to bystorm.com
- SSL certificate (or use Azure-managed)

**Steps:**

1. Add CNAME record in DNS:
   ```
   api.bystorm.com → EntityMatching-apim.azure-api.net
   ```

2. Azure Portal → `EntityMatching-apim` → **Custom domains**

3. Click **Add**

4. Fill in:
   - Hostname: `api.bystorm.com`
   - Certificate: Upload .pfx or use Azure-managed
   - Type: Gateway (not Management/Portal)

5. Click **Add**

6. Wait for DNS propagation (5-30 minutes)

7. Test: `https://api.bystorm.com/v1/profiles`

### Enable/Disable a Product

**Disable Free Tier temporarily:**

1. Azure Portal → Products → Free Tier
2. Click **Unpublish**
3. Existing subscriptions still work, but no new sign-ups

**Re-enable:**
1. Click **Publish**

### Approve Subscription Requests

For Premium/Enterprise tiers (manual approval):

1. Azure Portal → Subscriptions
2. See subscriptions with status "Submitted"
3. Click subscription → **Activate** or **Reject**

---

## Troubleshooting

### Issue: 401 Unauthorized

**Cause:** Missing or invalid subscription key

**Fix:**
1. Verify key is correct
2. Check subscription is active
3. Ensure product includes the API
4. Try regenerating key

**Test without key:**
```bash
# Should fail with 401
curl https://EntityMatching-apim.azure-api.net/v1/profiles

# Should succeed (Demo tier)
curl https://EntityMatching-apim.azure-api.net/v1/profiles
```

### Issue: 403 Forbidden

**Cause:** Rate limit exceeded or quota exhausted

**Fix:**
1. Check rate limit headers in response:
   ```
   X-RateLimit-Remaining: 0
   ```
2. Wait for renewal period
3. Upgrade to higher tier

**View quota usage:**
1. Developer Portal → Profile → Subscriptions
2. See quota usage percentage

### Issue: 404 Not Found

**Cause:** Endpoint doesn't exist or incorrect path

**Fix:**
1. Verify endpoint path: `/v1/profiles` (not `/profiles`)
2. Check API operations in APIM
3. Test backend directly:
   ```bash
   curl https://entityaiapi.azurewebsites.net/api/v1/entities
   ```

### Issue: 500 Internal Server Error

**Cause:** Backend (Azure Functions) error

**Fix:**
1. Check Application Insights logs
2. Test backend directly to isolate issue
3. Check function app logs:
   ```bash
   az functionapp log tail --name entityaiapi --resource-group entitymatchingai
   ```

### Issue: CORS Errors in Browser

**Cause:** CORS policy not configured correctly

**Fix:**
1. Check API policy includes CORS:
   ```xml
   <cors allow-credentials="true">
       <allowed-origins>
           <origin>https://yourdomain.com</origin>
       </allowed-origins>
   </cors>
   ```
2. Add your domain to allowed-origins
3. Ensure methods include OPTIONS
4. Check browser developer console for specific error

### Issue: Slow Response Times

**Cause:** Backend is slow or caching not working

**Fix:**
1. Check Application Insights for backend duration
2. Verify cache policy is applied:
   ```xml
   <cache-store duration="300" />
   ```
3. Check cache hit rate in Analytics
4. Consider increasing cache duration

---

## Best Practices

### Security

1. **Never commit subscription keys** to Git
2. **Use Key Vault** for sensitive configuration
3. **Rotate keys regularly** (quarterly)
4. **Use secondary key** during rotation (no downtime)
5. **Enable Application Insights** for security monitoring

### Performance

1. **Enable response caching** for GET requests
2. **Use CDN** for static content
3. **Monitor P95/P99 latency** in Application Insights
4. **Set appropriate timeouts** in policies (default: 120s)

### Scalability

1. **Start with Developer tier** ($50/month)
2. **Upgrade to Standard** when traffic increases
3. **Use Premium tier** for production (multi-region, VNet)
4. **Monitor RU consumption** in Cosmos DB

### Monitoring

1. **Set up alerts** for:
   - Error rate > 5%
   - P95 latency > 2 seconds
   - Rate limit hit > 80%
2. **Review analytics weekly**
3. **Check subscription quota usage**

---

## CLI Reference

### List APIs

```bash
az apim api list \
  --resource-group entitymatchingai \
  --service-name EntityMatching-apim
```

### List Products

```bash
az apim product list \
  --resource-group entitymatchingai \
  --service-name EntityMatching-apim
```

### List Subscriptions

```bash
az apim subscription list \
  --resource-group entitymatchingai \
  --service-name EntityMatching-apim
```

### Create Subscription

```bash
az apim subscription create \
  --resource-group entitymatchingai \
  --service-name EntityMatching-apim \
  --scope /products/free-tier \
  --display-name "My Test Subscription"
```

### Show Subscription Keys

```bash
az apim subscription show \
  --resource-group entitymatchingai \
  --service-name EntityMatching-apim \
  --subscription-id YOUR_SUBSCRIPTION_ID
```

---

## Resources

- **Azure APIM Docs**: https://learn.microsoft.com/en-us/azure/api-management/
- **Policy Reference**: https://learn.microsoft.com/en-us/azure/api-management/api-management-policies
- **Developer Portal**: https://EntityMatching-apim.developer.azure-api.net
- **Gateway URL**: https://EntityMatching-apim.azure-api.net

---

## Support

- **Internal Issues**: Check Azure Portal → APIM → Support
- **Policy Help**: See policy XML files in `apim-policies/`
- **API Issues**: Check backend Azure Functions logs

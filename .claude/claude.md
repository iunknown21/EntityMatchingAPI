# Claude Code Instructions for This Project

## CRITICAL: JSON Serialization in C# Azure Projects

**ALWAYS configure JSON serialization for camelCase when creating or modifying C# API projects.**

### Azure Functions (Isolated Worker Model)

When creating `Program.cs` or modifying the host builder, **ALWAYS include this configuration**:

```csharp
hostBuilder.ConfigureFunctionsWorkerDefaults(workerOptions =>
{
    // Configure System.Text.Json to use camelCase for all JSON serialization/deserialization
    workerOptions.Serializer = new Azure.Core.Serialization.JsonObjectSerializer(
        new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        });
});
```

### ASP.NET Core / Web API Projects

When creating `Program.cs`, **ALWAYS configure JSON options**:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
```

### Cosmos DB

When registering CosmosClient, **ALWAYS configure serialization**:

```csharp
var cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
});
```

### Why This Matters

- C# uses PascalCase by default (`Address`, `UserId`)
- REST APIs should use camelCase (`address`, `userId`)
- Without configuration, C# properties serialize as PascalCase
- This causes endless debugging and wasted time

### Request/Response Models

- **DO NOT** manually add `[JsonProperty]` or `[JsonPropertyName]` attributes to every property
- **DO** configure global serialization settings once in Program.cs
- Let the global configuration handle all casing automatically

### When Creating New Projects

1. Set up JSON serialization configuration FIRST (in Program.cs)
2. Test with a simple endpoint that returns an object
3. Verify response uses camelCase in Postman
4. Then proceed with the rest of the implementation

### Dependency Injection for External APIs

When registering services that need HttpClient:

- **ALWAYS** use `IHttpClientFactory.CreateClient()` to create HttpClient instances
- **DO NOT** register services with constructors that take HttpClient without providing it

Example:
```csharp
services.AddScoped<IMyService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<MyService>>();
    return new MyService(httpClient, config, logger);
});
```

## Project Conventions

- Use lowercase JSON property names in API documentation and examples
- Keep C# code using PascalCase (standard C# convention)
- Let the serialization configuration bridge the gap automatically

## Azure Functions Reserved Keywords

### CRITICAL: 'admin' is a Reserved Route Prefix

**NEVER use 'admin' as a route prefix in Azure Functions.**

Azure Functions reserves `/admin/*` for internal management endpoints:
- `/admin/functions/{functionName}` - Manual function triggers
- `/admin/host/*` - Host management
- `/admin/vfs/*` - Virtual file system

❌ **Will NOT work:**
```csharp
[HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/embeddings/status")]
```

✅ **Use alternative prefixes:**
```csharp
[HttpTrigger(AuthorizationLevel.Function, "get", Route = "super/embeddings/status")]
// or "api-admin", "management", "internal", etc.
```

**Symptoms of using 'admin':**
- Functions appear in Azure CLI function list
- Routes show correct InvokeUrlTemplate
- But all requests return 404 Not Found
- No error messages in logs

## Documentation
All Markdown files belong in the docs subfolder
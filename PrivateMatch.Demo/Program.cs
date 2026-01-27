using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrivateMatch.Demo;
using EntityMatching.SDK;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register EntityMatching SDK
builder.Services.AddScoped(sp => new ProfileMatchingClient(new ProfileMatchingClientOptions
{
    // Use APIM gateway (demo tier - no subscription key required)
    BaseUrl = "https://EntityMatching-apim.azure-api.net/v1",
    ApiKey = "", // Demo tier doesn't require subscription key
    OpenAIKey = builder.Configuration["OpenAI:ApiKey"] ?? ""
}));

await builder.Build().RunAsync();

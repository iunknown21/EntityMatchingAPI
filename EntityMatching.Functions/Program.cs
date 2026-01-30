using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Search;
using EntityMatching.Infrastructure.Services;
using System;
using System.Reflection;

namespace EntityMatching.Functions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== EntityMatching Functions Starting ===");

            // Version and build information for deployment verification
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var buildDate = new System.IO.FileInfo(assembly.Location).LastWriteTime;
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            Console.WriteLine($"Version: {version}");
            Console.WriteLine($"Informational Version: {informationalVersion ?? "N/A"}");
            Console.WriteLine($"Build Date: {buildDate:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Build Date (Ticks): {buildDate.Ticks}");
            Console.WriteLine($"Args: {string.Join(", ", args)}");
            Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");

            try
            {
                Console.WriteLine("Creating HostBuilder...");
                var hostBuilder = new HostBuilder();

                Console.WriteLine("Configuring FunctionsWorkerDefaults...");
                hostBuilder.ConfigureFunctionsWorkerDefaults(workerOptions =>
                {
                    // Configure System.Text.Json to use camelCase for all JSON serialization/deserialization
                    workerOptions.Serializer = new Azure.Core.Serialization.JsonObjectSerializer(
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true, // Accept any casing on input
                            WriteIndented = false
                        });
                });

                Console.WriteLine("Configuring Services...");
                hostBuilder.ConfigureServices((context, services) =>
                {
                    try
                    {
                        Console.WriteLine("  Inside ConfigureServices callback");

                        // Configuration
                        var configuration = context.Configuration;
                        Console.WriteLine("  Configuration loaded");

                        // Cosmos DB Client (Singleton)
                        Console.WriteLine("  Registering Cosmos DB client...");
                        services.AddSingleton(sp =>
                        {
                            try
                            {
                                var cosmosConnectionString = configuration["CosmosDb:ConnectionString"]
                                    ?? configuration["CosmosDb__ConnectionString"];
                                Console.WriteLine("    Creating CosmosClient...");
                                return new CosmosClient(cosmosConnectionString, new CosmosClientOptions
                                {
                                    SerializerOptions = new CosmosSerializationOptions
                                    {
                                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ERROR creating CosmosClient: {ex.Message}");
                                Console.WriteLine($"    Stack trace: {ex.StackTrace}");
                                throw;
                            }
                        });

                        // HttpClient for external API calls
                        Console.WriteLine("  Registering HttpClient...");
                        services.AddHttpClient();

                        // Service registrations
                        Console.WriteLine("  Registering EntityService...");
                        services.AddScoped<IEntityService>(sp =>
                        {
                            var cosmosClient = sp.GetRequiredService<CosmosClient>();
                            var databaseId = configuration["CosmosDb:DatabaseId"] ?? configuration["CosmosDb__DatabaseId"];
                            var containerId = configuration["CosmosDb:EntitiesContainerId"] ?? configuration["CosmosDb__EntitiesContainerId"] ?? "entities";
                            var logger = sp.GetRequiredService<ILogger<EntityService>>();
                            return new EntityService(cosmosClient, databaseId!, containerId, logger);
                        });

                        Console.WriteLine("  Registering ConversationService...");
                        services.AddScoped<IConversationService>(sp =>
                        {
                            var cosmosClient = sp.GetRequiredService<CosmosClient>();
                            var config = sp.GetRequiredService<IConfiguration>();
                            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                            var logger = sp.GetRequiredService<ILogger<ConversationService>>();
                            return new ConversationService(cosmosClient, config, httpClient, logger);
                        });

                        // Entity Summary Service with Strategy Pattern
                        Console.WriteLine("  Registering Entity Summary Strategies...");
                        services.AddScoped<IEntitySummaryStrategy, Infrastructure.Services.SummaryStrategies.PersonSummaryStrategy>();
                        services.AddScoped<IEntitySummaryStrategy, Infrastructure.Services.SummaryStrategies.JobSummaryStrategy>();
                        services.AddScoped<IEntitySummaryStrategy, Infrastructure.Services.SummaryStrategies.CareerSummaryStrategy>();
                        services.AddScoped<IEntitySummaryStrategy, Infrastructure.Services.SummaryStrategies.MajorSummaryStrategy>();

                        Console.WriteLine("  Registering EntitySummaryService...");
                        services.AddScoped<IEntitySummaryService, EntitySummaryService>();

                        Console.WriteLine("  Registering OpenAIEmbeddingService...");
                        services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();

                        Console.WriteLine("  Registering EmbeddingStorageService...");
                        services.AddScoped<IEmbeddingStorageService, EmbeddingStorageService>();

                        Console.WriteLine("  Registering SimilaritySearchService...");
                        services.AddScoped<ISimilaritySearchService, SimilaritySearchService>();

                        Console.WriteLine("  Registering AttributeFilterService...");
                        services.AddScoped<IAttributeFilterService, AttributeFilterService>();

                        // Mutual Matching Service
                        Console.WriteLine("  Registering MutualMatchService...");
                        services.AddScoped<IMutualMatchService, MutualMatchService>();

                        // Profile-based search services (events, gifts, jobs, etc.)
                        Console.WriteLine("  Registering GroqWebSearchService...");
                        services.AddScoped<IWebSearchService>(sp =>
                        {
                            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                            var config = sp.GetRequiredService<IConfiguration>();
                            var logger = sp.GetRequiredService<ILogger<GroqWebSearchService>>();
                            return new GroqWebSearchService(httpClient, config, logger);
                        });

                        Console.WriteLine("  Registering EventSearchStrategy...");
                        services.AddScoped<IThingSearchStrategy<EventSearchParams, Event>, EventSearchStrategy>();

                        Console.WriteLine("  Registering EventDiscoveryService...");
                        services.AddScoped<IThingDiscoveryService<EventSearchParams, Event>, EventDiscoveryService>();

                        // Reputation service
                        Console.WriteLine("  Registering ReputationService...");
                        services.AddScoped<IReputationService>(sp =>
                        {
                            var cosmosClient = sp.GetRequiredService<CosmosClient>();
                            var databaseId = configuration["CosmosDb:DatabaseId"] ?? configuration["CosmosDb__DatabaseId"];
                            var ratingsContainerId = "ratings";
                            var reputationsContainerId = "reputations";
                            var logger = sp.GetRequiredService<ILogger<ReputationService>>();
                            return new ReputationService(cosmosClient, databaseId!, ratingsContainerId, reputationsContainerId, logger);
                        });

                        // Match service
                        Console.WriteLine("  Registering MatchService...");
                        services.AddScoped<IMatchService>(sp =>
                        {
                            var cosmosClient = sp.GetRequiredService<CosmosClient>();
                            var databaseId = configuration["CosmosDb:DatabaseId"] ?? configuration["CosmosDb__DatabaseId"];
                            var containerId = "matches";
                            var logger = sp.GetRequiredService<ILogger<MatchService>>();
                            return new MatchService(cosmosClient, databaseId!, containerId, logger);
                        });

                        Console.WriteLine("  All services registered successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ERROR in ConfigureServices: {ex.Message}");
                        Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                        throw;
                    }
                });

                Console.WriteLine("Building host...");
                var host = hostBuilder.Build();

                Console.WriteLine("Host built successfully, calling Run()...");
                host.Run();
                Console.WriteLine("Host.Run() completed normally");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }
    }
}

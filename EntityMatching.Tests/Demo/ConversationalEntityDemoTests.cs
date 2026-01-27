using Xunit;
using Xunit.Abstractions;
using EntityMatching.Core.Interfaces;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Shared.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Tests.Demo
{
    /// <summary>
    /// Demonstrates conversational profile building using natural language
    /// This is the key differentiator: Users talk naturally instead of filling forms
    /// </summary>
    public class ConversationalEntityDemoTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly IConfiguration _configuration;
        private CosmosClient? _cosmosClient;
        private IEntityService? _profileService;
        private IConversationService? _conversationService;
        private IEntitySummaryService? _profileSummaryService;
        private IEmbeddingService? _embeddingService;
        private IEmbeddingStorageService? _embeddingStorageService;
        private readonly List<string> _testProfileIds = new();
        private readonly string _demoUserId = $"demo-user-{Guid.NewGuid():N}";

        public ConversationalEntityDemoTests(ITestOutputHelper output)
        {
            _output = output;

            // Load configuration from testsettings.json (same as LargeScaleSearchDemoTests)
            var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(testSettingsPath, optional: false)
                .Build();

            var cosmosConnectionString = _configuration["CosmosDb:ConnectionString"];
            var openAiKey = _configuration["OpenAI:ApiKey"];
            var groqApiKey = _configuration["ApiKeys:Groq"];

            if (string.IsNullOrEmpty(cosmosConnectionString) || cosmosConnectionString.Contains("YOUR_"))
            {
                throw new InvalidOperationException("Cosmos DB connection string not configured in testsettings.json");
            }

            if (string.IsNullOrEmpty(openAiKey) || openAiKey.Contains("YOUR_"))
            {
                throw new InvalidOperationException("OpenAI API key not configured in testsettings.json");
            }

            if (string.IsNullOrEmpty(groqApiKey) || groqApiKey.Contains("YOUR_"))
            {
                throw new InvalidOperationException("Groq API key not configured in testsettings.json");
            }
        }

        public async Task InitializeAsync()
        {
            var cosmosConnectionString = _configuration["CosmosDb:ConnectionString"];
            _cosmosClient = new CosmosClient(cosmosConnectionString);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var profileLogger = loggerFactory.CreateLogger<EntityService>();
            var conversationLogger = loggerFactory.CreateLogger<ConversationService>();
            var summaryLogger = loggerFactory.CreateLogger<EntitySummaryService>();
            var embeddingLogger = loggerFactory.CreateLogger<OpenAIEmbeddingService>();
            var storageLogger = loggerFactory.CreateLogger<EmbeddingStorageService>();

            var databaseId = _configuration["CosmosDb:DatabaseId"] ?? "ProfileMatchingTestDB";
            var profilesContainerId = _configuration["CosmosDb:ProfilesContainerId"] ?? "profiles";

            _profileService = new EntityService(_cosmosClient, databaseId, profilesContainerId, profileLogger);
            _conversationService = new ConversationService(_cosmosClient, _configuration, new HttpClient(), conversationLogger);
            _profileSummaryService = new EntitySummaryService(summaryLogger, new List<IEntitySummaryStrategy>());
            _embeddingService = new OpenAIEmbeddingService(_configuration, embeddingLogger);
            _embeddingStorageService = new EmbeddingStorageService(_cosmosClient, _configuration, storageLogger);

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            // Check if we should keep test data
            var skipCleanup = Environment.GetEnvironmentVariable("SKIP_TEST_CLEANUP");
            if (!string.IsNullOrEmpty(skipCleanup) && skipCleanup.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine($"\n========== DATA KEPT FOR DEMO ==========");
                _output.WriteLine($" SKIP_TEST_CLEANUP is set - Keeping {_testProfileIds.Count} conversational profiles");
                _output.WriteLine($"\nThese profiles were built through natural conversation!");
                _output.WriteLine($" Try searching for them with semantic queries.");
                _output.WriteLine($"\nTo clean up later, run tests without SKIP_TEST_CLEANUP");
                _cosmosClient?.Dispose();
                return;
            }

            _output.WriteLine($"\nCleaning up {_testProfileIds.Count} conversational profiles...");

            foreach (var profileId in _testProfileIds)
            {
                try
                {
                    await _profileService!.DeleteEntityAsync(profileId);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            _cosmosClient?.Dispose();
            _output.WriteLine("Cleanup completed.");
        }

        [Fact]
        public async Task Demo_ConversationalProfileBuilding_50Profiles()
        {
            _output.WriteLine("====================================================================");
            _output.WriteLine("                                                                    ");
            _output.WriteLine("          Conversational PersonEntity Building Demo                     ");
            _output.WriteLine("                                                                    ");
            _output.WriteLine("  Build rich profiles through natural conversation, not forms!     ");
            _output.WriteLine("                                                                    ");
            _output.WriteLine("====================================================================\n");

            // Sample conversations representing different user types
            var conversationTemplates = GetConversationTemplates();

            _output.WriteLine($"▶ Building {conversationTemplates.Count} profiles through conversation...\n");

            int profileCount = 0;
            var startTime = DateTime.UtcNow;

            foreach (var template in conversationTemplates)
            {
                try
                {
                    profileCount++;

                    // Create minimal profile
                    var profile = new PersonEntity
                    {
                        Id = Guid.NewGuid(),
                        Name = template.Name,
                        OwnedByUserId = _demoUserId,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };

                    await _profileService!.AddEntityAsync(profile);
                    _testProfileIds.Add(profile.Id.ToString());

                    // Simulate conversation to build profile
                    foreach (var userMessage in template.UserMessages)
                    {
                        var response = await _conversationService!.ProcessUserMessageAsync(
                            profile.Id.ToString(),
                            _demoUserId,
                            userMessage
                        );

                        // Rate limiting - Groq has limits
                        await Task.Delay(100);
                    }

                    // Get conversation history to include in summary
                    var conversationHistory = await _conversationService!.GetConversationHistoryAsync(profile.Id.ToString());

                    // Generate AI summary from profile AND conversation
                    var summaryResult = await _profileSummaryService!.GenerateSummaryAsync(profile, conversationHistory);

                    // Generate embedding
                    var embedding = await _embeddingService!.GenerateEmbeddingAsync(summaryResult.Summary);
                    if (embedding != null)
                    {
                        var profileEmbedding = new EntityMatching.Core.Models.Embedding.EntityEmbedding
                        {
                            Id = Guid.NewGuid().ToString(),
                EntityId = profile.Id.ToString(),
                            Embedding = embedding,
                            EmbeddingModel = "text-embedding-3-small",
                EntitySummary = summaryResult.Summary,
                EntityLastModified = profile.LastModified,
                            GeneratedAt = DateTime.UtcNow,
                            Status = EntityMatching.Core.Models.Embedding.EmbeddingStatus.Generated
                        };

                        await _embeddingStorageService!.UpsertEmbeddingAsync(profileEmbedding);
                    }

                    if (profileCount % 10 == 0)
                    {
                        _output.WriteLine($"  ✓ Built {profileCount} profiles through conversation");
                    }

                    // Rate limiting for OpenAI embeddings
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"  ⚠ Error building profile {profileCount}: {ex.Message}");
                }
            }

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            var profilesPerSec = profileCount / duration;

            _output.WriteLine($"\n✅ Built {profileCount} profiles in {duration:F1} seconds ({profilesPerSec:F1} profiles/sec)");

            _output.WriteLine("\n====================================================================");
            _output.WriteLine("                                                                    ");
            _output.WriteLine("                  Conversational Profiles Created!                 ");
            _output.WriteLine("                                                                    ");
            _output.WriteLine($"  {profileCount} profiles built through natural conversation      ");
            _output.WriteLine("                                                                    ");
            _output.WriteLine("  Key Features Demonstrated:                                       ");
            _output.WriteLine("  - Natural language profiling (no forms!)                         ");
            _output.WriteLine("  - AI insight extraction                                          ");
            _output.WriteLine("  - Groq AI conversation management                                ");
            _output.WriteLine("  - Automatic embedding generation                                 ");
            _output.WriteLine("                                                                    ");
            _output.WriteLine("====================================================================\n");
        }

        /// <summary>
        /// Realistic conversation templates from EXECUTIVE_SUMMARY.md examples
        /// </summary>
        private List<ConversationTemplate> GetConversationTemplates()
        {
            return new List<ConversationTemplate>
            {
                // Example from EXECUTIVE_SUMMARY.md
                new ConversationTemplate
                {
                    Name = "Outdoor Enthusiast - Sarah",
                    UserMessages = new List<string>
                    {
                        "They love hiking on weekends, enjoy trying new Thai restaurants, and are learning Python for data science.",
                        "They prefer mountain trails over coastal hikes.",
                        "They're interested in machine learning specifically, especially for analyzing hiking trail data."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Tech Professional - Marcus",
                    UserMessages = new List<string>
                    {
                        "He's a senior software engineer specializing in cloud architecture.",
                        "He works primarily with AWS and Azure, loves building scalable systems.",
                        "Outside of work, he plays guitar and enjoys craft beer tasting."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Artist - Elena",
                    UserMessages = new List<string>
                    {
                        "She's a watercolor artist who loves painting landscapes.",
                        "She enjoys quiet coffee shops and indie music.",
                        "She's introverted and prefers one-on-one conversations to large gatherings."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Fitness Enthusiast - Jordan",
                    UserMessages = new List<string>
                    {
                        "They're into CrossFit and competitive running.",
                        "They follow a strict paleo diet and track macros.",
                        "They love early morning workouts and meal prepping on Sundays."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Food Blogger - Aisha",
                    UserMessages = new List<string>
                    {
                        "She runs a food blog focused on vegan and plant-based recipes.",
                        "She has a peanut allergy so she's very careful about ingredients.",
                        "She loves discovering hidden gem restaurants and farmer's markets."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Parent - David",
                    UserMessages = new List<string>
                    {
                        "He's a stay-at-home dad with two kids, ages 3 and 5.",
                        "He enjoys family-friendly activities like parks and children's museums.",
                        "He's always looking for quiet, kid-friendly venues with good accessibility."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Gamer - Taylor",
                    UserMessages = new List<string>
                    {
                        "They're a competitive esports player, mainly Valorant and League of Legends.",
                        "They stream on Twitch and love building custom PCs.",
                        "They prefer night-time activities and online hangouts with their gaming crew."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Bookworm - Maya",
                    UserMessages = new List<string>
                    {
                        "She reads about 50 books a year, mostly fantasy and sci-fi.",
                        "She's part of three book clubs and loves discussing plot theories.",
                        "She enjoys quiet libraries, cozy cafes, and rainy days with a good book."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Traveler - Carlos",
                    UserMessages = new List<string>
                    {
                        "He's visited 45 countries and counting.",
                        "He prefers backpacking and budget travel over luxury resorts.",
                        "He's learning Spanish and Mandarin to connect better with locals."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Musician - Zara",
                    UserMessages = new List<string>
                    {
                        "She's a jazz pianist who performs at local venues.",
                        "She loves intimate jazz clubs with good acoustics.",
                        "She's sensitive to loud noises and prefers smaller, quieter concert halls."
                    }
                },

                // Add more templates to reach 50...
                new ConversationTemplate
                {
                    Name = "Accessibility Advocate - James",
                    UserMessages = new List<string>
                    {
                        "He uses a wheelchair and advocates for accessible design.",
                        "He's passionate about inclusive architecture and urban planning.",
                        "He loves accessible hiking trails and venues with proper ramp access."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Dog Lover - Sophie",
                    UserMessages = new List<string>
                    {
                        "She has three rescue dogs and volunteers at the local shelter.",
                        "She's always looking for dog-friendly parks and patios.",
                        "She loves morning dog walks and weekend hiking trips with her pack."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Startup Founder - Raj",
                    UserMessages = new List<string>
                    {
                        "He's building a SaaS startup in the HR tech space.",
                        "He's obsessed with productivity hacks and loves networking events.",
                        "He drinks way too much coffee and prefers co-working spaces to traditional offices."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Yoga Instructor - Lily",
                    UserMessages = new List<string>
                    {
                        "She teaches vinyasa and yin yoga at a local studio.",
                        "She's vegan and practices mindfulness meditation daily.",
                        "She loves nature, green smoothies, and outdoor yoga sessions."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Photography Enthusiast - Omar",
                    UserMessages = new List<string>
                    {
                        "He's an amateur landscape photographer who chases golden hour.",
                        "He loves hiking to remote locations for the perfect shot.",
                        "He's learning Lightroom and Photoshop to enhance his editing skills."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Baker - Emma",
                    UserMessages = new List<string>
                    {
                        "She's a home baker specializing in sourdough and artisan breads.",
                        "She loves farmer's markets and sourcing local ingredients.",
                        "She dreams of opening her own bakery someday."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Climate Activist - Alex",
                    UserMessages = new List<string>
                    {
                        "They're passionate about environmental sustainability and zero waste.",
                        "They organize local beach cleanups and climate protests.",
                        "They bike everywhere and refuse single-use plastics."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Fashion Designer - Mia",
                    UserMessages = new List<string>
                    {
                        "She designs sustainable fashion using recycled materials.",
                        "She loves vintage shopping and upcycling old clothes.",
                        "She's inspired by 90s fashion and minimalist aesthetics."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Podcast Host - Tyler",
                    UserMessages = new List<string>
                    {
                        "He hosts a true crime podcast with 50K monthly listeners.",
                        "He spends hours researching cold cases and interviewing experts.",
                        "He loves audiobooks, documentaries, and late-night recording sessions."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Rock Climber - Nina",
                    UserMessages = new List<string>
                    {
                        "She's into bouldering and sport climbing, usually at the local gym.",
                        "She's training for her first outdoor lead climb in Yosemite.",
                        "She has strong arms, loves chalk dust, and fears heights ironically."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Chef - Andre",
                    UserMessages = new List<string>
                    {
                        "He's a sous chef at a Michelin-starred French restaurant.",
                        "He's obsessed with perfecting his knife skills and plating techniques.",
                        "He loves food markets, cooking shows, and experimenting with molecular gastronomy."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Therapist - Dr. Kim",
                    UserMessages = new List<string>
                    {
                        "She's a licensed therapist specializing in anxiety and trauma.",
                        "She practices self-care through journaling and nature walks.",
                        "She's an empath who needs quiet time to recharge after sessions."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Marine Biologist - Lucas",
                    UserMessages = new List<string>
                    {
                        "He studies coral reef ecosystems and ocean conservation.",
                        "He's a certified scuba diver who spends weekends underwater.",
                        "He's passionate about protecting marine life and reducing ocean plastic."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Stand-up Comedian - Jess",
                    UserMessages = new List<string>
                    {
                        "She performs at open mics and comedy clubs around the city.",
                        "She uses humor to talk about mental health and millennial struggles.",
                        "She loves late nights, hecklers (kind of), and writing new material."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Urban Farmer - Ben",
                    UserMessages = new List<string>
                    {
                        "He grows vegetables on his rooftop in Brooklyn.",
                        "He's into permaculture, composting, and seed saving.",
                        "He sells produce at the local farmer's market on weekends."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Historical Reenactor - Catherine",
                    UserMessages = new List<string>
                    {
                        "She participates in Civil War reenactments as a nurse.",
                        "She loves history museums, period clothing, and antique shops.",
                        "She's meticulous about historical accuracy and handmakes her costumes."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Woodworker - Marcus",
                    UserMessages = new List<string>
                    {
                        "He builds custom furniture in his garage workshop.",
                        "He loves working with reclaimed wood and hand tools.",
                        "He finds woodworking meditative and therapeutic."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Astronomer - Dr. Patel",
                    UserMessages = new List<string>
                    {
                        "She studies exoplanets and searches for signs of extraterrestrial life.",
                        "She spends nights at the observatory and loves stargazing.",
                        "She's fascinated by the cosmos and dreams of space exploration."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Bartender - Jake",
                    UserMessages = new List<string>
                    {
                        "He's a mixologist who creates craft cocktails at a speakeasy.",
                        "He loves experimenting with infusions, bitters, and unique garnishes.",
                        "He's a night owl who thrives in social, high-energy environments."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Neuroscientist - Dr. Lee",
                    UserMessages = new List<string>
                    {
                        "She researches neuroplasticity and brain injury recovery.",
                        "She's fascinated by how the brain adapts and heals.",
                        "She loves TED talks, brain teasers, and chess."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Voice Actor - Ryan",
                    UserMessages = new List<string>
                    {
                        "He voices characters for animated shows and video games.",
                        "He can do dozens of accents and character voices.",
                        "He loves improv comedy, audiobooks, and perfecting his craft."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Vintage Collector - Grace",
                    UserMessages = new List<string>
                    {
                        "She collects vintage vinyl records from the 60s and 70s.",
                        "She spends weekends at estate sales and thrift stores.",
                        "She loves retro aesthetics, analog technology, and music history."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Wilderness Guide - Connor",
                    UserMessages = new List<string>
                    {
                        "He leads multi-day backpacking trips in national parks.",
                        "He's trained in wilderness first aid and survival skills.",
                        "He loves teaching people to appreciate and protect nature."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Ceramicist - Yuki",
                    UserMessages = new List<string>
                    {
                        "She creates handmade pottery using traditional Japanese techniques.",
                        "She loves the tactile process of working with clay.",
                        "She finds ceramics calming and meditative."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Data Analyst - Priya",
                    UserMessages = new List<string>
                    {
                        "She analyzes marketing data to optimize campaign performance.",
                        "She's proficient in SQL, Python, and Tableau.",
                        "She loves finding patterns in data and turning numbers into insights."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Skateboarder - Diego",
                    UserMessages = new List<string>
                    {
                        "He skates at the local skate park almost every day.",
                        "He's working on landing a kickflip and dreams of going pro.",
                        "He loves street culture, hip-hop, and DIY ramps."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Librarian - Helen",
                    UserMessages = new List<string>
                    {
                        "She works at a public library and runs children's story time.",
                        "She's read thousands of books and gives great recommendations.",
                        "She loves quiet spaces, the smell of old books, and literary discussions."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Surfer - Kai",
                    UserMessages = new List<string>
                    {
                        "He surfs every morning before work at dawn patrol.",
                        "He loves ocean conservation and beach cleanups.",
                        "He's laid-back, sun-kissed, and always checking the surf report."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Jewelry Designer - Isabella",
                    UserMessages = new List<string>
                    {
                        "She designs minimalist jewelry using recycled metals.",
                        "She sells her pieces on Etsy and at local craft fairs.",
                        "She loves gemstones, metalworking, and creating wearable art."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Cybersecurity Expert - Dmitri",
                    UserMessages = new List<string>
                    {
                        "He's a white-hat hacker who tests corporate security systems.",
                        "He loves CTF competitions and solving complex puzzles.",
                        "He's paranoid about privacy and uses encrypted everything."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Birdwatcher - Margaret",
                    UserMessages = new List<string>
                    {
                        "She's identified over 300 bird species in North America.",
                        "She wakes up early for dawn birding sessions.",
                        "She loves binoculars, field guides, and peaceful nature walks."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Meditation Teacher - Sanjay",
                    UserMessages = new List<string>
                    {
                        "He teaches mindfulness meditation and breathwork.",
                        "He's been practicing for 15 years and finds peace in stillness.",
                        "He loves yoga retreats, zen gardens, and helping others find calm."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Street Artist - Luna",
                    UserMessages = new List<string>
                    {
                        "She creates vibrant murals and graffiti art around the city.",
                        "She's passionate about public art and making neighborhoods beautiful.",
                        "She loves spray paint, late-night art missions, and bold colors."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Paramedic - Jason",
                    UserMessages = new List<string>
                    {
                        "He works 12-hour shifts responding to emergency calls.",
                        "He's seen it all and has nerves of steel.",
                        "He decompresses with video games and dark humor."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Beekeeper - Amelia",
                    UserMessages = new List<string>
                    {
                        "She maintains 10 beehives and harvests her own honey.",
                        "She's passionate about pollinator conservation.",
                        "She loves nature, sustainability, and educating people about bees."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Dancer - Carlos",
                    UserMessages = new List<string>
                    {
                        "He's a professional salsa dancer who competes internationally.",
                        "He teaches dance classes and loves performing on stage.",
                        "He's energetic, rhythmic, and always moving to music."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Archaeologist - Dr. Hassan",
                    UserMessages = new List<string>
                    {
                        "He excavates ancient civilizations and studies artifacts.",
                        "He's worked on digs in Egypt, Peru, and Greece.",
                        "He loves history, discovery, and piecing together the past."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Florist - Violet",
                    UserMessages = new List<string>
                    {
                        "She arranges flowers for weddings and special events.",
                        "She loves the beauty and symbolism of different blooms.",
                        "She wakes up early to buy fresh flowers at the wholesale market."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Film Critic - Nathan",
                    UserMessages = new List<string>
                    {
                        "He reviews movies for an online publication.",
                        "He's seen over 2000 films and loves classic cinema.",
                        "He's opinionated, analytical, and loves debating film theory."
                    }
                },

                new ConversationTemplate
                {
                    Name = "Personal Trainer - Keisha",
                    UserMessages = new List<string>
                    {
                        "She's a certified personal trainer specializing in strength training.",
                        "She's competed in powerlifting competitions.",
                        "She loves motivating clients and helping them hit their fitness goals."
                    }
                }
            };
        }

        private class ConversationTemplate
        {
            public string Name { get; set; } = "";
            public List<string> UserMessages { get; set; } = new();
        }
    }
}

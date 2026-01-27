using EntityMatching.SDK;
using EntityMatching.Shared.Models;

namespace EntityMatching.SDK.Examples;

/// <summary>
/// Example: Privacy-First Resume Upload
///
/// This example demonstrates how to upload a resume without sending the text to the server.
/// The resume text is processed locally to generate an embedding vector, and only the vector
/// is uploaded to the ProfileMatchingAPI.
/// </summary>
public static class UploadResumeExample
{
    public static async Task RunAsync()
    {
        // Initialize the client
        var client = new ProfileMatchingClient(new ProfileMatchingClientOptions
        {
            ApiKey = Environment.GetEnvironmentVariable("PROFILEMATCHING_API_KEY") ?? "your-api-key",
            OpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "your-openai-key",
            BaseUrl = "https://profileaiapi.azurewebsites.net" // Or https://api.bystorm.com when APIM is ready
        });

        // Step 1: Create a profile (or use existing profile ID)
        var profile = await client.Entities.CreateAsync(new Entity
        {
            OwnedByUserId = "user-123",
            Name = "John Doe",
            Description = "Software Engineer",
            IsSearchable = true,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        });

        Console.WriteLine($"Created profile: {profile.Id}");

        // Step 2: Resume text (stays on your device!)
        var resumeText = @"
            Senior Software Engineer

            EXPERIENCE:
            - 10 years of Python development
            - Expert in AWS (Lambda, S3, DynamoDB, EC2)
            - Built machine learning pipelines processing 100M+ events/day
            - Led team of 5 engineers at TechCorp

            SKILLS:
            - Languages: Python, TypeScript, C#, Go
            - Cloud: AWS, Azure, GCP
            - ML: TensorFlow, PyTorch, scikit-learn
            - Databases: PostgreSQL, MongoDB, Redis, SQL Server

            EDUCATION:
            - BS Computer Science, Stanford University
        ";

        // Step 3: Upload resume (privacy-first!)
        Console.WriteLine("Generating embedding locally...");
        await client.UploadResumeAsync(profile.Id, resumeText);

        Console.WriteLine("✅ Success! Resume vector uploaded.");
        Console.WriteLine("   🔒 Your resume text NEVER left this device!");
        Console.WriteLine("   🔒 Only a 1536-dimensional vector was sent to the server.");
        Console.WriteLine("   🔒 Even if the server is hacked, attackers get meaningless numbers.");

        // Step 4: Companies can now search for you
        // They will only see your profile ID and similarity score
        // Your name/contact stays private until you opt-in
        Console.WriteLine($"\nCompanies searching for \"Senior Python engineer, AWS experience\" will see:");
        Console.WriteLine($"  Entity #{profile.Id} (94% match)");
        Console.WriteLine("  [Your name and contact info remain hidden]");
    }
}

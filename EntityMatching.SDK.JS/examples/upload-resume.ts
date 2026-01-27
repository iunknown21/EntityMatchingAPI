/**
 * Example: Privacy-First Resume Upload
 *
 * This example demonstrates how to upload a resume without sending the text to the server.
 * The resume text is processed locally to generate an embedding vector, and only the vector
 * is uploaded to the ProfileMatchingAPI.
 */

import { ProfileMatchingClient } from '../src';

async function main() {
  // Initialize the client
  const client = new ProfileMatchingClient({
    apiKey: process.env.PROFILEMATCHING_API_KEY || 'your-api-key',
    openaiKey: process.env.OPENAI_API_KEY || 'your-openai-key',
    baseUrl: 'https://profileaiapi.azurewebsites.net', // Or https://api.bystorm.com when APIM is ready
  });

  // Step 1: Create a profile (or use existing profile ID)
  const profile = await client.profiles.create({
    ownedByUserId: 'user-123',
    name: 'John Doe',
    bio: 'Software Engineer',
    isSearchable: true,
  });

  console.log(`Created profile: ${profile.id}`);

  // Step 2: Resume text (stays on your device!)
  const resumeText = `
    Senior Software Engineer

    EXPERIENCE:
    - 10 years of Python development
    - Expert in AWS (Lambda, S3, DynamoDB, EC2)
    - Built machine learning pipelines processing 100M+ events/day
    - Led team of 5 engineers at TechCorp

    SKILLS:
    - Languages: Python, TypeScript, Go
    - Cloud: AWS, Azure, GCP
    - ML: TensorFlow, PyTorch, scikit-learn
    - Databases: PostgreSQL, MongoDB, Redis

    EDUCATION:
    - BS Computer Science, Stanford University
  `;

  // Step 3: Upload resume (privacy-first!)
  console.log('Generating embedding locally...');
  await client.uploadResume(profile.id.toString(), resumeText);

  console.log('✅ Success! Resume vector uploaded.');
  console.log('   🔒 Your resume text NEVER left this device!');
  console.log('   🔒 Only a 1536-dimensional vector was sent to the server.');
  console.log('   🔒 Even if the server is hacked, attackers get meaningless numbers.');

  // Step 4: Companies can now search for you
  // They will only see your profile ID and similarity score
  // Your name/contact stays private until you opt-in
  console.log('\nCompanies searching for "Senior Python engineer, AWS experience" will see:');
  console.log('  Profile #' + profile.id + ' (94% match)');
  console.log('  [Your name and contact info remain hidden]');
}

main().catch(console.error);

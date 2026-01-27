import OpenAI from 'openai';
import { HttpClient } from './utils/http';
import { ProfilesEndpoint } from './endpoints/profiles';
import { EmbeddingsEndpoint } from './endpoints/embeddings';
import { ConversationsEndpoint } from './endpoints/conversations';
import { SearchEndpoint } from './endpoints/search';

export interface ProfileMatchingClientOptions {
  /**
   * API key for ProfileMatchingAPI (Ocp-Apim-Subscription-Key)
   */
  apiKey: string;

  /**
   * Base URL for the API (defaults to https://api.bystorm.com)
   */
  baseUrl?: string;

  /**
   * OpenAI API key for client-side embedding generation
   * Required for privacy-first features like uploadResume()
   */
  openaiKey?: string;
}

/**
 * ProfileMatchingAPI Client SDK
 *
 * Privacy-first client for semantic profile matching with zero PII storage.
 * Supports client-side embedding generation to ensure sensitive data never leaves the user's device.
 *
 * @example
 * ```typescript
 * const client = new ProfileMatchingClient({
 *   apiKey: 'your-api-key',
 *   openaiKey: 'your-openai-key' // Optional, for client-side embedding
 * });
 *
 * // Privacy-first resume upload
 * await client.uploadResume(profileId, resumeText);
 * ```
 */
export class ProfileMatchingClient {
  private http: HttpClient;
  private openai?: OpenAI;

  /**
   * Profiles endpoint - CRUD operations for profiles
   */
  public profiles: ProfilesEndpoint;

  /**
   * Embeddings endpoint - Privacy-first vector upload
   */
  public embeddings: EmbeddingsEndpoint;

  /**
   * Conversations endpoint - Conversational profiling with AI
   */
  public conversations: ConversationsEndpoint;

  /**
   * Search endpoint - Semantic search with attribute filtering
   */
  public search: SearchEndpoint;

  constructor(options: ProfileMatchingClientOptions) {
    const baseUrl = options.baseUrl || 'https://api.bystorm.com';

    this.http = new HttpClient({
      baseUrl,
      apiKey: options.apiKey,
    });

    // Initialize OpenAI client if API key provided
    if (options.openaiKey) {
      this.openai = new OpenAI({
        apiKey: options.openaiKey,
        dangerouslyAllowBrowser: true, // Allow browser usage for client-side embedding
      });
    }

    // Initialize endpoint wrappers
    this.profiles = new ProfilesEndpoint(this.http);
    this.embeddings = new EmbeddingsEndpoint(this.http);
    this.conversations = new ConversationsEndpoint(this.http);
    this.search = new SearchEndpoint(this.http);
  }

  /**
   * Upload resume with privacy-first approach
   *
   * Generates embedding locally using OpenAI API, then uploads ONLY the vector.
   * The original resume text never leaves the client device.
   *
   * **Privacy Benefits:**
   * - Server never sees resume text
   * - Only 1536-dimensional vector is stored
   * - Even if database is breached, attackers get meaningless numbers
   * - GDPR compliant - no PII means no data protection requirements
   *
   * @param profileId - Profile ID to associate the resume with
   * @param resumeText - Resume text (stays on client, never sent to server)
   * @returns Upload confirmation
   *
   * @throws Error if OpenAI API key was not provided in constructor
   *
   * @example
   * ```typescript
   * const resumeText = `
   *   Senior Software Engineer with 10 years experience in Python and AWS.
   *   Built machine learning pipelines processing 100M+ events/day.
   * `;
   *
   * await client.uploadResume(profileId, resumeText);
   * // Resume text stays local, only vector uploaded!
   * ```
   */
  async uploadResume(profileId: string, resumeText: string): Promise<void> {
    if (!this.openai) {
      throw new Error(
        'OpenAI API key required for client-side embedding generation. ' +
          'Provide openaiKey in ProfileMatchingClientOptions.'
      );
    }

    // Step 1: Generate embedding locally (privacy-first!)
    const embeddingResponse = await this.openai.embeddings.create({
      model: 'text-embedding-3-small',
      input: resumeText,
    });

    const vector = embeddingResponse.data[0].embedding;

    // Step 2: Upload ONLY the vector (never the text)
    await this.embeddings.upload(profileId, {
      embedding: vector,
      embeddingModel: 'text-embedding-3-small',
      metadata: {
        generatedAt: new Date().toISOString(),
        clientVersion: '1.0.0',
      },
    });

    // Original resume text stays on client, never sent to server
    // This is the core privacy guarantee of ProfileMatchingAPI
  }

  /**
   * Generate embedding for any text (not just resumes)
   *
   * @param text - Text to generate embedding for
   * @returns 1536-dimensional embedding vector
   *
   * @throws Error if OpenAI API key was not provided
   */
  async generateEmbedding(text: string): Promise<number[]> {
    if (!this.openai) {
      throw new Error(
        'OpenAI API key required for embedding generation. ' +
          'Provide openaiKey in ProfileMatchingClientOptions.'
      );
    }

    const response = await this.openai.embeddings.create({
      model: 'text-embedding-3-small',
      input: text,
    });

    return response.data[0].embedding;
  }
}

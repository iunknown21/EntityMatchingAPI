import { HttpClient } from '../utils/http';
import {
  ConversationMessage,
  ConversationResponse,
  ConversationHistory,
} from '../models/common';

/**
 * Conversations endpoint wrapper
 * Handles conversational profiling with AI
 */
export class ConversationsEndpoint {
  constructor(private http: HttpClient) {}

  /**
   * Send a message to the conversational profiling AI
   *
   * @param profileId - Profile ID to build conversation for
   * @param message - Message containing profile information
   * @returns AI response with extracted insights
   */
  async sendMessage(
    profileId: string,
    message: ConversationMessage
  ): Promise<ConversationResponse> {
    return this.http.post<ConversationResponse>(
      `/api/v1/profiles/${profileId}/conversation`,
      message
    );
  }

  /**
   * Get conversation history for a profile
   *
   * @param profileId - Profile ID
   * @returns Conversation history with all extracted insights
   */
  async getHistory(profileId: string): Promise<ConversationHistory> {
    return this.http.get<ConversationHistory>(
      `/api/v1/profiles/${profileId}/conversation`
    );
  }

  /**
   * Delete conversation history for a profile
   *
   * @param profileId - Profile ID
   */
  async delete(profileId: string): Promise<void> {
    return this.http.delete<void>(`/api/v1/profiles/${profileId}/conversation`);
  }
}

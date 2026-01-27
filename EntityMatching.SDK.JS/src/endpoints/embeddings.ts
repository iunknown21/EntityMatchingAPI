import { HttpClient } from '../utils/http';
import { UploadEmbeddingRequest, EmbeddingUploadResponse } from '../models/common';

/**
 * Embeddings endpoint wrapper
 * Handles privacy-first vector upload operations
 */
export class EmbeddingsEndpoint {
  constructor(private http: HttpClient) {}

  /**
   * Upload a pre-computed embedding vector
   * Privacy-first: Client generates embeddings locally, uploads only vectors
   *
   * @param profileId - Profile ID to associate the embedding with
   * @param request - Embedding data (1536-dimensional vector)
   * @returns Upload confirmation with status
   */
  async upload(
    profileId: string,
    request: UploadEmbeddingRequest
  ): Promise<EmbeddingUploadResponse> {
    return this.http.post<EmbeddingUploadResponse>(
      `/api/v1/profiles/${profileId}/embeddings/upload`,
      request
    );
  }
}

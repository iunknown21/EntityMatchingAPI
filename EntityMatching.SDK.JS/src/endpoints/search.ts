import { HttpClient } from '../utils/http';
import { SearchRequest, SearchResponse } from '../models/common';

/**
 * Search endpoint wrapper
 * Handles semantic search with attribute filtering
 */
export class SearchEndpoint {
  constructor(private http: HttpClient) {}

  /**
   * Search profiles using semantic similarity and attribute filters
   * Privacy-first: Returns only profile IDs and similarity scores
   *
   * @param request - Search query with optional filters
   * @returns Search results with profile IDs and similarity scores
   */
  async search(request: SearchRequest): Promise<SearchResponse> {
    return this.http.post<SearchResponse>('/api/v1/profiles/search', request);
  }
}

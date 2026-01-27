import { HttpClient } from '../utils/http';
import { Profile } from '../models/common';

/**
 * Profiles endpoint wrapper
 * Handles all profile CRUD operations
 */
export class ProfilesEndpoint {
  constructor(private http: HttpClient) {}

  /**
   * Get all profiles for a user
   */
  async list(userId: string): Promise<Profile[]> {
    return this.http.get<Profile[]>('/api/v1/profiles', { userId });
  }

  /**
   * Get a single profile by ID
   */
  async get(profileId: string): Promise<Profile> {
    return this.http.get<Profile>(`/api/v1/profiles/${profileId}`);
  }

  /**
   * Create a new profile
   */
  async create(profile: Partial<Profile>): Promise<Profile> {
    return this.http.post<Profile>('/api/v1/profiles', profile);
  }

  /**
   * Update an existing profile
   */
  async update(profileId: string, profile: Partial<Profile>): Promise<Profile> {
    return this.http.put<Profile>(`/api/v1/profiles/${profileId}`, profile);
  }

  /**
   * Delete a profile
   */
  async delete(profileId: string): Promise<void> {
    return this.http.delete<void>(`/api/v1/profiles/${profileId}`);
  }

  /**
   * Get similar profiles based on vector similarity
   */
  async getSimilar(profileId: string, limit?: number): Promise<any> {
    const params: Record<string, string> = {};
    if (limit) params.limit = limit.toString();
    return this.http.get(`/api/v1/profiles/${profileId}/similar`, params);
  }
}

/**
 * Common types and interfaces used across the SDK
 */

export interface Profile {
  id: string;
  ownedByUserId: string;
  name: string;
  bio?: string;
  birthday?: string;
  contactInformation?: ContactInformation;
  isSearchable: boolean;
  privacySettings?: PrivacySettings;
  createdAt: string;
  lastModified: string;
}

export interface ContactInformation {
  email?: string;
  phone?: string;
  address?: string;
}

export interface PrivacySettings {
  defaultVisibility: 'Public' | 'Private' | 'FriendsOnly';
  fieldVisibilityMap?: Record<string, 'Public' | 'Private' | 'FriendsOnly'>;
}

export interface SearchRequest {
  query: string;
  attributeFilters?: AttributeFilters;
  requestingUserId?: string;
  enforcePrivacy?: boolean;
  limit?: number;
  minSimilarity?: number;
}

export interface AttributeFilters {
  logicalOperator: 'And' | 'Or';
  filters: AttributeFilter[];
}

export interface AttributeFilter {
  fieldPath: string;
  operator: FilterOperator;
  value?: any;
}

export type FilterOperator =
  | 'Equals'
  | 'NotEquals'
  | 'Contains'
  | 'NotContains'
  | 'GreaterThan'
  | 'LessThan'
  | 'InRange'
  | 'IsTrue'
  | 'IsFalse'
  | 'Exists'
  | 'NotExists';

export interface SearchResult {
  profileId: string;
  similarityScore: number;
  matchedAttributes?: Record<string, any>;
  profileName?: string;
  embeddingDimensions?: number;
}

export interface SearchResponse {
  matches: SearchResult[];
  totalMatches: number;
  metadata: SearchMetadata;
}

export interface SearchMetadata {
  searchedAt: string;
  totalEmbeddingsSearched: number;
  minSimilarity: number;
  requestedLimit: number;
  searchDurationMs: number;
}

export interface UploadEmbeddingRequest {
  embedding: number[];
  embeddingModel?: string;
  metadata?: ClientEmbeddingMetadata;
}

export interface ClientEmbeddingMetadata {
  generatedAt?: string;
  clientVersion?: string;
}

export interface EmbeddingUploadResponse {
  profileId: string;
  status: string;
  dimensions: number;
  embeddingModel: string;
  generatedAt: string;
  message: string;
}

export interface ConversationMessage {
  userId: string;
  message: string;
}

export interface ConversationResponse {
  aiResponse: string;
  newInsights: Insight[];
}

export interface Insight {
  category: string;
  insight: string;
  confidence: number;
}

export interface ConversationHistory {
  profileId: string;
  conversationStarted: string;
  lastUpdated: string;
  totalChunks: number;
  extractedInsights: Insight[];
  messageCount: number;
}

export interface ApiError {
  message: string;
  statusCode: number;
}

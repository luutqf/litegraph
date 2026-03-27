import { v4 as uuidV4 } from 'uuid';

/**
 * VectorSearchRequest class representing a vector search request.
 */
export class VectorSearchRequest {
  /**
   * @param {Object} request - Information about the vector search request.
   * @param {string} [request.TenantGUID] - Tenant GUID.
   * @param {string} [request.GraphGUID] - Graph GUID.
   * @param {string} [request.Domain] - Domain for the search.
   * @param {string} [request.SearchType] - Type of search to perform.
   * @param {Array<string>} [request.Labels] - Labels to filter by.
   * @param {Array<string>} [request.Tags] - Tags to filter by.
   * @param {Array<number>} [request.Embeddings] - Vector embeddings to search with.
   * @param {number|null} [request.StartIndex] - Start index for pagination.
   * @param {number|null} [request.MaxResults] - Maximum number of results to return.
   */
  constructor(request = {}) {
    const {
      TenantGUID = uuidV4(),
      GraphGUID = uuidV4(),
      Domain = null,
      SearchType = null,
      Labels = null,
      Tags = null,
      Embeddings = null,
      StartIndex = null,
      MaxResults = null,
    } = request;

    this.TenantGUID = TenantGUID;
    this.GraphGUID = GraphGUID;
    this.Domain = Domain;
    this.SearchType = SearchType;
    this.Labels = Labels;
    this.Tags = Tags;
    this.Embeddings = Embeddings;
    this.StartIndex = StartIndex;
    this.MaxResults = MaxResults;
  }
}

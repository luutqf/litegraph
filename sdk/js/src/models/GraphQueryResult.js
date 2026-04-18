export class GraphQueryExecutionProfile {
  constructor(profile = {}) {
    const {
      ParseTimeMs = 0,
      PlanTimeMs = 0,
      ExecuteTimeMs = 0,
      AuthorizationTimeMs = 0,
      RepositoryTimeMs = 0,
      RepositoryOperationCount = 0,
      VectorSearchTimeMs = 0,
      VectorSearchCount = 0,
      TransactionTimeMs = 0,
      SerializationTimeMs = 0,
      TotalTimeMs = 0,
    } = profile || {};

    this.ParseTimeMs = ParseTimeMs;
    this.PlanTimeMs = PlanTimeMs;
    this.ExecuteTimeMs = ExecuteTimeMs;
    this.AuthorizationTimeMs = AuthorizationTimeMs;
    this.RepositoryTimeMs = RepositoryTimeMs;
    this.RepositoryOperationCount = RepositoryOperationCount;
    this.VectorSearchTimeMs = VectorSearchTimeMs;
    this.VectorSearchCount = VectorSearchCount;
    this.TransactionTimeMs = TransactionTimeMs;
    this.SerializationTimeMs = SerializationTimeMs;
    this.TotalTimeMs = TotalTimeMs;
  }
}

export class GraphQueryPlanSummary {
  constructor(plan = {}) {
    const {
      Kind = null,
      Mutates = false,
      UsesVectorSearch = false,
      VectorDomain = null,
      HasOrder = false,
      HasLimit = false,
      EstimatedCost = 0,
      SeedKind = null,
      SeedVariable = null,
      SeedField = null,
    } = plan || {};

    this.Kind = Kind;
    this.Mutates = Mutates;
    this.UsesVectorSearch = UsesVectorSearch;
    this.VectorDomain = VectorDomain;
    this.HasOrder = HasOrder;
    this.HasLimit = HasLimit;
    this.EstimatedCost = EstimatedCost;
    this.SeedKind = SeedKind;
    this.SeedVariable = SeedVariable;
    this.SeedField = SeedField;
  }
}

export default class GraphQueryResult {
  constructor(result = {}) {
    const {
      Profile = null,
      Mutated = false,
      ExecutionTimeMs = 0,
      ExecutionProfile = null,
      Warnings = [],
      Plan = null,
      Rows = [],
      Nodes = [],
      Edges = [],
      Labels = [],
      Tags = [],
      Vectors = [],
      VectorSearchResults = [],
      RowCount = Rows.length,
    } = result || {};

    this.Profile = Profile;
    this.Mutated = Mutated;
    this.ExecutionTimeMs = ExecutionTimeMs;
    this.ExecutionProfile = ExecutionProfile ? new GraphQueryExecutionProfile(ExecutionProfile) : null;
    this.Warnings = Warnings;
    this.Plan = Plan ? new GraphQueryPlanSummary(Plan) : null;
    this.Rows = Rows;
    this.Nodes = Nodes.map((node) => new Node(node));
    this.Edges = Edges.map((edge) => new Edge(edge));
    this.Labels = Labels.map((label) => new LabelMetadata(label));
    this.Tags = Tags.map((tag) => new TagMetaData(tag));
    this.Vectors = Vectors.map((vector) => new VectorMetadata(vector));
    this.VectorSearchResults = VectorSearchResults.map((result) => new VectorSearchResult(result));
    this.RowCount = RowCount;
  }
}
import Edge from './Edge';
import LabelMetadata from './LabelMetadata';
import Node from './Node';
import TagMetaData from './TagMetaData';
import { VectorMetadata } from './VectorMetadata';
import { VectorSearchResult } from './VectorSearchResult';

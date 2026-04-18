export class GraphQueryExecutionProfile {
    constructor(profile?: any);
    ParseTimeMs: number;
    PlanTimeMs: number;
    ExecuteTimeMs: number;
    AuthorizationTimeMs: number;
    RepositoryTimeMs: number;
    RepositoryOperationCount: number;
    VectorSearchTimeMs: number;
    VectorSearchCount: number;
    TransactionTimeMs: number;
    SerializationTimeMs: number;
    TotalTimeMs: number;
}
export class GraphQueryPlanSummary {
    constructor(plan?: any);
    Kind: string | null;
    Mutates: boolean;
    UsesVectorSearch: boolean;
    VectorDomain: string | null;
    HasOrder: boolean;
    HasLimit: boolean;
    EstimatedCost: number;
    SeedKind: string | null;
    SeedVariable: string | null;
    SeedField: string | null;
}
export default class GraphQueryResult {
    constructor(result?: any);
    Profile: string | null;
    Mutated: boolean;
    ExecutionTimeMs: number;
    ExecutionProfile: GraphQueryExecutionProfile | null;
    Warnings: string[];
    Plan: GraphQueryPlanSummary | null;
    Rows: Array<any>;
    Nodes: Array<Node>;
    Edges: Array<Edge>;
    Labels: Array<LabelMetadata>;
    Tags: Array<TagMetaData>;
    Vectors: Array<VectorMetadata>;
    VectorSearchResults: Array<VectorSearchResult>;
    RowCount: number;
}
import Edge from './Edge';
import LabelMetadata from './LabelMetadata';
import Node from './Node';
import TagMetaData from './TagMetaData';
import { VectorMetadata } from './VectorMetadata';
import { VectorSearchResult } from './VectorSearchResult';

from typing import Any, Dict, List, Optional

from pydantic import BaseModel, ConfigDict, Field

from .edge import EdgeModel
from .label import LabelModel
from .node import NodeModel
from .tag import TagModel
from .vector_metadata import VectorMetadataModel
from .vector_search_result import VectorSearchResultModel


class GraphQueryPlanSummaryModel(BaseModel):
    """
    Native graph query plan summary.
    """

    kind: Optional[str] = Field(default=None, alias="Kind")
    mutates: bool = Field(default=False, alias="Mutates")
    uses_vector_search: bool = Field(default=False, alias="UsesVectorSearch")
    vector_domain: Optional[str] = Field(default=None, alias="VectorDomain")
    has_order: bool = Field(default=False, alias="HasOrder")
    has_limit: bool = Field(default=False, alias="HasLimit")
    estimated_cost: int = Field(default=0, alias="EstimatedCost")
    seed_kind: Optional[str] = Field(default=None, alias="SeedKind")
    seed_variable: Optional[str] = Field(default=None, alias="SeedVariable")
    seed_field: Optional[str] = Field(default=None, alias="SeedField")
    model_config = ConfigDict(populate_by_name=True)


class GraphQueryExecutionProfileModel(BaseModel):
    """
    Optional native graph query execution timing profile.
    """

    parse_time_ms: float = Field(default=0, alias="ParseTimeMs")
    plan_time_ms: float = Field(default=0, alias="PlanTimeMs")
    execute_time_ms: float = Field(default=0, alias="ExecuteTimeMs")
    authorization_time_ms: float = Field(default=0, alias="AuthorizationTimeMs")
    repository_time_ms: float = Field(default=0, alias="RepositoryTimeMs")
    repository_operation_count: int = Field(default=0, alias="RepositoryOperationCount")
    vector_search_time_ms: float = Field(default=0, alias="VectorSearchTimeMs")
    vector_search_count: int = Field(default=0, alias="VectorSearchCount")
    transaction_time_ms: float = Field(default=0, alias="TransactionTimeMs")
    serialization_time_ms: float = Field(default=0, alias="SerializationTimeMs")
    total_time_ms: float = Field(default=0, alias="TotalTimeMs")
    model_config = ConfigDict(populate_by_name=True)


class GraphQueryRequestModel(BaseModel):
    """
    Native graph query request.
    """

    query: str = Field(alias="Query")
    parameters: Dict[str, Any] = Field(default_factory=dict, alias="Parameters")
    max_results: int = Field(default=1000, alias="MaxResults", ge=1)
    timeout_seconds: int = Field(default=30, alias="TimeoutSeconds", ge=1)
    include_profile: bool = Field(default=False, alias="IncludeProfile")
    model_config = ConfigDict(populate_by_name=True)


class GraphQueryResultModel(BaseModel):
    """
    Native graph query result.
    """

    profile: Optional[str] = Field(default=None, alias="Profile")
    mutated: bool = Field(default=False, alias="Mutated")
    execution_time_ms: float = Field(default=0, alias="ExecutionTimeMs")
    execution_profile: Optional[GraphQueryExecutionProfileModel] = Field(
        default=None, alias="ExecutionProfile"
    )
    warnings: List[str] = Field(default_factory=list, alias="Warnings")
    plan: Optional[GraphQueryPlanSummaryModel] = Field(default=None, alias="Plan")
    rows: List[Dict[str, Any]] = Field(default_factory=list, alias="Rows")
    nodes: List[NodeModel] = Field(default_factory=list, alias="Nodes")
    edges: List[EdgeModel] = Field(default_factory=list, alias="Edges")
    labels: List[LabelModel] = Field(default_factory=list, alias="Labels")
    tags: List[TagModel] = Field(default_factory=list, alias="Tags")
    vectors: List[VectorMetadataModel] = Field(default_factory=list, alias="Vectors")
    vector_search_results: List[VectorSearchResultModel] = Field(
        default_factory=list, alias="VectorSearchResults"
    )
    row_count: int = Field(default=0, alias="RowCount")
    model_config = ConfigDict(populate_by_name=True)

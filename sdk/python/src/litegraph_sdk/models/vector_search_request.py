from typing import List, Optional
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field


class VectorSearchRequestModel(BaseModel):
    """Request model for vector search operations."""

    tenant_guid: Optional[UUID] = Field(None, alias="TenantGUID")
    graph_guid: Optional[UUID] = Field(None, alias="GraphGUID")
    domain: Optional[str] = Field(None, alias="Domain")
    search_type: Optional[str] = Field("CosineDistance", alias="SearchType")
    vectors: Optional[List[float]] = Field(None, alias="Vectors")
    top_k: Optional[int] = Field(10, alias="TopK")
    minimum_score: Optional[float] = Field(None, alias="MinimumScore")
    maximum_distance: Optional[float] = Field(None, alias="MaximumDistance")
    minimum_inner_product: Optional[float] = Field(None, alias="MinimumInnerProduct")
    labels: Optional[List[str]] = Field(None, alias="Labels")
    tags: Optional[dict] = Field(None, alias="Tags")
    filter: Optional[dict] = Field(None, alias="Filter")

    model_config = ConfigDict(populate_by_name=True, json_encoders={UUID: str})

from typing import Optional
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field


class VectorSearchResultModel(BaseModel):
    """Result model for vector search operations."""

    score: Optional[float] = Field(None, alias="Score")
    distance: Optional[float] = Field(None, alias="Distance")
    inner_product: Optional[float] = Field(None, alias="InnerProduct")
    graph_guid: Optional[UUID] = Field(None, alias="GraphGUID")
    node_guid: Optional[UUID] = Field(None, alias="NodeGUID")
    edge_guid: Optional[UUID] = Field(None, alias="EdgeGUID")
    graph: Optional[dict] = Field(None, alias="Graph")
    node: Optional[dict] = Field(None, alias="Node")
    edge: Optional[dict] = Field(None, alias="Edge")

    model_config = ConfigDict(populate_by_name=True, json_encoders={UUID: str})

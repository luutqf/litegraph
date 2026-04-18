from typing import Any, List, Optional

from pydantic import BaseModel, ConfigDict, Field


class TransactionOperationModel(BaseModel):
    """
    A single graph-scoped transaction operation.
    """

    operation_type: str = Field(default="Create", alias="OperationType")
    object_type: str = Field(default="Node", alias="ObjectType")
    guid: Optional[str] = Field(default=None, alias="GUID")
    payload: Optional[Any] = Field(default=None, alias="Payload")
    model_config = ConfigDict(populate_by_name=True)


class TransactionOperationResultModel(BaseModel):
    """
    Result for a single graph transaction operation.
    """

    index: int = Field(default=0, alias="Index")
    operation_type: str = Field(default="Create", alias="OperationType")
    object_type: str = Field(default="Node", alias="ObjectType")
    guid: Optional[str] = Field(default=None, alias="GUID")
    success: bool = Field(default=True, alias="Success")
    result: Optional[Any] = Field(default=None, alias="Result")
    error: Optional[str] = Field(default=None, alias="Error")
    model_config = ConfigDict(populate_by_name=True)


class TransactionRequestModel(BaseModel):
    """
    Graph-scoped transaction request.
    """

    operations: List[TransactionOperationModel] = Field(
        default_factory=list, alias="Operations"
    )
    max_operations: int = Field(default=1000, alias="MaxOperations", ge=1, le=10000)
    timeout_seconds: int = Field(default=60, alias="TimeoutSeconds", ge=1, le=3600)
    model_config = ConfigDict(populate_by_name=True)


class TransactionResultModel(BaseModel):
    """
    Graph-scoped transaction result.
    """

    success: bool = Field(default=True, alias="Success")
    rolled_back: bool = Field(default=False, alias="RolledBack")
    failed_operation_index: Optional[int] = Field(
        default=None, alias="FailedOperationIndex"
    )
    error: Optional[str] = Field(default=None, alias="Error")
    operations: List[TransactionOperationResultModel] = Field(
        default_factory=list, alias="Operations"
    )
    duration_ms: float = Field(default=0, alias="DurationMs")
    model_config = ConfigDict(populate_by_name=True)

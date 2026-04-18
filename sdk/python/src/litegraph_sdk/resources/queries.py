from typing import Any, Dict, Optional, Union

from ..configuration import get_client
from ..exceptions import GRAPH_REQUIRED_ERROR, TENANT_REQUIRED_ERROR
from ..mixins import JSON_CONTENT_TYPE
from ..models.query import GraphQueryRequestModel, GraphQueryResultModel


class Query:
    """
    Native graph query helpers.
    """

    @classmethod
    def request(
        cls,
        query: str,
        parameters: Optional[Dict[str, Any]] = None,
        max_results: int = 1000,
        timeout_seconds: int = 30,
        include_profile: bool = False,
    ) -> GraphQueryRequestModel:
        return GraphQueryRequestModel(
            Query=query,
            Parameters=parameters or {},
            MaxResults=max_results,
            TimeoutSeconds=timeout_seconds,
            IncludeProfile=include_profile,
        )

    @classmethod
    def execute(
        cls,
        request: Union[GraphQueryRequestModel, Dict[str, Any], str],
        graph_guid: Optional[str] = None,
        parameters: Optional[Dict[str, Any]] = None,
        max_results: int = 1000,
        timeout_seconds: int = 30,
        include_profile: bool = False,
    ) -> GraphQueryResultModel:
        client = get_client()
        if client.tenant_guid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        gid = graph_guid or client.graph_guid
        if not gid:
            raise ValueError(GRAPH_REQUIRED_ERROR)

        if isinstance(request, str):
            request = cls.request(
                request,
                parameters=parameters,
                max_results=max_results,
                timeout_seconds=timeout_seconds,
                include_profile=include_profile,
            )
        elif isinstance(request, dict):
            request = GraphQueryRequestModel.model_validate(request)
        elif not isinstance(request, GraphQueryRequestModel):
            raise TypeError("request must be a GraphQueryRequestModel, dict, or query string")

        data = request.model_dump(mode="json", by_alias=True, exclude_none=True)
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/query"
        response = client.request("POST", url, json=data, headers=JSON_CONTENT_TYPE)
        return GraphQueryResultModel.model_validate(response)

from typing import Type

from pydantic import BaseModel

from ..configuration import get_client
from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    DeletableAPIResource,
    ExistsAPIResource,
    ExportGexfMixin,
    RetrievableAPIResource,
    SearchableAPIResource,
    UpdatableAPIResource,
)
from ..models.existence_request import ExistenceRequestModel
from ..models.existence_result import ExistenceResultModel
from ..models.graphs import GraphModel
from ..models.search_graphs import SearchRequestGraph, SearchResultGraph
from ..utils.url_helper import _get_url


class Graph(
    ExistsAPIResource,
    CreateableAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
    ExportGexfMixin,
    SearchableAPIResource,
):
    """
    Graph resource class.
    """

    RESOURCE_NAME: str = "graphs"
    REQUIRE_GRAPH_GUID: bool = False
    MODEL = GraphModel
    SEARCH_MODELS = SearchRequestGraph, SearchResultGraph
    EXISTENCE_REQUEST_MODEL: Type[BaseModel] = ExistenceRequestModel
    EXISTENCE_RESPONSE_MODEL: Type[BaseModel] = ExistenceResultModel

    @classmethod
    def delete(cls, resource_id: str, force: bool = False) -> None:
        """
        Delete a resource by its ID.
        """
        client = get_client()
        graph_id = client.graph_guid if cls.REQUIRE_GRAPH_GUID else None

        if cls.REQUIRE_GRAPH_GUID and graph_id is None:
            raise ValueError("Graph GUID is required for this resource.")

        url = (
            _get_url(cls, graph_id, resource_id, force=None)
            if force
            else _get_url(cls, graph_id, resource_id)
        )
        client.request("DELETE", url)

    @classmethod
    def batch_existence(
        cls, graph_guid: str, request: ExistenceRequestModel
    ) -> ExistenceResultModel:
        """
        Execute a batch existence request.
        """
        if request is None:
            raise ValueError("Request cannot be None")

        if not isinstance(request, cls.EXISTENCE_REQUEST_MODEL):
            raise TypeError(
                f"Request must be an instance of {cls.EXISTENCE_REQUEST_MODEL.__name__}"
            )

        if not request.contains_existence_request():
            raise ValueError("Request must contain at least one existence check")

        client = get_client()

        # Construct URL
        url = _get_url(cls, graph_guid, "existence")

        # Prepare request data
        data = request.model_dump(mode="json", by_alias=True)

        # Make the request
        headers = {"Content-Type": "application/json"}
        response = client.request(method="POST", url=url, json=data, headers=headers)

        # Parse and validate response

        return cls.EXISTENCE_RESPONSE_MODEL.model_validate(response)

    @classmethod
    def export_gexf(cls, graph_id: str, include_data: bool = False) -> str:
        params = {}
        if include_data:
            params["incldata"] = None
        return super().export_gexf(graph_id, **params)

    @classmethod
    def get_statistics(cls, graph_guid: str = None, **kwargs):
        """Get graph statistics."""
        client = get_client()
        if graph_guid:
            url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/stats"
        else:
            url = f"v1.0/tenants/{client.tenant_guid}/graphs/stats"
        return client.request("GET", url)

    @classmethod
    def enable_vector_index(cls, graph_guid: str, config: dict, **kwargs):
        """Enable vector indexing on a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex/enable"
        return client.request("PUT", url, json=config)

    @classmethod
    def disable_vector_index(cls, graph_guid: str, **kwargs):
        """Disable vector indexing on a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex"
        return client.request("DELETE", url)

    @classmethod
    def rebuild_vector_index(cls, graph_guid: str, **kwargs):
        """Rebuild the vector index for a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex/rebuild"
        return client.request("POST", url)

    @classmethod
    def get_vector_index_config(cls, graph_guid: str, **kwargs):
        """Get the vector index configuration for a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex/config"
        return client.request("GET", url)

    @classmethod
    def get_vector_index_stats(cls, graph_guid: str, **kwargs):
        """Get vector index statistics for a graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectorindex/stats"
        return client.request("GET", url)

    @classmethod
    def get_subgraph(cls, graph_guid: str, node_guid: str, **kwargs):
        """Get subgraph starting from a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/subgraph"
        return client.request("GET", url)

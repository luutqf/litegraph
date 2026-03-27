from typing import List

from ..configuration import get_client
from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    DeletableAPIResource,
    DeleteMultipleAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    UpdatableAPIResource,
)
from ..models.vector_metadata import VectorMetadataModel
from ..models.vector_search_request import VectorSearchRequestModel
from ..models.vector_search_result import VectorSearchResultModel


class Vector(
    ExistsAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
    DeleteMultipleAPIResource,
):
    """
    Vector resource class.
    """

    RESOURCE_NAME: str = "vectors"
    MODEL = VectorMetadataModel
    REQUIRE_GRAPH_GUID: bool = False

    @classmethod
    def search(cls, search_request: VectorSearchRequestModel, **kwargs) -> List[VectorSearchResultModel]:
        """Search vectors using similarity search."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/vectors/search"
        data = search_request.model_dump(by_alias=True, exclude_none=True)
        response = client.request("POST", url, json=data)
        if isinstance(response, list):
            return [VectorSearchResultModel(**item) for item in response]
        return response

    @classmethod
    def read_graph_vectors(cls, graph_guid: str, **kwargs):
        """Read all vectors for a specific graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectors"
        return client.request("GET", url)

    @classmethod
    def read_node_vectors(cls, graph_guid: str, node_guid: str, **kwargs):
        """Read all vectors for a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/vectors"
        return client.request("GET", url)

    @classmethod
    def read_edge_vectors(cls, graph_guid: str, edge_guid: str, **kwargs):
        """Read all vectors for a specific edge."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/vectors"
        return client.request("GET", url)

    @classmethod
    def delete_graph_vectors(cls, graph_guid: str, **kwargs):
        """Delete all vectors for a specific graph."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/vectors"
        return client.request("DELETE", url)

    @classmethod
    def delete_node_vectors(cls, graph_guid: str, node_guid: str, **kwargs):
        """Delete all vectors for a specific node."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/nodes/{node_guid}/vectors"
        return client.request("DELETE", url)

    @classmethod
    def delete_edge_vectors(cls, graph_guid: str, edge_guid: str, **kwargs):
        """Delete all vectors for a specific edge."""
        client = get_client()
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{graph_guid}/edges/{edge_guid}/vectors"
        return client.request("DELETE", url)

from ..configuration import get_client
from ..mixins import (
    AllRetrievableAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    DeletableAPIResource,
    ExistsAPIResource,
    RetrievableAPIResource,
    SearchableAPIResource,
    UpdatableAPIResource,
    DeleteMultipleAPIResource,
    DeleteAllAPIResource
)
from ..models.node import NodeModel
from ..models.search_node_edge import SearchRequest, SearchResult


class Node(
    ExistsAPIResource,
    CreateableAPIResource,
    CreateableMultipleAPIResource,
    RetrievableAPIResource,
    AllRetrievableAPIResource,
    UpdatableAPIResource,
    DeletableAPIResource,
    SearchableAPIResource,
    DeleteMultipleAPIResource,
    DeleteAllAPIResource,
):
    """
    Node resource class.
    """

    RESOURCE_NAME: str = "nodes"
    MODEL = NodeModel
    SEARCH_MODELS = SearchRequest, SearchResult

    @classmethod
    def read_most_connected(cls, graph_guid: str = None, **kwargs):
        """Read the most connected nodes in a graph."""
        client = get_client()
        gid = graph_guid or client.graph_guid
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/nodes/mostconnected"
        return client.request("GET", url)

    @classmethod
    def read_least_connected(cls, graph_guid: str = None, **kwargs):
        """Read the least connected nodes in a graph."""
        client = get_client()
        gid = graph_guid or client.graph_guid
        url = f"v1.0/tenants/{client.tenant_guid}/graphs/{gid}/nodes/leastconnected"
        return client.request("GET", url)

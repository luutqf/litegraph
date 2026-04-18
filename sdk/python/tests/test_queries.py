from unittest.mock import Mock

import pytest

from litegraph_sdk.base import BaseClient
from litegraph_sdk.models.query import GraphQueryRequestModel
from litegraph_sdk.resources.queries import Query


@pytest.fixture
def mock_client(monkeypatch):
    client = Mock(spec=BaseClient)
    client.tenant_guid = "tenant-1"
    client.graph_guid = "graph-1"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def test_query_request_serializes_parameters_and_profile():
    request = Query.request(
        "MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 1",
        parameters={"name": "Ada"},
        max_results=25,
        timeout_seconds=12,
        include_profile=True,
    )

    payload = request.model_dump(mode="json", by_alias=True, exclude_none=True)

    assert payload["Query"].startswith("MATCH")
    assert payload["Parameters"] == {"name": "Ada"}
    assert payload["MaxResults"] == 25
    assert payload["TimeoutSeconds"] == 12
    assert payload["IncludeProfile"] is True


def test_query_execute_posts_to_graph_scoped_endpoint(mock_client):
    mock_client.request.return_value = {
        "Profile": "LiteGraph Cypher/GQL-inspired",
        "Mutated": False,
        "Rows": [{"n": {"GUID": "node-1", "Name": "Ada"}}],
        "Nodes": [{"GUID": "node-1", "Name": "Ada"}],
        "Edges": [],
        "Labels": [],
        "Tags": [],
        "Vectors": [],
        "VectorSearchResults": [],
        "RowCount": 1,
        "Plan": {"Kind": "MatchNode", "Mutates": False, "EstimatedCost": 10},
    }

    result = Query.execute(
        "MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 1",
        parameters={"name": "Ada"},
    )

    mock_client.request.assert_called_once()
    args, kwargs = mock_client.request.call_args
    assert args[0] == "POST"
    assert args[1] == "v1.0/tenants/tenant-1/graphs/graph-1/query"
    assert kwargs["json"]["Parameters"]["name"] == "Ada"
    assert result.row_count == 1
    assert result.nodes[0].name == "Ada"
    assert result.plan.kind == "MatchNode"


def test_query_execute_accepts_request_model_and_graph_override(mock_client):
    mock_client.request.return_value = {"Rows": [], "RowCount": 0}
    request = GraphQueryRequestModel(Query="OPTIONAL MATCH (n) RETURN n LIMIT 1")

    Query.execute(request, graph_guid="graph-2")

    args, kwargs = mock_client.request.call_args
    assert args[1] == "v1.0/tenants/tenant-1/graphs/graph-2/query"
    assert kwargs["json"]["Query"].startswith("OPTIONAL MATCH")


def test_query_requires_graph_guid(mock_client):
    mock_client.graph_guid = None

    with pytest.raises(ValueError, match="Graph GUID is required"):
        Query.execute("MATCH (n) RETURN n")

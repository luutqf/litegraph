from unittest.mock import Mock

import pytest

from litegraph_sdk.base import BaseClient
from litegraph_sdk.models.node import NodeModel
from litegraph_sdk.models.transaction import TransactionRequestModel
from litegraph_sdk.resources.transactions import Transaction


@pytest.fixture
def mock_client(monkeypatch):
    client = Mock(spec=BaseClient)
    client.tenant_guid = "tenant-1"
    client.graph_guid = "graph-1"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def test_transaction_builder_serializes_typed_operations(mock_client):
    request = (
        Transaction.builder()
        .with_max_operations(10)
        .with_timeout_seconds(30)
        .create_node(
            NodeModel(
                GUID="node-1",
                TenantGUID="tenant-1",
                GraphGUID="graph-1",
                Name="Ada",
            )
        )
        .attach_tag(
            {"GUID": "tag-1", "NodeGUID": "node-1", "Key": "role", "Value": "engineer"}
        )
        .delete_edge("edge-1")
        .build()
    )

    payload = request.model_dump(mode="json", by_alias=True, exclude_none=True)

    assert payload["MaxOperations"] == 10
    assert payload["TimeoutSeconds"] == 30
    assert payload["Operations"][0]["OperationType"] == "Create"
    assert payload["Operations"][0]["ObjectType"] == "Node"
    assert payload["Operations"][0]["Payload"]["Name"] == "Ada"
    assert payload["Operations"][1]["OperationType"] == "Attach"
    assert payload["Operations"][1]["ObjectType"] == "Tag"
    assert payload["Operations"][2]["OperationType"] == "Delete"
    assert payload["Operations"][2]["GUID"] == "edge-1"


def test_transaction_execute_posts_to_graph_scoped_endpoint(mock_client):
    mock_client.request.return_value = {
        "Success": True,
        "RolledBack": False,
        "Operations": [
            {
                "Index": 0,
                "OperationType": "Create",
                "ObjectType": "Node",
                "GUID": "node-1",
                "Success": True,
                "Result": {"GUID": "node-1"},
            }
        ],
        "DurationMs": 2.5,
    }

    result = Transaction.execute(
        Transaction.builder().create_node({"GUID": "node-1", "Name": "Ada"})
    )

    mock_client.request.assert_called_once()
    args, kwargs = mock_client.request.call_args
    assert args[0] == "POST"
    assert args[1] == "v1.0/tenants/tenant-1/graphs/graph-1/transaction"
    assert kwargs["json"]["Operations"][0]["Payload"]["Name"] == "Ada"
    assert result.success is True
    assert result.operations[0].guid == "node-1"


def test_transaction_context_executes_on_clean_exit(mock_client):
    mock_client.request.return_value = {
        "Success": False,
        "RolledBack": True,
        "FailedOperationIndex": 0,
        "Error": "duplicate node",
        "Operations": [
            {
                "Index": 0,
                "OperationType": "Create",
                "ObjectType": "Node",
                "GUID": "node-1",
                "Success": False,
                "Error": "duplicate node",
            }
        ],
    }

    with Transaction.context() as tx:
        tx.create_node({"GUID": "node-1", "Name": "Ada"})

    assert tx.result is not None
    assert tx.result.success is False
    assert tx.result.rolled_back is True
    assert tx.result.failed_operation_index == 0


def test_transaction_requires_graph_guid(mock_client):
    mock_client.graph_guid = None
    request = TransactionRequestModel(Operations=[])

    with pytest.raises(ValueError, match="Graph GUID is required"):
        Transaction.execute(request)

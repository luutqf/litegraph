namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using Touchstone.Core;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Touchstone test cases for the MCP protocol surface exposed by LiteGraph.McpServer through Voltaic:
    /// the stateless 2026-07-28 Streamable HTTP transport, tools/call argument handling, and handshake negotiation.
    /// </summary>
    public static partial class LiteGraphTouchstoneSuites
    {
        #region Private-Members

        private const string _McpStatelessVersion = "2026-07-28";
        private const string _McpNewestHandshakeVersion = "2025-11-25";

        #endregion

        #region Private-Methods

        private static TestSuiteDescriptor CreateMcpProtocolSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Mcp.Protocol",
                displayName: "MCP protocol surface: stateless transport, tools/call, and handshake negotiation",
                cases: new List<TestCaseDescriptor>
                {
                    McpProtocolCase("Mcp.Protocol.StatelessDiscover", "server/discover advertises 2026-07-28 with resultType complete", TestMcpStatelessDiscover),
                    McpProtocolCase("Mcp.Protocol.StatelessToolsList", "Stateless tools/list returns LiteGraph tools with resultType and cache fields", TestMcpStatelessToolsList),
                    McpProtocolCase("Mcp.Protocol.StatelessToolCall", "Stateless tools/call reads the default tenant", TestMcpStatelessToolCall),
                    McpProtocolCase("Mcp.Protocol.StatelessToolCallRoundTrip", "Stateless tools/call creates, reads, and deletes a graph", TestMcpStatelessToolCallRoundTrip),
                    McpProtocolCase("Mcp.Protocol.StatelessToolCallMissingArgument", "Stateless tools/call without a required argument is rejected", TestMcpStatelessToolCallMissingArgument),
                    McpProtocolCase("Mcp.Protocol.StatelessUnknownTool", "Stateless tools/call for an unknown tool is rejected", TestMcpStatelessUnknownTool),
                    McpProtocolCase("Mcp.Protocol.HandshakeToolCall", "Handshake tools/call passes arguments through to the handler", TestMcpHandshakeToolCall),
                    McpProtocolCase("Mcp.Protocol.MethodCallWithoutParams", "A direct method call without params is rejected by the handler", TestMcpMethodCallWithoutParams),
                    McpProtocolCase("Mcp.Protocol.InitializeCapsHandshakeVersion", "initialize requesting 2026-07-28 negotiates the newest handshake revision", TestMcpInitializeCapsHandshakeVersion)
                },
                afterSuiteAsync: CleanupMcpSuiteAsync);
        }

        private static TestCaseDescriptor McpProtocolCase(string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(
                suiteId: "Mcp.Protocol",
                caseId: caseId,
                displayName: displayName,
                executeAsync: async ct =>
                {
                    try
                    {
                        await executeAsync(ct).ConfigureAwait(false);
                    }
                    catch
                    {
                        _PreserveMcpArtifactsOnCleanup = true;
                        throw;
                    }
                });
        }

        private static async Task<McpHttpClient> ConnectStatelessMcpClientAsync(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment is not running.");

            McpHttpClient client = new McpHttpClient();
            ConfigureMcpHttpClient(client, 30000);

            bool connected = await client.ConnectStatelessAsync(
                _McpEnvironment.McpHttpEndpoint,
                "/mcp",
                _McpStatelessVersion,
                autoNegotiate: false,
                token: cancellationToken).ConfigureAwait(false);

            if (!connected)
            {
                client.Dispose();
                throw new InvalidOperationException("Unable to connect a stateless MCP client to " + _McpEnvironment.McpHttpEndpoint + "/mcp");
            }

            return client;
        }

        private static async Task TestMcpStatelessDiscover(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                AssertTrue(client.IsStateless, "Client is in stateless mode");

                McpDiscoverResult discover = await client.DiscoverAsync(cancellationToken).ConfigureAwait(false);
                AssertTrue(discover.SupportedVersions.Contains(_McpStatelessVersion), "server/discover advertises " + _McpStatelessVersion);
                AssertEqual(McpResult.ResultTypeComplete, discover.ResultType, "server/discover carries resultType complete");
                AssertTrue(discover.TtlMs != null, "server/discover carries ttlMs");
                AssertTrue(!String.IsNullOrEmpty(discover.CacheScope), "server/discover carries cacheScope");
            }
        }

        private static async Task TestMcpStatelessToolsList(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
                string? cursor = null;
                int pages = 0;

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Dictionary<string, object?>? parameters = cursor == null
                        ? null
                        : new Dictionary<string, object?> { { "cursor", cursor } };

                    JsonRpcResponse response = await client.SendStatelessAsync("tools/list", parameters, null, cancellationToken).ConfigureAwait(false);
                    AssertTrue(response.Error == null, "tools/list page " + (pages + 1) + " succeeds (" + DescribeRpcError(response) + ")");
                    pages++;

                    using (JsonDocument result = ParseRpcResult(response))
                    {
                        JsonElement root = result.RootElement;
                        AssertEqual(McpResult.ResultTypeComplete, GetStringProperty(root, "resultType"), "tools/list carries resultType complete");
                        AssertTrue(root.TryGetProperty("ttlMs", out JsonElement _), "tools/list carries ttlMs");
                        AssertTrue(root.TryGetProperty("cacheScope", out JsonElement _), "tools/list carries cacheScope");
                        AssertTrue(root.TryGetProperty("tools", out JsonElement tools) && tools.ValueKind == JsonValueKind.Array, "tools/list returns a tools array");

                        foreach (JsonElement tool in tools.EnumerateArray())
                        {
                            string? name = GetStringProperty(tool, "name");
                            if (!String.IsNullOrEmpty(name)) names.Add(name);
                        }

                        cursor = GetStringProperty(root, "nextCursor");
                    }
                }
                while (!String.IsNullOrEmpty(cursor) && pages < 50);

                AssertTrue(String.IsNullOrEmpty(cursor), "tools/list pagination terminates");
                AssertTrue(names.Count > 100, "tools/list pages return the full LiteGraph catalog (" + names.Count + " tools across " + pages + " pages)");
                foreach (string expected in new[] { "tenant/get", "graph/create", "node/search", "edge/create", "vector/search", "authorization/role/create" })
                {
                    AssertTrue(names.Contains(expected), "tools/list includes '" + expected + "'");
                }
            }
        }

        private static async Task TestMcpStatelessToolCall(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                JsonRpcResponse response = await client.CallToolStatelessAsync(
                    "tenant/get",
                    new { tenantGuid = _DefaultTenantGuid },
                    token: cancellationToken).ConfigureAwait(false);

                AssertTrue(response.Error == null, "tools/call tenant/get succeeds (" + DescribeRpcError(response) + ")");

                using (JsonDocument result = ParseRpcResult(response))
                {
                    AssertEqual(McpResult.ResultTypeComplete, GetStringProperty(result.RootElement, "resultType"), "tools/call carries resultType complete");
                    AssertFalse(IsToolError(result.RootElement), "tools/call result is not an error");

                    string text = GetToolText(result.RootElement);
                    TenantMetadata? tenant = _McpSerializer.DeserializeJson<TenantMetadata>(text);
                    AssertNotNull(tenant, "tools/call content deserializes to a tenant");
                    AssertEqual(Guid.Parse(_DefaultTenantGuid), tenant!.GUID, "tools/call returns the default tenant");
                }
            }
        }

        private static async Task TestMcpStatelessToolCallRoundTrip(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                string graphName = "mcp-stateless-" + Guid.NewGuid().ToString("N");

                JsonRpcResponse created = await client.CallToolStatelessAsync(
                    "graph/create",
                    new { tenantGuid = _DefaultTenantGuid, name = graphName },
                    token: cancellationToken).ConfigureAwait(false);
                AssertTrue(created.Error == null, "tools/call graph/create succeeds (" + DescribeRpcError(created) + ")");

                Graph? graph;
                using (JsonDocument result = ParseRpcResult(created))
                {
                    AssertFalse(IsToolError(result.RootElement), "graph/create result is not an error");
                    graph = _McpSerializer.DeserializeJson<Graph>(GetToolText(result.RootElement));
                }

                AssertNotNull(graph, "graph/create returns a graph");
                AssertEqual(graphName, graph!.Name, "graph/create stores the name");

                JsonRpcResponse read = await client.CallToolStatelessAsync(
                    "graph/get",
                    new { tenantGuid = _DefaultTenantGuid, graphGuid = graph.GUID.ToString() },
                    token: cancellationToken).ConfigureAwait(false);
                AssertTrue(read.Error == null, "tools/call graph/get succeeds (" + DescribeRpcError(read) + ")");

                using (JsonDocument result = ParseRpcResult(read))
                {
                    Graph? readGraph = _McpSerializer.DeserializeJson<Graph>(GetToolText(result.RootElement));
                    AssertNotNull(readGraph, "graph/get returns a graph");
                    AssertEqual(graph.GUID, readGraph!.GUID, "graph/get returns the created graph");
                }

                JsonRpcResponse deleted = await client.CallToolStatelessAsync(
                    "graph/delete",
                    new { tenantGuid = _DefaultTenantGuid, graphGuid = graph.GUID.ToString(), force = true },
                    token: cancellationToken).ConfigureAwait(false);
                AssertTrue(deleted.Error == null, "tools/call graph/delete succeeds (" + DescribeRpcError(deleted) + ")");

                using (JsonDocument result = ParseRpcResult(deleted))
                {
                    AssertFalse(IsToolError(result.RootElement), "graph/delete result is not an error");
                }
            }
        }

        private static async Task TestMcpStatelessToolCallMissingArgument(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                JsonRpcResponse response = await client.CallToolStatelessAsync(
                    "tenant/get",
                    new { },
                    token: cancellationToken).ConfigureAwait(false);

                bool rejected = response.Error != null;
                if (!rejected)
                {
                    using (JsonDocument result = ParseRpcResult(response))
                    {
                        rejected = IsToolError(result.RootElement);
                    }
                }

                AssertTrue(rejected, "tools/call tenant/get without tenantGuid is rejected");
                if (response.Error != null)
                {
                    AssertTrue(
                        (response.Error.Message ?? "").Contains("tenantGuid"),
                        "The rejection names the missing argument (" + response.Error.Message + ")");
                }
            }
        }

        private static async Task TestMcpStatelessUnknownTool(CancellationToken cancellationToken)
        {
            using (McpHttpClient client = await ConnectStatelessMcpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                JsonRpcResponse response = await client.CallToolStatelessAsync(
                    "no/such/tool",
                    new { },
                    token: cancellationToken).ConfigureAwait(false);

                AssertTrue(response.Error != null, "tools/call for an unknown tool returns a JSON-RPC error");
            }
        }

        private static async Task TestMcpHandshakeToolCall(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            JsonRpcResponse response = await _McpClient.CallAsync(
                "tools/call",
                new { name = "tenant/get", arguments = new { tenantGuid = _DefaultTenantGuid } },
                token: cancellationToken).ConfigureAwait(false);

            AssertTrue(response.Error == null, "Handshake tools/call succeeds (" + DescribeRpcError(response) + ")");

            using (JsonDocument result = ParseRpcResult(response))
            {
                AssertFalse(IsToolError(result.RootElement), "Handshake tools/call result is not an error");
                TenantMetadata? tenant = _McpSerializer.DeserializeJson<TenantMetadata>(GetToolText(result.RootElement));
                AssertNotNull(tenant, "Handshake tools/call returns a tenant");
                AssertEqual(Guid.Parse(_DefaultTenantGuid), tenant!.GUID, "Handshake tools/call returns the default tenant");
            }
        }

        private static async Task TestMcpMethodCallWithoutParams(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpClient == null) throw new InvalidOperationException("MCP client is null");

            JsonRpcResponse response = await _McpClient.CallAsync("tenant/get", null, token: cancellationToken).ConfigureAwait(false);
            AssertTrue(response.Error != null, "tenant/get without params returns a JSON-RPC error");

            JsonRpcResponse missingName = await _McpClient.CallAsync("graph/create", new { tenantGuid = _DefaultTenantGuid }, token: cancellationToken).ConfigureAwait(false);
            AssertTrue(missingName.Error != null, "graph/create without a name returns a JSON-RPC error");
        }

        private static async Task TestMcpInitializeCapsHandshakeVersion(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);
            if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment is not running.");

            string body =
                "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{"
                + "\"protocolVersion\":\"" + _McpStatelessVersion + "\","
                + "\"capabilities\":{},"
                + "\"clientInfo\":{\"name\":\"litegraph-touchstone\",\"version\":\"1.0.0\"}}}";

            using (HttpClient http = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _McpEnvironment.McpHttpEndpoint + "/mcp"))
            {
                http.Timeout = TimeSpan.FromSeconds(30);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.Accept.ParseAdd("text/event-stream");

                using (HttpResponseMessage response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    AssertEqual(200, (int)response.StatusCode, "initialize responds (body " + Truncate(responseBody, 200) + ")");

                    using (JsonDocument document = JsonDocument.Parse(ExtractJsonRpcPayload(responseBody)))
                    {
                        AssertTrue(document.RootElement.TryGetProperty("result", out JsonElement result), "initialize returns a result (body " + Truncate(responseBody, 200) + ")");
                        AssertEqual(_McpNewestHandshakeVersion, GetStringProperty(result, "protocolVersion"), "initialize negotiates the newest handshake revision rather than " + _McpStatelessVersion);
                    }
                }
            }
        }

        private static JsonDocument ParseRpcResult(JsonRpcResponse response)
        {
            if (response.Result == null) throw new InvalidOperationException("JSON-RPC response has no result.");
            return JsonDocument.Parse(JsonSerializer.Serialize(response.Result));
        }

        private static string DescribeRpcError(JsonRpcResponse response)
        {
            if (response.Error == null) return "no error";
            return "error " + response.Error.Code + ": " + response.Error.Message;
        }

        private static string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            if (!element.TryGetProperty(propertyName, out JsonElement value)) return null;
            return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }

        private static bool IsToolError(JsonElement result)
        {
            return result.TryGetProperty("isError", out JsonElement isError) && isError.ValueKind == JsonValueKind.True;
        }

        private static string GetToolText(JsonElement result)
        {
            if (!result.TryGetProperty("content", out JsonElement content) || content.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Tool result has no content array.");

            foreach (JsonElement item in content.EnumerateArray())
            {
                string? text = GetStringProperty(item, "text");
                if (text != null) return text;
            }

            throw new InvalidOperationException("Tool result has no text content.");
        }

        private static string ExtractJsonRpcPayload(string body)
        {
            string trimmed = body.TrimStart();
            if (trimmed.StartsWith("{", StringComparison.Ordinal)) return trimmed;

            foreach (string line in body.Split('\n'))
            {
                string candidate = line.Trim();
                if (candidate.StartsWith("data:", StringComparison.Ordinal))
                {
                    string data = candidate.Substring(5).Trim();
                    if (data.StartsWith("{", StringComparison.Ordinal)) return data;
                }
            }

            throw new InvalidOperationException("Response does not contain a JSON-RPC payload: " + Truncate(body, 200));
        }

        #endregion
    }
}

namespace LiteGraph.McpServer.Classes
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net;
    using System.Text;
    using LiteGraph.Sdk;

    /// <summary>
    /// Minimal authenticated REST bridge for MCP operations that must honor server-side RBAC.
    /// </summary>
    internal static class LiteGraphMcpRestProxy
    {
        #region Private-Members

        private static readonly HttpClient _Http = new HttpClient();

        #endregion

        #region Public-Methods

        public static string SendJson(LiteGraphSdk sdk, HttpMethod method, string pathAndQuery, string? jsonBody = null)
        {
            return SendJson(sdk, method, pathAndQuery, jsonBody, true).Body;
        }

        public static string SendJsonOrNullOnNotFound(LiteGraphSdk sdk, HttpMethod method, string pathAndQuery, string? jsonBody = null)
        {
            RestResponse response = SendJson(sdk, method, pathAndQuery, jsonBody, false);
            if (response.IsSuccess) return response.Body;
            if (response.StatusCode == HttpStatusCode.NotFound) return "null";

            throw new InvalidOperationException(
                "LiteGraph endpoint returned "
                + (int)response.StatusCode
                + " "
                + response.ReasonPhrase
                + ": "
                + response.Body);
        }

        public static bool HeadExists(LiteGraphSdk sdk, string pathAndQuery)
        {
            RestResponse response = SendJson(sdk, HttpMethod.Head, pathAndQuery, null, false);
            if (response.IsSuccess) return true;
            if (response.StatusCode == HttpStatusCode.NotFound) return false;

            throw new InvalidOperationException(
                "LiteGraph endpoint returned "
                + (int)response.StatusCode
                + " "
                + response.ReasonPhrase
                + ": "
                + response.Body);
        }

        public static string Escape(Guid guid)
        {
            return Uri.EscapeDataString(guid.ToString());
        }

        public static string Escape(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Uri.EscapeDataString(value);
        }

        #endregion

        #region Private-Methods

        private static RestResponse SendJson(LiteGraphSdk sdk, HttpMethod method, string pathAndQuery, string? jsonBody, bool throwOnError)
        {
            if (sdk == null) throw new ArgumentNullException(nameof(sdk));
            if (String.IsNullOrEmpty(sdk.Endpoint)) throw new ArgumentNullException(nameof(sdk.Endpoint));
            if (String.IsNullOrEmpty(pathAndQuery)) throw new ArgumentNullException(nameof(pathAndQuery));

            string url = sdk.Endpoint.TrimEnd('/') + "/" + pathAndQuery.TrimStart('/');

            using (HttpRequestMessage request = new HttpRequestMessage(method, url))
            {
                if (!String.IsNullOrEmpty(sdk.BearerToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sdk.BearerToken);

                if (jsonBody != null)
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = _Http.Send(request))
                {
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode && throwOnError)
                    {
                        throw new InvalidOperationException(
                            "LiteGraph endpoint returned "
                            + (int)response.StatusCode
                            + " "
                            + response.ReasonPhrase
                            + ": "
                            + body);
                    }

                    return new RestResponse(response.StatusCode, response.ReasonPhrase ?? String.Empty, body, response.IsSuccessStatusCode);
                }
            }
        }

        #endregion

        #region Private-Classes

        private sealed class RestResponse
        {
            public HttpStatusCode StatusCode { get; }

            public string ReasonPhrase { get; }

            public string Body { get; }

            public bool IsSuccess { get; }

            public RestResponse(HttpStatusCode statusCode, string reasonPhrase, string body, bool isSuccess)
            {
                StatusCode = statusCode;
                ReasonPhrase = reasonPhrase;
                Body = body;
                IsSuccess = isSuccess;
            }
        }

        #endregion
    }
}

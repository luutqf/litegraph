namespace LiteGraph.McpServer.Classes
{
    using System;
    using System.Text.Json;

    /// <summary>
    /// Raised when the LiteGraph REST server answers an MCP-forwarded request with a non-success status.
    /// Carries the HTTP status and the REST error description so the MCP layer can report a meaningful error.
    /// Instances are immutable and safe to share across threads.
    /// </summary>
    public class LiteGraphRestException : Exception
    {
        #region Public-Members

        /// <summary>
        /// Label of the REST endpoint that failed, for example "LiteGraph endpoint" or "LiteGraph query endpoint".
        /// </summary>
        public string EndpointLabel { get; }

        /// <summary>
        /// HTTP status code returned by the REST server (for example 400, 401, 403, 404, 409, or 500).
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// HTTP reason phrase returned by the REST server; empty when none was supplied.
        /// </summary>
        public string ReasonPhrase { get; }

        /// <summary>
        /// Raw response body returned by the REST server; empty when none was supplied.
        /// </summary>
        public string ResponseBody { get; }

        /// <summary>
        /// Human-readable failure description extracted from the REST error body (its Description, then Message);
        /// null when the body is not a LiteGraph error object.
        /// </summary>
        public string? Description { get; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the exception from a REST response.
        /// </summary>
        /// <param name="endpointLabel">Label of the REST endpoint that failed; must not be null or empty.</param>
        /// <param name="statusCode">HTTP status code returned by the REST server.</param>
        /// <param name="reasonPhrase">HTTP reason phrase; null is treated as empty.</param>
        /// <param name="responseBody">Raw response body; null is treated as empty.</param>
        /// <exception cref="ArgumentNullException">Thrown when endpointLabel is null or empty.</exception>
        public LiteGraphRestException(string endpointLabel, int statusCode, string? reasonPhrase, string? responseBody)
            : base(BuildMessage(endpointLabel, statusCode, reasonPhrase, responseBody))
        {
            if (String.IsNullOrEmpty(endpointLabel)) throw new ArgumentNullException(nameof(endpointLabel));

            EndpointLabel = endpointLabel;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase ?? String.Empty;
            ResponseBody = responseBody ?? String.Empty;
            Description = ExtractDescription(ResponseBody);
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private static string BuildMessage(string endpointLabel, int statusCode, string? reasonPhrase, string? responseBody)
        {
            string? description = ExtractDescription(responseBody);
            string detail = description ?? (String.IsNullOrWhiteSpace(responseBody) ? "no response body" : responseBody);

            return (String.IsNullOrEmpty(endpointLabel) ? "LiteGraph endpoint" : endpointLabel)
                + " returned "
                + statusCode
                + (String.IsNullOrEmpty(reasonPhrase) ? "" : " " + reasonPhrase)
                + ": "
                + detail;
        }

        private static string? ExtractDescription(string? responseBody)
        {
            if (String.IsNullOrWhiteSpace(responseBody)) return null;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object) return null;

                    foreach (string property in new[] { "Description", "Message" })
                    {
                        if (root.TryGetProperty(property, out JsonElement value)
                            && value.ValueKind == JsonValueKind.String
                            && !String.IsNullOrWhiteSpace(value.GetString()))
                        {
                            return value.GetString();
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        #endregion
    }
}

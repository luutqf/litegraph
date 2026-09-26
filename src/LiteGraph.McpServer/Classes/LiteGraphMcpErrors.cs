namespace LiteGraph.McpServer.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.Json;
    using Voltaic.Mcp;

    /// <summary>
    /// Translates exceptions raised by LiteGraph MCP tool handlers into MCP protocol errors, so clients receive
    /// a specific JSON-RPC error code and a readable message instead of Voltaic's generic "-32603 Internal error".
    /// All members are stateless and thread-safe.
    /// </summary>
    public static class LiteGraphMcpErrors
    {
        #region Public-Members

        /// <summary>
        /// JSON-RPC invalid-params code (-32602), used for argument errors and REST 400 responses.
        /// </summary>
        public const int InvalidParamsCode = -32602;

        /// <summary>
        /// JSON-RPC internal-error code (-32603), used for unexpected failures; the message still carries the cause.
        /// </summary>
        public const int InternalErrorCode = -32603;

        /// <summary>
        /// Server-defined code (-32000) for REST failures that have no more specific mapping (for example 5xx responses).
        /// </summary>
        public const int UpstreamErrorCode = -32000;

        /// <summary>
        /// Server-defined code (-32001) for REST 401 Unauthorized responses.
        /// </summary>
        public const int UnauthorizedCode = -32001;

        /// <summary>
        /// Server-defined code (-32003) for REST 403 Forbidden responses.
        /// </summary>
        public const int ForbiddenCode = -32003;

        /// <summary>
        /// Server-defined code (-32004) for REST 404 Not Found responses and missing records.
        /// </summary>
        public const int NotFoundCode = -32004;

        /// <summary>
        /// Server-defined code (-32009) for REST 409 Conflict responses.
        /// </summary>
        public const int ConflictCode = -32009;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Translate an exception raised by a tool handler into an MCP protocol exception.
        /// An existing <see cref="McpProtocolException"/> is returned unchanged; wrapper exceptions are unwrapped first.
        /// </summary>
        /// <param name="exception">Exception raised by the handler; must not be null.</param>
        /// <returns>An MCP protocol exception with a specific code, a readable message, and structured data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
        public static McpProtocolException Translate(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            Exception ex = Unwrap(exception);

            if (ex is McpProtocolException protocolException) return protocolException;

            if (ex is LiteGraphRestException rest)
            {
                Dictionary<string, object?> data = new Dictionary<string, object?>
                {
                    { "statusCode", rest.StatusCode },
                    { "description", rest.Description }
                };

                return new McpProtocolException(CodeForStatus(rest.StatusCode), rest.Message, data);
            }

            if (ex is OperationCanceledException) return McpProtocolException.CancelledRequest(ex.Message);

            int code;
            if (ex is ArgumentException || ex is FormatException || ex is JsonException) code = InvalidParamsCode;
            else if (ex is KeyNotFoundException) code = NotFoundCode;
            else if (ex is UnauthorizedAccessException) code = UnauthorizedCode;
            else code = InternalErrorCode;

            string message = String.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
            if (ex is FormatException) message = "Invalid argument format: " + message;
            return new McpProtocolException(code, message, new Dictionary<string, object?> { { "exception", ex.GetType().Name } });
        }

        /// <summary>
        /// Map an HTTP status code returned by the LiteGraph REST server to a JSON-RPC error code.
        /// 400 maps to -32602, 401 to -32001, 403 to -32003, 404 to -32004, 409 to -32009, any other status to -32000.
        /// </summary>
        /// <param name="statusCode">HTTP status code.</param>
        /// <returns>JSON-RPC error code.</returns>
        public static int CodeForStatus(int statusCode)
        {
            switch (statusCode)
            {
                case 400:
                    return InvalidParamsCode;
                case 401:
                    return UnauthorizedCode;
                case 403:
                    return ForbiddenCode;
                case 404:
                    return NotFoundCode;
                case 409:
                    return ConflictCode;
                default:
                    return UpstreamErrorCode;
            }
        }

        #endregion

        #region Private-Methods

        private static Exception Unwrap(Exception exception)
        {
            Exception current = exception;

            while (true)
            {
                if (current is AggregateException aggregate && aggregate.InnerExceptions.Count == 1)
                    current = aggregate.InnerExceptions[0];
                else if (current is TargetInvocationException invocation && invocation.InnerException != null)
                    current = invocation.InnerException;
                else
                    return current;
            }
        }

        #endregion
    }
}

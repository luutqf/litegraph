namespace LiteGraph
{
    using System;

    /// <summary>
    /// Authorization denial audit entry.
    /// </summary>
    public class AuthorizationAuditEntry
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Timestamp from creation, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Request ID.
        /// </summary>
        public string RequestId { get; set; } = null;

        /// <summary>
        /// Correlation ID.
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Trace ID.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// User GUID.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// Credential GUID.
        /// </summary>
        public Guid? CredentialGUID { get; set; } = null;

        /// <summary>
        /// Request type.
        /// </summary>
        public string RequestType { get; set; } = null;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// Request path.
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Source IP address.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// Authentication result.
        /// </summary>
        public string AuthenticationResult { get; set; } = null;

        /// <summary>
        /// Authorization result.
        /// </summary>
        public string AuthorizationResult { get; set; } = null;

        /// <summary>
        /// Authorization denial reason.
        /// </summary>
        public string Reason { get; set; } = null;

        /// <summary>
        /// Required scope.
        /// </summary>
        public string RequiredScope { get; set; } = null;

        /// <summary>
        /// True if administrator authentication was used.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Optional description.
        /// </summary>
        public string Description { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthorizationAuditEntry()
        {
        }

        #endregion
    }
}

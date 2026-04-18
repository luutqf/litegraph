namespace LiteGraph
{
    using System;

    /// <summary>
    /// Search/filter parameters for authorization audit listing.
    /// </summary>
    public class AuthorizationAuditSearchRequest
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID filter.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Graph GUID filter.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// User GUID filter.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// Credential GUID filter.
        /// </summary>
        public Guid? CredentialGUID { get; set; } = null;

        /// <summary>
        /// Request ID filter.
        /// </summary>
        public string RequestId { get; set; } = null;

        /// <summary>
        /// Correlation ID filter.
        /// </summary>
        public string CorrelationId { get; set; } = null;

        /// <summary>
        /// Trace ID filter.
        /// </summary>
        public string TraceId { get; set; } = null;

        /// <summary>
        /// Request type filter.
        /// </summary>
        public string RequestType { get; set; } = null;

        /// <summary>
        /// Denial reason filter.
        /// </summary>
        public string Reason { get; set; } = null;

        /// <summary>
        /// Required scope filter.
        /// </summary>
        public string RequiredScope { get; set; } = null;

        /// <summary>
        /// Inclusive lower bound on CreatedUtc.
        /// </summary>
        public DateTime? FromUtc { get; set; } = null;

        /// <summary>
        /// Exclusive upper bound on CreatedUtc.
        /// </summary>
        public DateTime? ToUtc { get; set; } = null;

        /// <summary>
        /// Zero-based page index.
        /// </summary>
        public int Page
        {
            get
            {
                return _Page;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Page));
                _Page = value;
            }
        }

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize
        {
            get
            {
                return _PageSize;
            }
            set
            {
                if (value < 1 || value > 1000) throw new ArgumentOutOfRangeException(nameof(PageSize));
                _PageSize = value;
            }
        }

        #endregion

        #region Private-Members

        private int _Page = 0;
        private int _PageSize = 25;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthorizationAuditSearchRequest()
        {
        }

        #endregion
    }
}

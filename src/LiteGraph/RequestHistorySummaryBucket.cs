namespace LiteGraph
{
    using System;

    /// <summary>
    /// Single bucket of request history summary data.
    /// </summary>
    public class RequestHistorySummaryBucket
    {
        #region Public-Members

        /// <summary>
        /// Start of the bucket, in UTC.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of successful requests in this bucket.
        /// </summary>
        public long SuccessCount { get; set; } = 0;

        /// <summary>
        /// Number of failed requests in this bucket.
        /// </summary>
        public long FailureCount { get; set; } = 0;

        /// <summary>
        /// Total number of requests in this bucket.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySummaryBucket()
        {
        }

        #endregion
    }
}

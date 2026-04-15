namespace LiteGraph
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Bucketed summary of request history over a time window.
    /// </summary>
    public class RequestHistorySummary
    {
        #region Public-Members

        /// <summary>
        /// Start of the window, in UTC.
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// End of the window, in UTC.
        /// </summary>
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Bucket interval (minute, 15minute, hour, 6hour, day).
        /// </summary>
        public string Interval { get; set; } = null;

        /// <summary>
        /// Total number of successful requests.
        /// </summary>
        public long TotalSuccess { get; set; } = 0;

        /// <summary>
        /// Total number of failed requests.
        /// </summary>
        public long TotalFailure { get; set; } = 0;

        /// <summary>
        /// Total number of requests.
        /// </summary>
        public long TotalRequests { get; set; } = 0;

        /// <summary>
        /// Bucketed data.
        /// </summary>
        public List<RequestHistorySummaryBucket> Data { get; set; } = new List<RequestHistorySummaryBucket>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySummary()
        {
        }

        #endregion
    }
}

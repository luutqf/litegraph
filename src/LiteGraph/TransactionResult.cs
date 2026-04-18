namespace LiteGraph
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Graph-scoped transaction result.
    /// </summary>
    public class TransactionResult
    {
        #region Public-Members

        /// <summary>
        /// True if the transaction committed.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// True if the transaction rolled back.
        /// </summary>
        public bool RolledBack { get; set; } = false;

        /// <summary>
        /// Index of failed operation, if any.
        /// </summary>
        public int? FailedOperationIndex { get; set; } = null;

        /// <summary>
        /// Error message, if the transaction failed.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Operation results.
        /// </summary>
        public List<TransactionOperationResult> Operations { get; set; } = new List<TransactionOperationResult>();

        /// <summary>
        /// Transaction duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        #endregion
    }
}

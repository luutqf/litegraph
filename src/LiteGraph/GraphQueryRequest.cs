namespace LiteGraph
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Native LiteGraph graph query request.
    /// </summary>
    public class GraphQueryRequest
    {
        #region Public-Members

        /// <summary>
        /// Cypher/GQL-inspired LiteGraph-native query text.
        /// </summary>
        public string Query
        {
            get
            {
                return _Query;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Query));
                _Query = value;
            }
        }

        /// <summary>
        /// Query parameters.  Inline literals are allowed for simple values; parameters should be used for user input and large values.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maximum rows to return when the query omits LIMIT.
        /// </summary>
        public int MaxResults
        {
            get
            {
                return _MaxResults;
            }
            set
            {
                if (value < 1 || value > 10000) throw new ArgumentOutOfRangeException(nameof(MaxResults));
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Query timeout in seconds.
        /// </summary>
        public int TimeoutSeconds
        {
            get
            {
                return _TimeoutSeconds;
            }
            set
            {
                if (value < 1 || value > 3600) throw new ArgumentOutOfRangeException(nameof(TimeoutSeconds));
                _TimeoutSeconds = value;
            }
        }

        /// <summary>
        /// Include parse, plan, execute, and total timing details in the response.
        /// </summary>
        public bool IncludeProfile { get; set; } = false;

        #endregion

        #region Private-Members

        private string _Query = null;
        private int _MaxResults = 100;
        private int _TimeoutSeconds = 30;

        #endregion
    }
}

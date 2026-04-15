namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Request history capture and retention settings.
    /// </summary>
    public class RequestHistorySettings
    {
        #region Public-Members

        /// <summary>
        /// Whether request history capture is enabled.  Default is true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Maximum request body bytes to persist.  Bodies larger than this are truncated and marked.  Default 131072 (128 KB).
        /// Minimum: 0. Maximum: 10485760 (10 MB).
        /// </summary>
        public int MaxRequestBodyBytes
        {
            get
            {
                return _MaxRequestBodyBytes;
            }
            set
            {
                if (value < 0 || value > 10485760) throw new ArgumentOutOfRangeException(nameof(MaxRequestBodyBytes));
                _MaxRequestBodyBytes = value;
            }
        }

        /// <summary>
        /// Maximum response body bytes to persist.  Bodies larger than this are truncated and marked.  Default 131072 (128 KB).
        /// Minimum: 0. Maximum: 10485760 (10 MB).
        /// </summary>
        public int MaxResponseBodyBytes
        {
            get
            {
                return _MaxResponseBodyBytes;
            }
            set
            {
                if (value < 0 || value > 10485760) throw new ArgumentOutOfRangeException(nameof(MaxResponseBodyBytes));
                _MaxResponseBodyBytes = value;
            }
        }

        /// <summary>
        /// Number of days to retain request history records before auto-purge.  Default 30.
        /// Minimum: 1. Maximum: 3650.
        /// </summary>
        public int RetentionDays
        {
            get
            {
                return _RetentionDays;
            }
            set
            {
                if (value < 1 || value > 3650) throw new ArgumentOutOfRangeException(nameof(RetentionDays));
                _RetentionDays = value;
            }
        }

        /// <summary>
        /// Interval in minutes between retention purge passes.  Default 60.
        /// Minimum: 1. Maximum: 1440.
        /// </summary>
        public int PurgeIntervalMinutes
        {
            get
            {
                return _PurgeIntervalMinutes;
            }
            set
            {
                if (value < 1 || value > 1440) throw new ArgumentOutOfRangeException(nameof(PurgeIntervalMinutes));
                _PurgeIntervalMinutes = value;
            }
        }

        #endregion

        #region Private-Members

        private int _MaxRequestBodyBytes = 131072;
        private int _MaxResponseBodyBytes = 131072;
        private int _RetentionDays = 30;
        private int _PurgeIntervalMinutes = 60;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySettings()
        {
        }

        #endregion
    }
}

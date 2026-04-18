namespace LiteGraph
{
    using System.Threading;

    /// <summary>
    /// Tracks in-process authorization policy changes for effective-permission cache invalidation.
    /// </summary>
    public static class AuthorizationPolicyChangeTracker
    {
        #region Private-Members

        private static long _Version = 0;

        #endregion

        #region Public-Members

        /// <summary>
        /// Current authorization policy version.
        /// </summary>
        public static long Version
        {
            get
            {
                return Interlocked.Read(ref _Version);
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Signal that authorization roles or assignments changed.
        /// </summary>
        /// <returns>Updated version.</returns>
        public static long SignalChanged()
        {
            return Interlocked.Increment(ref _Version);
        }

        #endregion
    }
}

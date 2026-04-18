namespace LiteGraph.Storage
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Storage migration result.
    /// </summary>
    public class StorageMigrationResult
    {
        /// <summary>
        /// Migration start timestamp.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Migration completion timestamp.
        /// </summary>
        public DateTime? CompletedUtc { get; set; } = null;

        /// <summary>
        /// Migrated entity counts.
        /// </summary>
        public StorageEntityCounts Migrated { get; set; } = new StorageEntityCounts();

        /// <summary>
        /// Verification result.
        /// </summary>
        public StorageVerificationResult Verification { get; set; } = null;

        /// <summary>
        /// Warnings produced during migration.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// True if migration and verification completed successfully.
        /// </summary>
        public bool Succeeded
        {
            get
            {
                return CompletedUtc.HasValue && (Verification == null || Verification.Succeeded);
            }
        }
    }
}

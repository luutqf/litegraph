namespace LiteGraph.Storage
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Storage verification result.
    /// </summary>
    public class StorageVerificationResult
    {
        /// <summary>
        /// Verification timestamp.
        /// </summary>
        public DateTime VerifiedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Source counts.
        /// </summary>
        public StorageEntityCounts SourceCounts { get; set; } = new StorageEntityCounts();

        /// <summary>
        /// Destination counts.
        /// </summary>
        public StorageEntityCounts DestinationCounts { get; set; } = new StorageEntityCounts();

        /// <summary>
        /// Verification differences.
        /// </summary>
        public List<string> Differences { get; set; } = new List<string>();

        /// <summary>
        /// Sampled source record GUIDs verified in the destination.
        /// </summary>
        public List<Guid> SampledGuids { get; set; } = new List<Guid>();

        /// <summary>
        /// True if no verification differences were found.
        /// </summary>
        public bool Succeeded
        {
            get
            {
                return Differences == null || Differences.Count == 0;
            }
        }
    }
}

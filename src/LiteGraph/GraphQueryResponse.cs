namespace LiteGraph
{
    using System.Collections.Generic;

    /// <summary>
    /// Native LiteGraph graph query response envelope.
    /// </summary>
    public class GraphQueryResponse
    {
        /// <summary>
        /// Query result.
        /// </summary>
        public GraphQueryResult Result { get; set; } = null;

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        public double ExecutionTimeMs { get; set; } = 0;

        /// <summary>
        /// Result row count.
        /// </summary>
        public int RowCount { get; set; } = 0;

        /// <summary>
        /// True if the query mutated graph child objects.
        /// </summary>
        public bool Mutated { get; set; } = false;

        /// <summary>
        /// Query plan summary.
        /// </summary>
        public GraphQueryPlanSummary Plan { get; set; } = null;

        /// <summary>
        /// Warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Build a response envelope from a result.
        /// </summary>
        /// <param name="result">Query result.</param>
        /// <returns>Query response.</returns>
        public static GraphQueryResponse FromResult(GraphQueryResult result)
        {
            if (result == null) return new GraphQueryResponse();

            return new GraphQueryResponse
            {
                Result = result,
                ExecutionTimeMs = result.ExecutionTimeMs,
                RowCount = result.RowCount,
                Mutated = result.Mutated,
                Plan = result.Plan,
                Warnings = result.Warnings ?? new List<string>()
            };
        }
    }
}

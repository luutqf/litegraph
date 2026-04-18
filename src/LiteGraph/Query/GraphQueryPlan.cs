namespace LiteGraph.Query
{
    using System;
    using System.Collections.Generic;
    using LiteGraph.Query.Ast;

    /// <summary>
    /// Provider-neutral execution plan for a native graph query.
    /// </summary>
    public class GraphQueryPlan
    {
        /// <summary>
        /// Parsed query AST.
        /// </summary>
        public GraphQueryAst Ast { get; }

        /// <summary>
        /// Query kind.
        /// </summary>
        public GraphQueryKindEnum Kind { get; }

        /// <summary>
        /// True if the query mutates graph child objects.
        /// </summary>
        public bool Mutates { get; }

        /// <summary>
        /// True if the query performs vector search.
        /// </summary>
        public bool UsesVectorSearch { get; }

        /// <summary>
        /// True if the query has an ORDER BY clause.
        /// </summary>
        public bool HasOrder { get; }

        /// <summary>
        /// True if the query has a LIMIT clause.
        /// </summary>
        public bool HasLimit { get; }

        /// <summary>
        /// Estimated relative cost. This is intentionally coarse until the provider-specific planner is implemented.
        /// </summary>
        public int EstimatedCost { get; }

        /// <summary>
        /// Repository seed kind when a predicate can be pushed to a repository read operation.
        /// </summary>
        public GraphQueryPlanSeedKindEnum SeedKind { get; }

        /// <summary>
        /// Variable associated with the seed, when applicable.
        /// </summary>
        public string SeedVariable { get; }

        /// <summary>
        /// Field associated with the seed, when applicable.
        /// </summary>
        public string SeedField { get; }

        /// <summary>
        /// Warnings produced while planning.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ast">Parsed AST.</param>
        /// <param name="mutates">Whether the query mutates graph child objects.</param>
        /// <param name="usesVectorSearch">Whether the query uses vector search.</param>
        /// <param name="hasOrder">Whether the query has ordering.</param>
        /// <param name="hasLimit">Whether the query has a limit.</param>
        /// <param name="estimatedCost">Estimated cost.</param>
        /// <param name="seedKind">Repository seed kind.</param>
        /// <param name="seedVariable">Seed variable.</param>
        /// <param name="seedField">Seed field.</param>
        /// <param name="warnings">Planning warnings.</param>
        public GraphQueryPlan(
            GraphQueryAst ast,
            bool mutates,
            bool usesVectorSearch,
            bool hasOrder,
            bool hasLimit,
            int estimatedCost,
            GraphQueryPlanSeedKindEnum seedKind,
            string seedVariable,
            string seedField,
            IReadOnlyList<string> warnings)
        {
            Ast = ast ?? throw new ArgumentNullException(nameof(ast));
            Kind = ast.Kind;
            Mutates = mutates;
            UsesVectorSearch = usesVectorSearch;
            HasOrder = hasOrder;
            HasLimit = hasLimit;
            EstimatedCost = estimatedCost;
            SeedKind = seedKind;
            SeedVariable = seedVariable;
            SeedField = seedField;
            Warnings = warnings ?? new List<string>();
        }
    }

    /// <summary>
    /// Repository seed selected during planning.
    /// </summary>
    public enum GraphQueryPlanSeedKindEnum
    {
        /// <summary>
        /// No seed.
        /// </summary>
        None,

        /// <summary>
        /// Seed from node GUID equality.
        /// </summary>
        NodeGuid,

        /// <summary>
        /// Seed from node name equality.
        /// </summary>
        NodeName,

        /// <summary>
        /// Seed from edge source GUID equality.
        /// </summary>
        EdgeFromGuid,

        /// <summary>
        /// Seed from edge target GUID equality.
        /// </summary>
        EdgeToGuid,

        /// <summary>
        /// Seed from edge GUID equality.
        /// </summary>
        EdgeGuid,

        /// <summary>
        /// Seed from edge name equality.
        /// </summary>
        EdgeName,

        /// <summary>
        /// Seed from vector index search.
        /// </summary>
        VectorIndex
    }
}

namespace LiteGraph.Indexing.Vector
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// Vector payload and metadata stored in a vector index.
    /// </summary>
    public class VectorIndexEntry
    {
        #region Public-Members

        /// <summary>
        /// Index identifier.
        /// </summary>
        public Guid Id { get; set; } = Guid.Empty;

        /// <summary>
        /// Vector data.
        /// </summary>
        public List<float> Vector { get; set; } = null;

        /// <summary>
        /// Human-readable vector name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Classification labels.
        /// </summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>
        /// Arbitrary key/value metadata.
        /// </summary>
        public Dictionary<string, object> Tags { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public VectorIndexEntry()
        {
        }

        /// <summary>
        /// Build a vector index entry from persisted vector metadata and the graph object to which it belongs.
        /// </summary>
        /// <param name="vector">Vector metadata.</param>
        /// <param name="graph">Graph, if available.</param>
        /// <param name="node">Node, if this is a node vector and the node is available.</param>
        /// <param name="edge">Edge, if this is an edge vector and the edge is available.</param>
        /// <returns>Vector index entry.</returns>
        public static VectorIndexEntry FromVectorMetadata(
            VectorMetadata vector,
            Graph graph = null,
            Node node = null,
            Edge edge = null)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            string domain = GetDomain(vector);
            Dictionary<string, object> tags = BuildTags(vector, graph, node, edge, domain);

            return new VectorIndexEntry
            {
                Id = GetIndexId(vector),
                Vector = vector.Vectors != null ? new List<float>(vector.Vectors) : null,
                Name = GetName(vector, graph, node, edge, domain),
                Labels = GetLabels(graph, node, edge, domain),
                Tags = tags.Count > 0 ? tags : null
            };
        }

        #endregion

        #region Private-Methods

        private static Guid GetIndexId(VectorMetadata vector)
        {
            if (vector.NodeGUID.HasValue) return vector.NodeGUID.Value;
            if (vector.EdgeGUID.HasValue) return vector.EdgeGUID.Value;
            if (vector.GraphGUID != Guid.Empty) return vector.GraphGUID;
            return vector.GUID;
        }

        private static string GetDomain(VectorMetadata vector)
        {
            if (vector.NodeGUID.HasValue) return "Node";
            if (vector.EdgeGUID.HasValue) return "Edge";
            return "Graph";
        }

        private static string GetName(VectorMetadata vector, Graph graph, Node node, Edge edge, string domain)
        {
            string name = null;

            if (String.Equals(domain, "Node", StringComparison.Ordinal))
                name = node?.Name;
            else if (String.Equals(domain, "Edge", StringComparison.Ordinal))
                name = edge?.Name;
            else
                name = graph?.Name;

            if (!String.IsNullOrEmpty(name)) return name;
            if (!String.IsNullOrEmpty(vector.Content)) return vector.Content;
            return null;
        }

        private static List<string> GetLabels(Graph graph, Node node, Edge edge, string domain)
        {
            List<string> labels = null;

            if (String.Equals(domain, "Node", StringComparison.Ordinal))
                labels = node?.Labels;
            else if (String.Equals(domain, "Edge", StringComparison.Ordinal))
                labels = edge?.Labels;
            else
                labels = graph?.Labels;

            return labels != null && labels.Count > 0 ? new List<string>(labels) : null;
        }

        private static Dictionary<string, object> BuildTags(VectorMetadata vector, Graph graph, Node node, Edge edge, string domain)
        {
            Dictionary<string, object> tags = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            NameValueCollection objectTags = null;
            if (String.Equals(domain, "Node", StringComparison.Ordinal))
                objectTags = node?.Tags;
            else if (String.Equals(domain, "Edge", StringComparison.Ordinal))
                objectTags = edge?.Tags;
            else
                objectTags = graph?.Tags;

            AddNameValueCollection(tags, objectTags);

            AddTag(tags, "litegraph.domain", domain);
            AddTag(tags, "litegraph.vector_guid", vector.GUID.ToString("D"));
            AddTag(tags, "litegraph.tenant_guid", vector.TenantGUID.ToString("D"));
            AddTag(tags, "litegraph.graph_guid", vector.GraphGUID.ToString("D"));
            AddTag(tags, "litegraph.model", vector.Model);
            AddTag(tags, "litegraph.dimensionality", vector.Dimensionality);
            AddTag(tags, "litegraph.created_utc", vector.CreatedUtc.ToString("O"));
            AddTag(tags, "litegraph.last_update_utc", vector.LastUpdateUtc.ToString("O"));

            if (graph != null)
                AddTag(tags, "litegraph.graph_name", graph.Name);

            if (vector.NodeGUID.HasValue)
                AddTag(tags, "litegraph.node_guid", vector.NodeGUID.Value.ToString("D"));
            if (node != null)
                AddTag(tags, "litegraph.node_name", node.Name);

            if (vector.EdgeGUID.HasValue)
                AddTag(tags, "litegraph.edge_guid", vector.EdgeGUID.Value.ToString("D"));
            if (edge != null)
            {
                AddTag(tags, "litegraph.edge_name", edge.Name);
                AddTag(tags, "litegraph.edge_from", edge.From.ToString("D"));
                AddTag(tags, "litegraph.edge_to", edge.To.ToString("D"));
            }

            return tags;
        }

        private static void AddNameValueCollection(Dictionary<string, object> tags, NameValueCollection values)
        {
            if (values == null || values.Count < 1) return;

            foreach (string key in values.AllKeys)
            {
                if (String.IsNullOrEmpty(key)) continue;

                string[] allValues = values.GetValues(key);
                if (allValues == null || allValues.Length < 1) continue;

                if (allValues.Length == 1)
                    tags[key] = allValues[0];
                else
                    tags[key] = new List<string>(allValues);
            }
        }

        private static void AddTag(Dictionary<string, object> tags, string key, object value)
        {
            if (String.IsNullOrEmpty(key) || value == null) return;

            if (value is string stringValue && String.IsNullOrEmpty(stringValue)) return;

            tags[key] = value;
        }

        #endregion
    }
}

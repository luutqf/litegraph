namespace LiteGraph.Query
{
    /// <summary>
    /// Native graph query token.
    /// </summary>
    public class GraphQueryToken
    {
        /// <summary>
        /// Token type.
        /// </summary>
        public GraphQueryTokenTypeEnum Type { get; set; }

        /// <summary>
        /// Token text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// One-based source line.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// One-based source column.
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphQueryToken(GraphQueryTokenTypeEnum type, string text, int line, int column)
        {
            Type = type;
            Text = text;
            Line = line;
            Column = column;
        }
    }
}

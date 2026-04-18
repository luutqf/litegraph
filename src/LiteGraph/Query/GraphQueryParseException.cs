namespace LiteGraph.Query
{
    using System;

    /// <summary>
    /// Native graph query parse exception with source location.
    /// </summary>
    public class GraphQueryParseException : ArgumentException
    {
        /// <summary>
        /// One-based source line.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// One-based source column.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="line">Line.</param>
        /// <param name="column">Column.</param>
        public GraphQueryParseException(string message, int line, int column)
            : base(message + " at line " + line + ", column " + column + ".")
        {
            Line = line;
            Column = column;
        }
    }
}

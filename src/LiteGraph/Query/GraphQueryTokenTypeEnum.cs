namespace LiteGraph.Query
{
    /// <summary>
    /// Native graph query token types.
    /// </summary>
    public enum GraphQueryTokenTypeEnum
    {
        /// <summary>
        /// Identifier or keyword.
        /// </summary>
        Identifier,

        /// <summary>
        /// Parameter reference.
        /// </summary>
        Parameter,

        /// <summary>
        /// Quoted string literal.
        /// </summary>
        String,

        /// <summary>
        /// Numeric literal.
        /// </summary>
        Number,

        /// <summary>
        /// Left parenthesis.
        /// </summary>
        LeftParen,

        /// <summary>
        /// Right parenthesis.
        /// </summary>
        RightParen,

        /// <summary>
        /// Left bracket.
        /// </summary>
        LeftBracket,

        /// <summary>
        /// Right bracket.
        /// </summary>
        RightBracket,

        /// <summary>
        /// Left brace.
        /// </summary>
        LeftBrace,

        /// <summary>
        /// Right brace.
        /// </summary>
        RightBrace,

        /// <summary>
        /// Colon.
        /// </summary>
        Colon,

        /// <summary>
        /// Comma.
        /// </summary>
        Comma,

        /// <summary>
        /// Dot.
        /// </summary>
        Dot,

        /// <summary>
        /// Equals sign.
        /// </summary>
        Equals,

        /// <summary>
        /// Greater-than sign.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Greater-than-or-equals sign.
        /// </summary>
        GreaterThanOrEquals,

        /// <summary>
        /// Less-than sign.
        /// </summary>
        LessThan,

        /// <summary>
        /// Less-than-or-equals sign.
        /// </summary>
        LessThanOrEquals,

        /// <summary>
        /// Dash.
        /// </summary>
        Dash,

        /// <summary>
        /// Arrow.
        /// </summary>
        Arrow,

        /// <summary>
        /// Semicolon.
        /// </summary>
        Semicolon,

        /// <summary>
        /// Star.
        /// </summary>
        Star,

        /// <summary>
        /// End of input.
        /// </summary>
        End
    }
}

namespace LiteGraph.Query
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Lexer for the LiteGraph native graph query profile.
    /// </summary>
    public static class Lexer
    {
        /// <summary>
        /// Tokenize query text.
        /// </summary>
        /// <param name="query">Query text.</param>
        /// <returns>Tokens.</returns>
        public static List<GraphQueryToken> Tokenize(string query)
        {
            if (String.IsNullOrWhiteSpace(query)) throw new ArgumentNullException(nameof(query));

            List<GraphQueryToken> tokens = new List<GraphQueryToken>();
            int index = 0;
            int line = 1;
            int column = 1;

            while (index < query.Length)
            {
                char current = query[index];

                if (Char.IsWhiteSpace(current))
                {
                    AdvanceWhitespace(query, ref index, ref line, ref column);
                    continue;
                }

                int tokenLine = line;
                int tokenColumn = column;

                if (IsIdentifierStart(current))
                {
                    string text = ReadWhile(query, ref index, ref column, IsIdentifierPart);
                    tokens.Add(new GraphQueryToken(GraphQueryTokenTypeEnum.Identifier, text, tokenLine, tokenColumn));
                    continue;
                }

                if (current == '$')
                {
                    index++;
                    column++;
                    if (index >= query.Length || !IsIdentifierStart(query[index]))
                        throw new GraphQueryParseException("Parameter name expected", tokenLine, tokenColumn);

                    string name = ReadWhile(query, ref index, ref column, IsIdentifierPart);
                    tokens.Add(new GraphQueryToken(GraphQueryTokenTypeEnum.Parameter, "$" + name, tokenLine, tokenColumn));
                    continue;
                }

                if (current == '\'' || current == '"')
                {
                    tokens.Add(ReadString(query, ref index, ref line, ref column));
                    continue;
                }

                if (Char.IsDigit(current) || (current == '-' && index + 1 < query.Length && Char.IsDigit(query[index + 1])))
                {
                    tokens.Add(ReadNumber(query, ref index, ref column, tokenLine, tokenColumn));
                    continue;
                }

                if (current == '-' && index + 1 < query.Length && query[index + 1] == '>')
                {
                    tokens.Add(new GraphQueryToken(GraphQueryTokenTypeEnum.Arrow, "->", tokenLine, tokenColumn));
                    index += 2;
                    column += 2;
                    continue;
                }

                if (current == '>' && index + 1 < query.Length && query[index + 1] == '=')
                {
                    tokens.Add(new GraphQueryToken(GraphQueryTokenTypeEnum.GreaterThanOrEquals, ">=", tokenLine, tokenColumn));
                    index += 2;
                    column += 2;
                    continue;
                }

                if (current == '<' && index + 1 < query.Length && query[index + 1] == '=')
                {
                    tokens.Add(new GraphQueryToken(GraphQueryTokenTypeEnum.LessThanOrEquals, "<=", tokenLine, tokenColumn));
                    index += 2;
                    column += 2;
                    continue;
                }

                switch (current)
                {
                    case '(':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.LeftParen, current, tokenLine, tokenColumn));
                        break;
                    case ')':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.RightParen, current, tokenLine, tokenColumn));
                        break;
                    case '[':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.LeftBracket, current, tokenLine, tokenColumn));
                        break;
                    case ']':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.RightBracket, current, tokenLine, tokenColumn));
                        break;
                    case '{':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.LeftBrace, current, tokenLine, tokenColumn));
                        break;
                    case '}':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.RightBrace, current, tokenLine, tokenColumn));
                        break;
                    case ':':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.Colon, current, tokenLine, tokenColumn));
                        break;
                    case ',':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.Comma, current, tokenLine, tokenColumn));
                        break;
                    case '.':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.Dot, current, tokenLine, tokenColumn));
                        break;
                    case '=':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.Equals, current, tokenLine, tokenColumn));
                        break;
                    case '>':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.GreaterThan, current, tokenLine, tokenColumn));
                        break;
                    case '<':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.LessThan, current, tokenLine, tokenColumn));
                        break;
                    case '-':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.Dash, current, tokenLine, tokenColumn));
                        break;
                    case ';':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.Semicolon, current, tokenLine, tokenColumn));
                        break;
                    case '*':
                        tokens.Add(Single(GraphQueryTokenTypeEnum.Star, current, tokenLine, tokenColumn));
                        break;
                    default:
                        throw new GraphQueryParseException("Unexpected character '" + current + "'", tokenLine, tokenColumn);
                }

                index++;
                column++;
            }

            tokens.Add(new GraphQueryToken(GraphQueryTokenTypeEnum.End, String.Empty, line, column));
            return tokens;
        }

        private static GraphQueryToken Single(GraphQueryTokenTypeEnum type, char c, int line, int column)
        {
            return new GraphQueryToken(type, c.ToString(), line, column);
        }

        private static void AdvanceWhitespace(string query, ref int index, ref int line, ref int column)
        {
            while (index < query.Length && Char.IsWhiteSpace(query[index]))
            {
                if (query[index] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }

                index++;
            }
        }

        private static string ReadWhile(string query, ref int index, ref int column, Func<char, bool> predicate)
        {
            int start = index;
            while (index < query.Length && predicate(query[index]))
            {
                index++;
                column++;
            }

            return query.Substring(start, index - start);
        }

        private static GraphQueryToken ReadString(string query, ref int index, ref int line, ref int column)
        {
            char quote = query[index];
            int tokenLine = line;
            int tokenColumn = column;
            int start = index;

            index++;
            column++;

            while (index < query.Length)
            {
                char current = query[index];
                if (current == quote && query[index - 1] != '\\')
                {
                    index++;
                    column++;
                    return new GraphQueryToken(GraphQueryTokenTypeEnum.String, query.Substring(start, index - start), tokenLine, tokenColumn);
                }

                if (current == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }

                index++;
            }

            throw new GraphQueryParseException("Unterminated string literal", tokenLine, tokenColumn);
        }

        private static GraphQueryToken ReadNumber(string query, ref int index, ref int column, int line, int tokenColumn)
        {
            int start = index;
            if (query[index] == '-')
            {
                index++;
                column++;
            }

            while (index < query.Length && Char.IsDigit(query[index]))
            {
                index++;
                column++;
            }

            if (index + 1 < query.Length && query[index] == '.' && Char.IsDigit(query[index + 1]))
            {
                index++;
                column++;
                while (index < query.Length && Char.IsDigit(query[index]))
                {
                    index++;
                    column++;
                }
            }

            return new GraphQueryToken(GraphQueryTokenTypeEnum.Number, query.Substring(start, index - start), line, tokenColumn);
        }

        private static bool IsIdentifierStart(char c)
        {
            return Char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierPart(char c)
        {
            return Char.IsLetterOrDigit(c) || c == '_' || c == '-';
        }
    }
}

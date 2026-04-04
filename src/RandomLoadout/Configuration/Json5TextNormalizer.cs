using System.Text;

namespace RandomLoadout
{
    internal static class Json5TextNormalizer
    {
        public static string Normalize(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return string.Empty;
            }

            return RemoveTrailingCommas(RemoveComments(rawText));
        }

        private static string RemoveComments(string text)
        {
            StringBuilder builder = new StringBuilder(text.Length);
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escapeNext = false;
            bool inLineComment = false;
            bool inBlockComment = false;

            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                char next = i + 1 < text.Length ? text[i + 1] : '\0';

                if (inLineComment)
                {
                    if (current == '\r' || current == '\n')
                    {
                        inLineComment = false;
                        builder.Append(current);
                    }

                    continue;
                }

                if (inBlockComment)
                {
                    if (current == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }

                    continue;
                }

                if (!inSingleQuote && !inDoubleQuote && current == '/' && next == '/')
                {
                    inLineComment = true;
                    i++;
                    continue;
                }

                if (!inSingleQuote && !inDoubleQuote && current == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (!escapeNext)
                {
                    if (current == '\'' && !inDoubleQuote)
                    {
                        inSingleQuote = !inSingleQuote;
                    }
                    else if (current == '"' && !inSingleQuote)
                    {
                        inDoubleQuote = !inDoubleQuote;
                    }
                }

                builder.Append(current);

                if ((inSingleQuote || inDoubleQuote) && current == '\\' && !escapeNext)
                {
                    escapeNext = true;
                }
                else
                {
                    escapeNext = false;
                }
            }

            return builder.ToString();
        }

        private static string RemoveTrailingCommas(string text)
        {
            StringBuilder builder = new StringBuilder(text.Length);
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escapeNext = false;

            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                builder.Append(current);

                if (!escapeNext)
                {
                    if (current == '\'' && !inDoubleQuote)
                    {
                        inSingleQuote = !inSingleQuote;
                    }
                    else if (current == '"' && !inSingleQuote)
                    {
                        inDoubleQuote = !inDoubleQuote;
                    }
                }

                if ((inSingleQuote || inDoubleQuote) && current == '\\' && !escapeNext)
                {
                    escapeNext = true;
                    continue;
                }

                escapeNext = false;

                if (inSingleQuote || inDoubleQuote || current != ',')
                {
                    continue;
                }

                int lookahead = i + 1;
                while (lookahead < text.Length && char.IsWhiteSpace(text[lookahead]))
                {
                    lookahead++;
                }

                if (lookahead < text.Length && (text[lookahead] == ']' || text[lookahead] == '}'))
                {
                    builder.Length--;
                }
            }

            return builder.ToString();
        }
    }
}

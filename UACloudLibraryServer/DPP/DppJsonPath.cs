using System;
using System.Collections.Generic;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Parses a JSONPath expression into an ordered list of segments.
    /// </summary>
    /// <remarks>
    /// Per EN 18222, the value of <c>elementIdPath</c> shall follow
    /// RFC 9535 JSONPath. This parser implements the subset of RFC 9535 actually expressed
    /// by the DPP data model:
    /// <list type="bullet">
    ///   <item>Optional root identifier <c>$</c> and <c>$.</c>.</item>
    ///   <item>Dot child selector: <c>.name</c>.</item>
    ///   <item>Bracket name selector with single or double quotes: <c>['name']</c> / <c>["name"]</c>.</item>
    ///   <item>Bracket index selector: <c>[123]</c>.</item>
    /// </list>
    /// Filter expressions, wildcards (<c>*</c>), slice selectors, and the descendant operator (<c>..</c>)
    /// are intentionally rejected; callers must surface this as a <c>ClientErrorBadRequest</c>.
    /// </remarks>
    public static class DppJsonPath
    {
        /// <summary>
        /// A single segment of a parsed JSONPath expression. Exactly one of
        /// <see cref="Name"/> or <see cref="Index"/> is populated.
        /// </summary>
        public readonly struct Segment
        {
            public string Name { get; }
            public int? Index { get; }

            private Segment(string name, int? index)
            {
                Name = name;
                Index = index;
            }

            public static Segment ForName(string name) => new(name, null);
            public static Segment ForIndex(int index) => new(null, index);

            public bool IsName => Name is not null;
            public bool IsIndex => Index.HasValue;

            public override string ToString() => IsName ? $".{Name}" : $"[{Index}]";
        }

        /// <summary>
        /// Parses the supplied path. Returns <c>false</c> with an <paramref name="error"/>
        /// message when the path is malformed or uses an unsupported construct.
        /// </summary>
        public static bool TryParse(string path, out IReadOnlyList<Segment> segments, out string error)
        {
            segments = null;
            error = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "elementIdPath must not be empty.";
                return false;
            }

            ReadOnlySpan<char> span = path.AsSpan();
            int i = 0;

            // Optional RFC 9535 root identifier. The supported subset only allows the bare
            // '$' to be followed by a dot child selector ('.'), a bracket selector ('['), or
            // the end of the expression. Without this guard the parser would silently treat
            // inputs like "$elements" as an unprefixed name segment, accepting invalid
            // JSONPath and misleading clients.
            if (span[i] == '$')
            {
                i++;
                if (i < span.Length && span[i] != '.' && span[i] != '[')
                {
                    error = $"Unexpected character '{span[i]}' after root identifier '$' in elementIdPath.";
                    return false;
                }
            }

            var result = new List<Segment>();

            while (i < span.Length)
            {
                char c = span[i];

                if (c == '.')
                {
                    // Reject the descendant operator (..) explicitly.
                    if (i + 1 < span.Length && span[i + 1] == '.')
                    {
                        error = "Descendant operator '..' is not supported.";
                        return false;
                    }

                    i++; // consume '.'
                    int start = i;
                    while (i < span.Length && span[i] != '.' && span[i] != '[')
                    {
                        char nameChar = span[i];
                        if (nameChar == '*')
                        {
                            error = "Wildcard '*' is not supported.";
                            return false;
                        }

                        i++;
                    }

                    if (i == start)
                    {
                        error = "Empty name segment after '.'.";
                        return false;
                    }

                    result.Add(Segment.ForName(span.Slice(start, i - start).ToString()));
                    continue;
                }

                if (c == '[')
                {
                    int closing = path.IndexOf(']', i + 1);
                    if (closing < 0)
                    {
                        error = "Unbalanced '[' in elementIdPath.";
                        return false;
                    }

                    ReadOnlySpan<char> body = span.Slice(i + 1, closing - i - 1).Trim();
                    if (body.Length == 0)
                    {
                        error = "Empty selector inside brackets.";
                        return false;
                    }

                    if (body[0] == '\'' || body[0] == '"')
                    {
                        char quote = body[0];
                        if (body.Length < 2 || body[body.Length - 1] != quote)
                        {
                            error = "Unterminated quoted name inside brackets.";
                            return false;
                        }

                        string name = body.Slice(1, body.Length - 2).ToString();
                        if (name.Length == 0)
                        {
                            error = "Empty quoted name inside brackets.";
                            return false;
                        }

                        result.Add(Segment.ForName(name));
                    }
                    else if (body[0] == '?' || body[0] == '*' || body.IndexOf(':') >= 0)
                    {
                        error = "Filters, wildcards and slice selectors are not supported.";
                        return false;
                    }
                    else
                    {
                        if (!int.TryParse(body, out int index) || index < 0)
                        {
                            error = $"Invalid index selector '[{body.ToString()}]'.";
                            return false;
                        }

                        result.Add(Segment.ForIndex(index));
                    }

                    i = closing + 1;
                    continue;
                }

                // First (unprefixed) name segment, e.g. "elements.0".
                if (result.Count == 0)
                {
                    int start = i;
                    while (i < span.Length && span[i] != '.' && span[i] != '[')
                    {
                        // Reject the wildcard selector here too: without this guard the
                        // unprefixed leading segment would accept "*" or "foo*bar" as a
                        // literal name, contradicting the documented RFC 9535 subset.
                        if (span[i] == '*')
                        {
                            error = "Wildcard '*' is not supported.";
                            return false;
                        }

                        i++;
                    }

                    result.Add(Segment.ForName(span.Slice(start, i - start).ToString()));
                    continue;
                }

                error = $"Unexpected character '{c}' in elementIdPath at position {i}.";
                return false;
            }

            if (result.Count == 0)
            {
                error = "elementIdPath did not yield any segments.";
                return false;
            }

            segments = result;
            return true;
        }
    }
}

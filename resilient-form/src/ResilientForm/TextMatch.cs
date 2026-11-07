using System.Text;

namespace ResilientForm;

public static class TextMatch
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                pendingSpace = false;
                continue;
            }

            if (!pendingSpace && builder.Length > 0)
            {
                builder.Append(' ');
                pendingSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    public static int Score(string wanted, string observed)
    {
        var left = Normalize(wanted);
        var right = Normalize(observed);
        if (left.Length == 0 || right.Length == 0)
        {
            return 0;
        }

        if (left == right)
        {
            return 100;
        }

        var wantedTokens = Tokens(left);
        var observedTokens = Tokens(right);
        if (wantedTokens.Count == 0 || observedTokens.Count == 0)
        {
            return 0;
        }

        var at = IndexOfContiguous(observedTokens, wantedTokens);
        if (at >= 0)
        {
            var extra = observedTokens.Count - wantedTokens.Count;
            return Math.Max(0, 100 - (extra * 15));
        }

        var shared = wantedTokens.Intersect(observedTokens).Count();
        return shared * 15;
    }

    public static int Best(IEnumerable<string>? texts, string wanted, int cap)
    {
        var best = 0;
        if (texts == null)
        {
            return 0;
        }

        foreach (var text in texts)
        {
            var score = Math.Min(cap, Score(wanted, text));
            if (score > best)
            {
                best = score;
            }
        }

        return best;
    }

    private static List<string> Tokens(string normalized)
    {
        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static int IndexOfContiguous(List<string> haystack, List<string> needle)
    {
        if (needle.Count > haystack.Count)
        {
            return -1;
        }

        for (var start = 0; start <= haystack.Count - needle.Count; start++)
        {
            var matches = true;
            for (var offset = 0; offset < needle.Count; offset++)
            {
                if (haystack[start + offset] != needle[offset])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return start;
            }
        }

        return -1;
    }
}

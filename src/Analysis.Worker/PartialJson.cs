using System.Text.Json.Nodes;
using System.Text.Json;

public static class PartialJson
{
    public static bool TryParse(string text, out JsonNode? node) => TryParse(text, out node, fallback: true);

    private static bool TryParse(string text, out JsonNode? node, bool fallback)
    {
        var stack = new Stack<char>();
        bool inString = false, escape = false;
        var lastComma = -1;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (escape) escape = false;
                else if (c == '\\') escape = true;
                else if (c == '"') inString = false;
                continue;
            }
            switch (c)
            {
                case '"': inString = true; break;
                case '{': stack.Push('}'); break;
                case '[': stack.Push(']'); break;
                case '}' or ']': if (stack.Count > 0) stack.Pop(); break;
                case ',': lastComma = i; break;
            }
        }

        var s = text;
        if (inString) s = (escape ? s[..^1] : s) + "\"";
        s = s.TrimEnd();
        if (s.EndsWith(',')) s = s[..^1];
        else if (s.EndsWith(':')) s += "null";
        s += string.Concat(stack);              // Stack enumerates top-first = correct closing order

        try { node = JsonNode.Parse(s); return true; }
        catch (JsonException)
        {
            // Usually a half-written key ({"a":1,"summ). Drop it by cutting at the last comma.
            if (fallback && lastComma > 0) return TryParse(text[..lastComma], out node, fallback: false);
            node = null;
            return false;
        }
    }
}
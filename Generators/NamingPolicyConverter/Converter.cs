namespace Generators.NamingPolicyConverter;

public static class Converter
{
    extension(string s)
    {
        public string Convert(NamingPolicy policy) => Convert(s.AsSpan(), policy);
    }

    static string Convert(ReadOnlySpan<char> source, NamingPolicy policy) => policy switch
    {
        NamingPolicy.CamelCase => ConvertCamel(source, false),
        NamingPolicy.PascalCase => ConvertCamel(source, true),
        NamingPolicy.SnakeCaseLower => Convert(source, false, '_'),
        NamingPolicy.SnakeCaseUpper => Convert(source, true, '_'),
        NamingPolicy.KebabCaseLower => Convert(source, false, '-'),
        NamingPolicy.KebabCaseUpper => Convert(source, true, '-'),
        _ => source.ToString(),
    };

    static string ConvertCamel(ReadOnlySpan<char> source, bool upper)
    {
        var buffer = source.Length <= 512 ? stackalloc char[source.Length] : new char[source.Length];

        var written = 0;
        var s = buffer;

        foreach (var word in new WordEnumerable(source))
        {
            if (word.Length == 0) continue;

            var c = word[0];
            s[0] = (written != 0 || upper) ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
            word[1..].ToLowerInvariant(s[1..]);
            s = s[word.Length..];
            written += word.Length;
        }

        return buffer[..written].ToString();
    }

    static string Convert(ReadOnlySpan<char> source, bool upper, char separator)
    {
        // estimated to source.Length ×1.25.
        var estimatedLength = source.Length + ((source.Length + 3) >> 2);
        var buffer = estimatedLength <= 512 ? stackalloc char[estimatedLength] : new char[estimatedLength];

        static void EnsureCapacity(ref Span<char> buffer, int required)
        {
            if (buffer.Length >= required) return;
            var newBuffer = new char[buffer.Length * 2];
            buffer.CopyTo(newBuffer);
            buffer = newBuffer;
        }

        var written = 0;
        var first = true;

        foreach (var word in new WordEnumerable(source))
        {
            if (first) first = false;
            else
            {
                EnsureCapacity(ref buffer, written + 1);
                buffer[written] = separator;
                written++;
            }

            EnsureCapacity(ref buffer, written + word.Length);
            if (upper) word.ToUpperInvariant(buffer[written..]);
            else word.ToLowerInvariant(buffer[written..]);
            written += word.Length;
        }

        return buffer[..written].ToString();
    }
}

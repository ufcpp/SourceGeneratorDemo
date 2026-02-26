using System.Globalization;

namespace Generators.NamingPolicyConverter;

readonly ref struct WordEnumerable(ReadOnlySpan<char> s)
{
    private readonly ReadOnlySpan<char> _s = s;

    public WordEnumerator GetEnumerator() => new(_s);
}

ref struct WordEnumerator(ReadOnlySpan<char> s)
{
    private readonly ReadOnlySpan<char> _s = s;
    private UnicodeCategory _prevCat = UnicodeCategory.OtherNotAssigned;
    private int _i = 0;
    private int _start = 0;
    private int _end = 0;

    public bool MoveNext()
    {
        var i = _i;
        var s = _s;

        if (i >= s.Length) return false;

        var separator = false;
        _start = i;
        while (true)
        {
            var c = s[i];
            var cat = char.GetUnicodeCategory(c);
            var prev = _prevCat;

            if (!IsMark(cat)) _prevCat = cat;

            if (prev == UnicodeCategory.LowercaseLetter && cat == UnicodeCategory.UppercaseLetter) break;
            if (IsNumber(prev) && IsLetter(cat)) break;
            if (IsCaseLetter(prev) && IsNonCaseLetter(cat)) break;
            if (IsNonCaseLetter(prev) && IsCaseLetter(cat)) break;
            if (IsSeparator(cat)) { separator = true; break; }
            if (++i >= s.Length) break;
        }

        _i = separator ? i + 1 : i;
        _end = i;

        return true;
    }

    private static bool IsMark(UnicodeCategory cat)
        => cat == UnicodeCategory.NonSpacingMark
        || cat == UnicodeCategory.NonSpacingMark
        || cat == UnicodeCategory.EnclosingMark;

    private static bool IsNumber(UnicodeCategory cat)
        => cat == UnicodeCategory.DecimalDigitNumber
        || cat == UnicodeCategory.OtherNumber
        || cat == UnicodeCategory.LetterNumber;

    private static bool IsLetter(UnicodeCategory cat)
        => cat == UnicodeCategory.LowercaseLetter
        || cat == UnicodeCategory.UppercaseLetter
        || cat == UnicodeCategory.OtherLetter
        || cat == UnicodeCategory.ModifierLetter;

    private static bool IsCaseLetter(UnicodeCategory cat)
        => cat == UnicodeCategory.LowercaseLetter
        || cat == UnicodeCategory.UppercaseLetter;

    private static bool IsNonCaseLetter(UnicodeCategory cat)
        => cat == UnicodeCategory.OtherLetter
        || cat == UnicodeCategory.ModifierLetter;

    private static bool IsSeparator(UnicodeCategory cat)
        => cat == UnicodeCategory.SpaceSeparator
        || cat == UnicodeCategory.ConnectorPunctuation
        || cat == UnicodeCategory.DashPunctuation
        || cat == UnicodeCategory.OtherPunctuation;

    public readonly ReadOnlySpan<char> Current => _s[_start.._end];
}

using System.Runtime.InteropServices;

namespace Generators.AttributeTemplates;

/// <summary>
/// Represents a C# literal value (int, long, float, double, decimal, char, string, bool, or null) and its explicit cast (byte, sbyte, short, ushort).
/// Uses explicit layout to store numeric values in the same memory location to minimize allocation.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal readonly struct Variant : IFormattable
{
    // Reference type stored separately
    [FieldOffset(0)] private readonly string? _string;

    // Numeric values share the same memory location (union-like)
    [FieldOffset(8)] private readonly bool _bool;
    [FieldOffset(8)] private readonly char _char;
    [FieldOffset(8)] private readonly sbyte _sbyte;
    [FieldOffset(8)] private readonly byte _byte;
    [FieldOffset(8)] private readonly short _short;
    [FieldOffset(8)] private readonly ushort _ushort;
    [FieldOffset(8)] private readonly int _int32;
    [FieldOffset(8)] private readonly uint _uint32;
    [FieldOffset(8)] private readonly long _int64;
    [FieldOffset(8)] private readonly ulong _uint64;
    [FieldOffset(8)] private readonly float _single;
    [FieldOffset(8)] private readonly double _double;
    [FieldOffset(8)] private readonly decimal _decimal;

    // sizeof(decimal) is 16 bytes, so we can store the kind after it.
    [FieldOffset(8 + 16)] private readonly LiteralKind _kind;

    public LiteralKind Kind => _kind;

    private Variant(LiteralKind kind) => _kind = kind;

    public Variant(bool value) : this(LiteralKind.Boolean) => _bool = value;
    public Variant(char value) : this(LiteralKind.Char) => _char = value;
    public Variant(sbyte value) : this(LiteralKind.SByte) => _sbyte = value;
    public Variant(byte value) : this(LiteralKind.Byte) => _byte = value;
    public Variant(short value) : this(LiteralKind.Int16) => _short = value;
    public Variant(ushort value) : this(LiteralKind.UInt16) => _ushort = value;
    public Variant(int value) : this(LiteralKind.Int32) => _int32 = value;
    public Variant(uint value) : this(LiteralKind.UInt32) => _uint32 = value;
    public Variant(long value) : this(LiteralKind.Int64) => _int64 = value;
    public Variant(ulong value) : this(LiteralKind.UInt64) => _uint64 = value;
    public Variant(float value) : this(LiteralKind.Single) => _single = value;
    public Variant(double value) : this(LiteralKind.Double) => _double = value;
    public Variant(decimal value) : this(LiteralKind.Decimal) => _decimal = value;
    public Variant(string value) : this(LiteralKind.String) => _string = value;

    public static Variant? TryFromObject(object? value) => value switch
    {
        null => default,
        bool b => new(b),
        char c => new(c),
        sbyte sb => new(sb),
        byte b => new(b),
        short s => new(s),
        ushort us => new(us),
        int i => new(i),
        uint ui => new(ui),
        long l => new(l),
        ulong ul => new(ul),
        float f => new(f),
        double d => new(d),
        decimal dec => new(dec),
        string s => new(s),
        _ => null
    };

    public static Variant FromObject(object? value)
        => TryFromObject(value)
        ?? throw new ArgumentException($"Unsupported literal type: {value?.GetType()}", nameof(value));

    public object? ToObject() => _kind switch
    {
        LiteralKind.Null => null,
        LiteralKind.Boolean => _bool,
        LiteralKind.Char => _char,
        LiteralKind.SByte => _sbyte,
        LiteralKind.Byte => _byte,
        LiteralKind.Int16 => _short,
        LiteralKind.UInt16 => _ushort,
        LiteralKind.Int32 => _int32,
        LiteralKind.UInt32 => _uint32,
        LiteralKind.Int64 => _int64,
        LiteralKind.UInt64 => _uint64,
        LiteralKind.Single => _single,
        LiteralKind.Double => _double,
        LiteralKind.Decimal => _decimal,
        LiteralKind.String => _string,
        _ => throw new InvalidOperationException($"Unknown literal kind: {_kind}")
    };

    public string ToString(string? format, IFormatProvider? formatProvider) => _kind switch
    {
        LiteralKind.Null => "",
        LiteralKind.Boolean => _bool.ToString(formatProvider),
        LiteralKind.Char => _char.ToString(formatProvider),
        LiteralKind.SByte => _sbyte.ToString(format, formatProvider),
        LiteralKind.Byte => _byte.ToString(format, formatProvider)!,
        LiteralKind.Int16 => _short.ToString(format, formatProvider),
        LiteralKind.UInt16 => _ushort.ToString(format, formatProvider),
        LiteralKind.Int32 => _int32.ToString(format, formatProvider),
        LiteralKind.UInt32 => _uint32.ToString(format, formatProvider),
        LiteralKind.Int64 => _int64.ToString(format, formatProvider),
        LiteralKind.UInt64 => _uint64.ToString(format, formatProvider),
        LiteralKind.Single => _single.ToString(format, formatProvider),
        LiteralKind.Double => _double.ToString(format, formatProvider),
        LiteralKind.Decimal => _decimal.ToString(format, formatProvider),
        LiteralKind.String => _string ?? "",
        _ => "",
    };

    public override string ToString() => ToString(null, null);
}

internal enum LiteralKind : byte
{
    Null = 0,
    Boolean,
    Char,
    SByte,
    Byte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Single,
    Double,
    Decimal,
    String,
}

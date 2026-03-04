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
    [FieldOffset(0)] internal readonly string? _string;
    [FieldOffset(0)] internal readonly Variant[]? _array;

    // Numeric values share the same memory location (union-like)
    [FieldOffset(8)] internal readonly bool _bool;
    [FieldOffset(8)] internal readonly char _char;
    [FieldOffset(8)] internal readonly sbyte _sbyte;
    [FieldOffset(8)] internal readonly byte _byte;
    [FieldOffset(8)] internal readonly short _short;
    [FieldOffset(8)] internal readonly ushort _ushort;
    [FieldOffset(8)] internal readonly int _int32;
    [FieldOffset(8)] internal readonly uint _uint32;
    [FieldOffset(8)] internal readonly long _int64;
    [FieldOffset(8)] internal readonly ulong _uint64;
    [FieldOffset(8)] internal readonly float _single;
    [FieldOffset(8)] internal readonly double _double;
    [FieldOffset(8)] internal readonly decimal _decimal;

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
    public Variant(Variant[] value) : this(LiteralKind.Array) => _array = value;

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
        object[] arr => new([.. arr.Select(FromObject)]),
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
        LiteralKind.Array => _array?.Select(v => v.ToObject()).ToArray(),
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
        LiteralKind.Array => string.Join(@"
", _array?.Select(v => v.ToString(format, formatProvider)) ?? []),
        _ => "",
    };

    public override string ToString() => ToString(null, null);

    /// <summary>
    /// Cast this variant to the specified target type.
    /// Supports numeric conversions between primitive types.
    /// </summary>
    public Variant Cast(LiteralKind targetKind) => (targetKind, _kind) switch
    {
        // No conversion needed
        (var target, var source) when target == source => this,

        // To SByte
        (LiteralKind.SByte, LiteralKind.Char) => new((sbyte)_char),
        (LiteralKind.SByte, LiteralKind.Byte) => new((sbyte)_byte),
        (LiteralKind.SByte, LiteralKind.Int16) => new((sbyte)_short),
        (LiteralKind.SByte, LiteralKind.UInt16) => new((sbyte)_ushort),
        (LiteralKind.SByte, LiteralKind.Int32) => new((sbyte)_int32),
        (LiteralKind.SByte, LiteralKind.UInt32) => new((sbyte)_uint32),
        (LiteralKind.SByte, LiteralKind.Int64) => new((sbyte)_int64),
        (LiteralKind.SByte, LiteralKind.UInt64) => new((sbyte)_uint64),
        (LiteralKind.SByte, LiteralKind.Single) => new((sbyte)_single),
        (LiteralKind.SByte, LiteralKind.Double) => new((sbyte)_double),
        (LiteralKind.SByte, LiteralKind.Decimal) => new((sbyte)_decimal),

        // To Byte
        (LiteralKind.Byte, LiteralKind.Char) => new((byte)_char),
        (LiteralKind.Byte, LiteralKind.SByte) => new((byte)_sbyte),
        (LiteralKind.Byte, LiteralKind.Int16) => new((byte)_short),
        (LiteralKind.Byte, LiteralKind.UInt16) => new((byte)_ushort),
        (LiteralKind.Byte, LiteralKind.Int32) => new((byte)_int32),
        (LiteralKind.Byte, LiteralKind.UInt32) => new((byte)_uint32),
        (LiteralKind.Byte, LiteralKind.Int64) => new((byte)_int64),
        (LiteralKind.Byte, LiteralKind.UInt64) => new((byte)_uint64),
        (LiteralKind.Byte, LiteralKind.Single) => new((byte)_single),
        (LiteralKind.Byte, LiteralKind.Double) => new((byte)_double),
        (LiteralKind.Byte, LiteralKind.Decimal) => new((byte)_decimal),

        // To Int16
        (LiteralKind.Int16, LiteralKind.Char) => new((short)_char),
        (LiteralKind.Int16, LiteralKind.SByte) => new((short)_sbyte),
        (LiteralKind.Int16, LiteralKind.Byte) => new((short)_byte),
        (LiteralKind.Int16, LiteralKind.UInt16) => new((short)_ushort),
        (LiteralKind.Int16, LiteralKind.Int32) => new((short)_int32),
        (LiteralKind.Int16, LiteralKind.UInt32) => new((short)_uint32),
        (LiteralKind.Int16, LiteralKind.Int64) => new((short)_int64),
        (LiteralKind.Int16, LiteralKind.UInt64) => new((short)_uint64),
        (LiteralKind.Int16, LiteralKind.Single) => new((short)_single),
        (LiteralKind.Int16, LiteralKind.Double) => new((short)_double),
        (LiteralKind.Int16, LiteralKind.Decimal) => new((short)_decimal),

        // To UInt16
        (LiteralKind.UInt16, LiteralKind.Char) => new((ushort)_char),
        (LiteralKind.UInt16, LiteralKind.SByte) => new((ushort)_sbyte),
        (LiteralKind.UInt16, LiteralKind.Byte) => new((ushort)_byte),
        (LiteralKind.UInt16, LiteralKind.Int16) => new((ushort)_short),
        (LiteralKind.UInt16, LiteralKind.Int32) => new((ushort)_int32),
        (LiteralKind.UInt16, LiteralKind.UInt32) => new((ushort)_uint32),
        (LiteralKind.UInt16, LiteralKind.Int64) => new((ushort)_int64),
        (LiteralKind.UInt16, LiteralKind.UInt64) => new((ushort)_uint64),
        (LiteralKind.UInt16, LiteralKind.Single) => new((ushort)_single),
        (LiteralKind.UInt16, LiteralKind.Double) => new((ushort)_double),
        (LiteralKind.UInt16, LiteralKind.Decimal) => new((ushort)_decimal),

        // To Int32
        (LiteralKind.Int32, LiteralKind.Char) => new((int)_char),
        (LiteralKind.Int32, LiteralKind.SByte) => new((int)_sbyte),
        (LiteralKind.Int32, LiteralKind.Byte) => new((int)_byte),
        (LiteralKind.Int32, LiteralKind.Int16) => new((int)_short),
        (LiteralKind.Int32, LiteralKind.UInt16) => new((int)_ushort),
        (LiteralKind.Int32, LiteralKind.UInt32) => new((int)_uint32),
        (LiteralKind.Int32, LiteralKind.Int64) => new((int)_int64),
        (LiteralKind.Int32, LiteralKind.UInt64) => new((int)_uint64),
        (LiteralKind.Int32, LiteralKind.Single) => new((int)_single),
        (LiteralKind.Int32, LiteralKind.Double) => new((int)_double),
        (LiteralKind.Int32, LiteralKind.Decimal) => new((int)_decimal),

        // To UInt32
        (LiteralKind.UInt32, LiteralKind.Char) => new((uint)_char),
        (LiteralKind.UInt32, LiteralKind.SByte) => new((uint)_sbyte),
        (LiteralKind.UInt32, LiteralKind.Byte) => new((uint)_byte),
        (LiteralKind.UInt32, LiteralKind.Int16) => new((uint)_short),
        (LiteralKind.UInt32, LiteralKind.UInt16) => new((uint)_ushort),
        (LiteralKind.UInt32, LiteralKind.Int32) => new((uint)_int32),
        (LiteralKind.UInt32, LiteralKind.Int64) => new((uint)_int64),
        (LiteralKind.UInt32, LiteralKind.UInt64) => new((uint)_uint64),
        (LiteralKind.UInt32, LiteralKind.Single) => new((uint)_single),
        (LiteralKind.UInt32, LiteralKind.Double) => new((uint)_double),
        (LiteralKind.UInt32, LiteralKind.Decimal) => new((uint)_decimal),

        // To Int64
        (LiteralKind.Int64, LiteralKind.Char) => new((long)_char),
        (LiteralKind.Int64, LiteralKind.SByte) => new((long)_sbyte),
        (LiteralKind.Int64, LiteralKind.Byte) => new((long)_byte),
        (LiteralKind.Int64, LiteralKind.Int16) => new((long)_short),
        (LiteralKind.Int64, LiteralKind.UInt16) => new((long)_ushort),
        (LiteralKind.Int64, LiteralKind.Int32) => new((long)_int32),
        (LiteralKind.Int64, LiteralKind.UInt32) => new((long)_uint32),
        (LiteralKind.Int64, LiteralKind.UInt64) => new((long)_uint64),
        (LiteralKind.Int64, LiteralKind.Single) => new((long)_single),
        (LiteralKind.Int64, LiteralKind.Double) => new((long)_double),
        (LiteralKind.Int64, LiteralKind.Decimal) => new((long)_decimal),

        // To UInt64
        (LiteralKind.UInt64, LiteralKind.Char) => new((ulong)_char),
        (LiteralKind.UInt64, LiteralKind.SByte) => new((ulong)_sbyte),
        (LiteralKind.UInt64, LiteralKind.Byte) => new((ulong)_byte),
        (LiteralKind.UInt64, LiteralKind.Int16) => new((ulong)_short),
        (LiteralKind.UInt64, LiteralKind.UInt16) => new((ulong)_ushort),
        (LiteralKind.UInt64, LiteralKind.Int32) => new((ulong)_int32),
        (LiteralKind.UInt64, LiteralKind.UInt32) => new((ulong)_uint32),
        (LiteralKind.UInt64, LiteralKind.Int64) => new((ulong)_int64),
        (LiteralKind.UInt64, LiteralKind.Single) => new((ulong)_single),
        (LiteralKind.UInt64, LiteralKind.Double) => new((ulong)_double),
        (LiteralKind.UInt64, LiteralKind.Decimal) => new((ulong)_decimal),

        // To Single
        (LiteralKind.Single, LiteralKind.Char) => new((float)_char),
        (LiteralKind.Single, LiteralKind.SByte) => new((float)_sbyte),
        (LiteralKind.Single, LiteralKind.Byte) => new((float)_byte),
        (LiteralKind.Single, LiteralKind.Int16) => new((float)_short),
        (LiteralKind.Single, LiteralKind.UInt16) => new((float)_ushort),
        (LiteralKind.Single, LiteralKind.Int32) => new((float)_int32),
        (LiteralKind.Single, LiteralKind.UInt32) => new((float)_uint32),
        (LiteralKind.Single, LiteralKind.Int64) => new((float)_int64),
        (LiteralKind.Single, LiteralKind.UInt64) => new((float)_uint64),
        (LiteralKind.Single, LiteralKind.Double) => new((float)_double),
        (LiteralKind.Single, LiteralKind.Decimal) => new((float)_decimal),

        // To Double
        (LiteralKind.Double, LiteralKind.Char) => new((double)_char),
        (LiteralKind.Double, LiteralKind.SByte) => new((double)_sbyte),
        (LiteralKind.Double, LiteralKind.Byte) => new((double)_byte),
        (LiteralKind.Double, LiteralKind.Int16) => new((double)_short),
        (LiteralKind.Double, LiteralKind.UInt16) => new((double)_ushort),
        (LiteralKind.Double, LiteralKind.Int32) => new((double)_int32),
        (LiteralKind.Double, LiteralKind.UInt32) => new((double)_uint32),
        (LiteralKind.Double, LiteralKind.Int64) => new((double)_int64),
        (LiteralKind.Double, LiteralKind.UInt64) => new((double)_uint64),
        (LiteralKind.Double, LiteralKind.Single) => new((double)_single),

        // To Decimal
        (LiteralKind.Decimal, LiteralKind.Char) => new((decimal)_char),
        (LiteralKind.Decimal, LiteralKind.SByte) => new((decimal)_sbyte),
        (LiteralKind.Decimal, LiteralKind.Byte) => new((decimal)_byte),
        (LiteralKind.Decimal, LiteralKind.Int16) => new((decimal)_short),
        (LiteralKind.Decimal, LiteralKind.UInt16) => new((decimal)_ushort),
        (LiteralKind.Decimal, LiteralKind.Int32) => new((decimal)_int32),
        (LiteralKind.Decimal, LiteralKind.UInt32) => new((decimal)_uint32),
        (LiteralKind.Decimal, LiteralKind.Int64) => new((decimal)_int64),
        (LiteralKind.Decimal, LiteralKind.UInt64) => new((decimal)_uint64),
        (LiteralKind.Decimal, LiteralKind.Single) => new((decimal)_single),
        (LiteralKind.Decimal, LiteralKind.Double) => new((decimal)_double),

        // To Char
        (LiteralKind.Char, LiteralKind.SByte) => new((char)_sbyte),
        (LiteralKind.Char, LiteralKind.Byte) => new((char)_byte),
        (LiteralKind.Char, LiteralKind.Int16) => new((char)_short),
        (LiteralKind.Char, LiteralKind.UInt16) => new((char)_ushort),
        (LiteralKind.Char, LiteralKind.Int32) => new((char)_int32),
        (LiteralKind.Char, LiteralKind.UInt32) => new((char)_uint32),
        (LiteralKind.Char, LiteralKind.Int64) => new((char)_int64),
        (LiteralKind.Char, LiteralKind.UInt64) => new((char)_uint64),
        (LiteralKind.Char, LiteralKind.Single) => new((char)_single),
        (LiteralKind.Char, LiteralKind.Double) => new((char)_double),
        (LiteralKind.Char, LiteralKind.Decimal) => new((char)_decimal),

        _ => throw new InvalidCastException($"Cannot cast from {_kind} to {targetKind}")
    };

    public static LiteralKind GetLiteralKind(string typeName) => typeName switch
    {
        "bool" or "Boolean" or "System.Boolean" => LiteralKind.Boolean,
        "sbyte" or "SByte" or "System.SByte" => LiteralKind.SByte,
        "byte" or "Byte" or "System.Byte" => LiteralKind.Byte,
        "short" or "Int16" or "System.Int16" => LiteralKind.Int16,
        "ushort" or "UInt16" or "System.UInt16" => LiteralKind.UInt16,
        "int" or "Int32" or "System.Int32" => LiteralKind.Int32,
        "uint" or "UInt32" or "System.UInt32" => LiteralKind.UInt32,
        "long" or "Int64" or "System.Int64" => LiteralKind.Int64,
        "ulong" or "UInt64" or "System.UInt64" => LiteralKind.UInt64,
        "float" or "Single" or "System.Single" => LiteralKind.Single,
        "double" or "Double" or "System.Double" => LiteralKind.Double,
        "decimal" or "Decimal" or "System.Decimal" => LiteralKind.Decimal,
        "char" or "Char" or "System.Char" => LiteralKind.Char,
        "string" or "String" or "System.String" => LiteralKind.String,
        _ => throw new ArgumentException($"Unsupported type name: {typeName}", nameof(typeName))
    };

    public static Variant operator +(Variant value) => value._kind switch
    {
        LiteralKind.SByte => new((int)value._sbyte),
        LiteralKind.Byte => new((int)value._byte),
        LiteralKind.Int16 => new((int)value._short),
        LiteralKind.UInt16 => new((int)value._ushort),
        LiteralKind.Int32 => value,
        LiteralKind.UInt32 => value,
        LiteralKind.Int64 => value,
        LiteralKind.UInt64 => value,
        LiteralKind.Single => value,
        LiteralKind.Double => value,
        LiteralKind.Decimal => value,
        _ => throw new InvalidOperationException($"Unary plus operator not supported for {value._kind}")
    };

    public static Variant operator -(Variant value) => value._kind switch
    {
        LiteralKind.SByte => new(-value._sbyte),
        LiteralKind.Byte => new(-value._byte),
        LiteralKind.Int16 => new(-value._short),
        LiteralKind.UInt16 => new(-value._ushort),
        LiteralKind.Int32 => new(-value._int32),
        LiteralKind.Int64 => new(-value._int64),
        LiteralKind.Single => new(-value._single),
        LiteralKind.Double => new(-value._double),
        LiteralKind.Decimal => new(-value._decimal),
        _ => throw new InvalidOperationException($"Unary minus operator not supported for {value._kind}")
    };

    public static Variant operator ~(Variant value) => value._kind switch
    {
        LiteralKind.Boolean => new(!value._bool),
        LiteralKind.SByte => new(~value._sbyte),
        LiteralKind.Byte => new(~value._byte),
        LiteralKind.Int16 => new(~value._short),
        LiteralKind.UInt16 => new(~value._ushort),
        LiteralKind.Int32 => new(~value._int32),
        LiteralKind.Int64 => new(~value._int64),
        _ => throw new InvalidOperationException($"Unary not operator not supported for {value._kind}")
    };

    public static Variant operator +(Variant left, Variant right) => (left._kind, right._kind) switch
    {
        (LiteralKind.Decimal, _) or (_, LiteralKind.Decimal) => new((decimal)left + (decimal)right),
        (LiteralKind.Double, _) or (_, LiteralKind.Double) => new((double)left + (double)right),
        (LiteralKind.Single, _) or (_, LiteralKind.Single) => new((float)left + (float)right),
        (LiteralKind.UInt64, LiteralKind.UInt64) => new(left._uint64 + right._uint64),
        (LiteralKind.Int64, _) or (_, LiteralKind.Int64) => new((long)left + (long)right),
        (LiteralKind.UInt64, _) or (_, LiteralKind.UInt64) => new((ulong)left + (ulong)right),
        (LiteralKind.UInt32, LiteralKind.UInt32) => new(left._uint32 + right._uint32),
        _ => new((int)left + (int)right)
    };

    public static Variant operator -(Variant left, Variant right) => (left._kind, right._kind) switch
    {
        (LiteralKind.Decimal, _) or (_, LiteralKind.Decimal) => new((decimal)left - (decimal)right),
        (LiteralKind.Double, _) or (_, LiteralKind.Double) => new((double)left - (double)right),
        (LiteralKind.Single, _) or (_, LiteralKind.Single) => new((float)left - (float)right),
        (LiteralKind.UInt64, LiteralKind.UInt64) => new(left._uint64 - right._uint64),
        (LiteralKind.Int64, _) or (_, LiteralKind.Int64) => new((long)left - (long)right),
        (LiteralKind.UInt64, _) or (_, LiteralKind.UInt64) => new((ulong)left - (ulong)right),
        (LiteralKind.UInt32, LiteralKind.UInt32) => new(left._uint32 - right._uint32),
        _ => new((int)left - (int)right)
    };

    public static Variant operator *(Variant left, Variant right) => (left._kind, right._kind) switch
    {
        (LiteralKind.Decimal, _) or (_, LiteralKind.Decimal) => new((decimal)left * (decimal)right),
        (LiteralKind.Double, _) or (_, LiteralKind.Double) => new((double)left * (double)right),
        (LiteralKind.Single, _) or (_, LiteralKind.Single) => new((float)left * (float)right),
        (LiteralKind.UInt64, LiteralKind.UInt64) => new(left._uint64 * right._uint64),
        (LiteralKind.Int64, _) or (_, LiteralKind.Int64) => new((long)left * (long)right),
        (LiteralKind.UInt64, _) or (_, LiteralKind.UInt64) => new((ulong)left * (ulong)right),
        (LiteralKind.UInt32, LiteralKind.UInt32) => new(left._uint32 * right._uint32),
        _ => new((int)left * (int)right)
    };

    public static Variant operator /(Variant left, Variant right) => (left._kind, right._kind) switch
    {
        (LiteralKind.Decimal, _) or (_, LiteralKind.Decimal) => new((decimal)left / (decimal)right),
        (LiteralKind.Double, _) or (_, LiteralKind.Double) => new((double)left / (double)right),
        (LiteralKind.Single, _) or (_, LiteralKind.Single) => new((float)left / (float)right),
        (LiteralKind.UInt64, LiteralKind.UInt64) => new(left._uint64 / right._uint64),
        (LiteralKind.Int64, _) or (_, LiteralKind.Int64) => new((long)left / (long)right),
        (LiteralKind.UInt64, _) or (_, LiteralKind.UInt64) => new((ulong)left / (ulong)right),
        (LiteralKind.UInt32, LiteralKind.UInt32) => new(left._uint32 / right._uint32),
        _ => new((int)left / (int)right)
    };

    public static Variant operator %(Variant left, Variant right) => (left._kind, right._kind) switch
    {
        (LiteralKind.Decimal, _) or (_, LiteralKind.Decimal) => new((decimal)left % (decimal)right),
        (LiteralKind.Double, _) or (_, LiteralKind.Double) => new((double)left % (double)right),
        (LiteralKind.Single, _) or (_, LiteralKind.Single) => new((float)left % (float)right),
        (LiteralKind.UInt64, LiteralKind.UInt64) => new(left._uint64 % right._uint64),
        (LiteralKind.Int64, _) or (_, LiteralKind.Int64) => new((long)left % (long)right),
        (LiteralKind.UInt64, _) or (_, LiteralKind.UInt64) => new((ulong)left % (ulong)right),
        (LiteralKind.UInt32, LiteralKind.UInt32) => new(left._uint32 % right._uint32),
        _ => new((int)left % (int)right)
    };

    public static Variant operator &(Variant left, Variant right) => (left._kind, right._kind) switch
    {
        (LiteralKind.Boolean, LiteralKind.Boolean) => new(left._bool & right._bool),
        (LiteralKind.UInt64, LiteralKind.UInt64) => new(left._uint64 & right._uint64),
        (LiteralKind.Int64, _) or (_, LiteralKind.Int64) => new((long)left & (long)right),
        (LiteralKind.UInt64, _) or (_, LiteralKind.UInt64) => new((ulong)left & (ulong)right),
        (LiteralKind.UInt32, LiteralKind.UInt32) => new(left._uint32 & right._uint32),
        _ => new((int)left & (int)right)
    };

    public static Variant operator |(Variant left, Variant right) => (left._kind, right._kind) switch
    {
        (LiteralKind.Boolean, LiteralKind.Boolean) => new(left._bool | right._bool),
        (LiteralKind.UInt64, LiteralKind.UInt64) => new(left._uint64 | right._uint64),
        (LiteralKind.Int64, _) or (_, LiteralKind.Int64) => new((long)left | (long)right),
        (LiteralKind.UInt64, _) or (_, LiteralKind.UInt64) => new((ulong)left | (ulong)right),
        (LiteralKind.UInt32, LiteralKind.UInt32) => new(left._uint32 | right._uint32),
        _ => new((int)left | (int)right)
    };

    public static explicit operator int(Variant value) => value._kind switch
    {
        LiteralKind.SByte => value._sbyte,
        LiteralKind.Byte => value._byte,
        LiteralKind.Int16 => value._short,
        LiteralKind.UInt16 => value._ushort,
        LiteralKind.Int32 => value._int32,
        LiteralKind.Char => value._char,
        _ => throw new InvalidOperationException($"Cannot convert {value._kind} to Int32")
    };

    public static explicit operator uint(Variant value) => value._kind switch
    {
        LiteralKind.Byte => value._byte,
        LiteralKind.UInt16 => value._ushort,
        LiteralKind.UInt32 => value._uint32,
        LiteralKind.Char => value._char,
        _ => (uint)(int)value
    };

    public static explicit operator long(Variant value) => value._kind switch
    {
        LiteralKind.SByte => value._sbyte,
        LiteralKind.Byte => value._byte,
        LiteralKind.Int16 => value._short,
        LiteralKind.UInt16 => value._ushort,
        LiteralKind.Int32 => value._int32,
        LiteralKind.UInt32 => value._uint32,
        LiteralKind.Int64 => value._int64,
        LiteralKind.Char => value._char,
        _ => throw new InvalidOperationException($"Cannot convert {value._kind} to Int64")
    };

    public static explicit operator ulong(Variant value) => value._kind switch
    {
        LiteralKind.Byte => value._byte,
        LiteralKind.UInt16 => value._ushort,
        LiteralKind.UInt32 => value._uint32,
        LiteralKind.UInt64 => value._uint64,
        LiteralKind.Char => value._char,
        _ => (ulong)(long)value
    };

    public static explicit operator float(Variant value) => value._kind switch
    {
        LiteralKind.SByte => value._sbyte,
        LiteralKind.Byte => value._byte,
        LiteralKind.Int16 => value._short,
        LiteralKind.UInt16 => value._ushort,
        LiteralKind.Int32 => value._int32,
        LiteralKind.UInt32 => value._uint32,
        LiteralKind.Int64 => value._int64,
        LiteralKind.UInt64 => value._uint64,
        LiteralKind.Single => value._single,
        LiteralKind.Char => value._char,
        _ => throw new InvalidOperationException($"Cannot convert {value._kind} to Single")
    };

    public static explicit operator double(Variant value) => value._kind switch
    {
        LiteralKind.SByte => value._sbyte,
        LiteralKind.Byte => value._byte,
        LiteralKind.Int16 => value._short,
        LiteralKind.UInt16 => value._ushort,
        LiteralKind.Int32 => value._int32,
        LiteralKind.UInt32 => value._uint32,
        LiteralKind.Int64 => value._int64,
        LiteralKind.UInt64 => value._uint64,
        LiteralKind.Single => value._single,
        LiteralKind.Double => value._double,
        LiteralKind.Char => value._char,
        _ => throw new InvalidOperationException($"Cannot convert {value._kind} to Double")
    };

    public static explicit operator decimal(Variant value) => value._kind switch
    {
        LiteralKind.SByte => value._sbyte,
        LiteralKind.Byte => value._byte,
        LiteralKind.Int16 => value._short,
        LiteralKind.UInt16 => value._ushort,
        LiteralKind.Int32 => value._int32,
        LiteralKind.UInt32 => value._uint32,
        LiteralKind.Int64 => value._int64,
        LiteralKind.UInt64 => value._uint64,
        LiteralKind.Decimal => value._decimal,
        LiteralKind.Char => value._char,
        _ => throw new InvalidOperationException($"Cannot convert {value._kind} to Decimal")
    };

    public static explicit operator bool(Variant value) => value._kind switch
    {
        LiteralKind.Boolean => value._bool,
        _ => throw new InvalidOperationException($"Cannot convert {value._kind} to Boolean")
    };
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
    Array,
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace Generators.AttributeTemplates.Targets;

internal readonly struct ArgumentList(string attributeId, object?[]? values, string? culture)
{
    public string AttributeId { get; } = attributeId;
    private readonly object?[]? _values = values;
    public IFormatProvider Culture { get; } = GetCultureInfo(culture);

    public static ArgumentList? Create(SemanticModel semantics, AttributeSyntax a)
    {
        var at = semantics.GetTypeInfo(a);

        if (at.Type is not { } t) return null;

        var bat = at.Type?.BaseType;
        if (!bat.IsTemplateAttribute()) return null;

        var id = t.GetUniqueId();

        if (a.ArgumentList is not { } list) return new(id, null, null);

        string? culture = null;

        var count = 0;
        foreach (var arg in list.Arguments)
        {
            if (arg.NameEquals is { } n)
            {
                //todo: ensure n.Name == "CultureName"
                var v = semantics.GetConstantValue(arg.Expression);
                //if (!v.HasValue || v.Value is not string) todo: error
                culture = (string?)v.Value;
                break;
            }
            ++count;
        }

        var values = new object?[count];
        var i = 0;

        foreach (var arg in list.Arguments.Take(count))
        {
            var v = semantics.GetConstantValue(arg.Expression);
            //if (!v.HasValue) todo: error
            values[i++] = v.Value;
        }

        return new(id, values, culture);
    }

    public static CultureInfo GetCultureInfo(string? culture)
    {
        if (culture is not { } name) return CultureInfo.InvariantCulture;

        try
        {
            var c = CultureInfo.GetCultureInfo(name);
            if (c.LCID == 4096) return CultureInfo.InvariantCulture; // error?
            else return c;
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }


    public int Count => _values?.Length ?? 0;
    public object? this[int index] => _values![index];
}

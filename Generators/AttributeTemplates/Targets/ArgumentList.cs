using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace Generators.AttributeTemplates.Targets;

internal readonly struct ArgumentList(string attributeId, Variant[]? values, string? culture)
{
    public string AttributeId { get; } = attributeId;
    private readonly Variant[]? _values = values;
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
            if (arg.NameEquals is { } n && n.Name.Identifier.ValueText == Intrinsic.CultureName)
            {
                var v = semantics.GetConstantValue(arg.Expression);
                if (!v.HasValue || v.Value is not string) throw AttributeTemplateException.Unreachable(arg.GetLocation());
                culture = (string?)v.Value;
                break;
            }
            ++count;
        }

        var values = new Variant[count];
        var i = 0;

        foreach (var arg in list.Arguments.Take(count))
        {
            values[i++] = GetArgumentValue(semantics, arg.Expression);
        }

        return new(id, values, culture);
    }

    private static Variant GetArgumentValue(SemanticModel semantics, ExpressionSyntax expression)
    {
        // Handle collection expressions [1, 2, 3]
        if (expression is CollectionExpressionSyntax collection)
        {
            var elements = collection.Elements
                .OfType<ExpressionElementSyntax>()
                .Select(elem => GetArgumentValue(semantics, elem.Expression))
                .ToArray();
            return new(elements);
        }

        // Handle implicit array creation new[] { 1, 2, 3 }
        if (expression is ImplicitArrayCreationExpressionSyntax implicitArray)
        {
            var elements = implicitArray.Initializer.Expressions
                .Select(expr => GetArgumentValue(semantics, expr))
                .ToArray();
            return new(elements);
        }

        // Handle explicit array creation new int[] { 1, 2, 3 }
        if (expression is ArrayCreationExpressionSyntax arrayCreation && arrayCreation.Initializer is { } initializer)
        {
            var elements = initializer.Expressions
                .Select(expr => GetArgumentValue(semantics, expr))
                .ToArray();
            return new(elements);
        }

        // Handle constant values
        var v = semantics.GetConstantValue(expression);
        if (!v.HasValue) throw AttributeTemplateException.Unreachable(expression.GetLocation());
        return Variant.TryFromObject(v.Value) ?? throw AttributeTemplateException.Unreachable(expression.GetLocation());
    }

    public static CultureInfo GetCultureInfo(string? culture)
    {
        if (culture is not { } name) return CultureInfo.InvariantCulture;

        try
        {
            var c = CultureInfo.GetCultureInfo(name);
            if (c.LCID == 4096) throw AttributeTemplateException.UnknownCultureName(name, Location.None);
            else return c;
        }
        catch (CultureNotFoundException)
        {
            throw AttributeTemplateException.UnknownCultureName(name, Location.None);
        }
    }


    public int Count => _values?.Length ?? 0;
    public Variant this[int index] => _values![index];
}

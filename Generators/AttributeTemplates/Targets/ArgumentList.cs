using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

internal readonly struct ArgumentList
{
    private readonly object?[]? _values;
    public string? Culture { get; }

    public ArgumentList(SemanticModel semantics, AttributeArgumentListSyntax? list)
    {
        if (list is null)
        {
            _values = null;
            return;
        }

        var count = 0;
        foreach (var a in list.Arguments)
        {
            ++count;
            if (a.NameEquals is { } n)
            {
                //todo: ensure n.Name == "CultureName"
                var v = semantics.GetConstantValue(a.Expression);
                //if (!v.HasValue || v.Value is not string) todo: error
                Culture = (string?)v.Value;
                break;
            }
        }

        var values = new object?[count];
        var i = 0;

        foreach (var a in list.Arguments.Take(count))
        {
            var v = semantics.GetConstantValue(a.Expression);
            //if (!v.HasValue) todo: error
            values[i++] = v.Value;
        }

        _values = values;
    }

    public int Count => _values?.Length ?? 0;
    public object? this[int index] => _values![index];
}

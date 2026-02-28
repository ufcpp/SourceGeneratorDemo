using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

internal readonly struct ArgumentList
{
    private readonly object?[]? _values;

    public ArgumentList(SemanticModel semantics, AttributeArgumentListSyntax? list)
    {
        if (list is null)
        {
            _values = null;
            return;
        }

        var values = new object?[list.Arguments.Count];
        var i = 0;

        foreach (var a in list.Arguments)
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

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal readonly struct ParameterList
{
    private readonly string[]? _parameterNames;
    private readonly Dictionary<string, string>? _nameToTypeTable;

    public ParameterList(ParameterListSyntax? list)
    {
        if (list is null)
        {
            _parameterNames = null;
            _nameToTypeTable = null;
            return;
        }

        var names = new string[list.Parameters.Count];
        Dictionary<string, string> d = [];
        var i = 0;

        foreach (var p in list.Parameters)
        {
            if (p.Type is not PredefinedTypeSyntax pd) continue; // todo: error
            d.Add(p.Identifier.ValueText, pd.Keyword.ValueText);
            names[i++] = p.Identifier.ValueText;
        }

        _parameterNames = names;
        _nameToTypeTable = d;
    }

    public int Count => _parameterNames?.Length ?? 0;
    public string this[int index] => _parameterNames![index];
    public bool Contains(string parameterName) => _nameToTypeTable is { } t && t.ContainsKey(parameterName);
}

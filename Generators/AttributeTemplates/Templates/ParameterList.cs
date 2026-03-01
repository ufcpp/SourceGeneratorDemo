using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal readonly struct ParameterList
{
    private readonly (string name, string type)[]? _parameters;
    private readonly Dictionary<string, int>? _table;

    public ParameterList(ParameterListSyntax? list)
    {
        if (list is null)
        {
            _parameters = null;
            _table = null;
            return;
        }

        var parameters = new (string, string)[list.Parameters.Count];
        Dictionary<string, int> d = [];
        var i = 0;

        foreach (var p in list.Parameters)
        {
            if (p.Type is not PredefinedTypeSyntax pd)
            {
                //todo: support when p.Type is a generic parameter type

                continue; // todo: error
            }

            d.Add(p.Identifier.ValueText, i);
            parameters[i++] = (p.Identifier.ValueText, pd.Keyword.ValueText);
        }

        _parameters = parameters;
        _table = d;
    }

    public int Count => _parameters?.Length ?? 0;
    public string this[int index] => _parameters![index].name;
    public int? GetIndex(string name) => _table is { } t && t.TryGetValue(name, out var i) ? i : null;
    public bool Contains(string parameterName) => _table is { } t && t.ContainsKey(parameterName);
}

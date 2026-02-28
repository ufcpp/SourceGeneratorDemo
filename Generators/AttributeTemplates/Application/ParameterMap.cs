using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;

namespace Generators.AttributeTemplates.Application;

internal readonly struct ParameterMap
{
    private readonly Dictionary<string, object?>? _map;
    public ParameterMap(ParameterList parameters, ArgumentList arguments)
    {
        //if (parameters.Count != arguments.Count) todo: error

        for (var i = 0; i < parameters.Count; i++)
        {
            (_map ??= []).Add(parameters[i], arguments[i]);
        }
    }

    public object? this[string parameterName] => _map is { } t && t.TryGetValue(parameterName, out var v) ? v : null;
}

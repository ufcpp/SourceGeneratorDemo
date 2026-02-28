using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;
using System.Globalization;

namespace Generators.AttributeTemplates.Application;

internal readonly struct ParameterMap
{
    private readonly Dictionary<string, object?>? _map;
    private readonly string? _culture;

    public ParameterMap(ParameterList parameters, ArgumentList arguments)
    {
        //if (parameters.Count != arguments.Count) todo: error

        for (var i = 0; i < parameters.Count; i++)
        {
            (_map ??= []).Add(parameters[i], arguments[i]);
        }

        _culture = arguments.Culture;
    }

    public object? this[string parameterName] => _map is { } t && t.TryGetValue(parameterName, out var v) ? v : null;

    public CultureInfo GetCultureInfo()
    {
        if(_culture is not { } name) return CultureInfo.InvariantCulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo(name);
            if (culture.LCID == 4096) return CultureInfo.InvariantCulture; // error?
            else return culture;
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}

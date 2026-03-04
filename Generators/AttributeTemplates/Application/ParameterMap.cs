using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;

namespace Generators.AttributeTemplates.Application;

internal readonly struct ParameterMap(ParameterList parameters, ArgumentList arguments)
{
    public Variant this[string parameterName] => parameters.GetIndex(parameterName) is { } i ? arguments[i] : default;
    public IFormatProvider Culture => arguments.Culture;
}

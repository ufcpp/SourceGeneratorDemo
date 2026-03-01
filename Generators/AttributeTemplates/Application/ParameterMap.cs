using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;

namespace Generators.AttributeTemplates.Application;

internal readonly struct ParameterMap(ParameterList parameters, ArgumentList arguments)
{
    public object? this[string parameterName] => parameters.GetIndex(parameterName) is { } i ? arguments[i] : null;
}

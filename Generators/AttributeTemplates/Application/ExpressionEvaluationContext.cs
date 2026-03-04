using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;

namespace Generators.AttributeTemplates.Application;

internal class ExpressionEvaluationContext(MemberHierarchy member, ParameterMap map) : IExpressionEvaluationContext
{
    public Variant this[string parameterName] => map[parameterName];
    public IFormatProvider Culture => map.Culture;
    public IntrinsicError TryGetIntrinsicValue(string id, Index level, int? parameterIndex, out string? value)
        => member.TryGetIntrinsicValue(id, level, parameterIndex, out value);
}

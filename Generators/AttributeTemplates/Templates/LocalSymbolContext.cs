namespace Generators.AttributeTemplates.Templates;

internal class LocalSymbolContext : IExpressionEvaluationContext
{
    private readonly IExpressionEvaluationContext _parent;
    private readonly (string name, Variant value)[] _variables;

    public LocalSymbolContext(IExpressionEvaluationContext parent, params (string name, Variant value)[] variables)
    {
        _parent = parent;
        _variables = variables;
    }

    public Variant? this[string parameterName]
    {
        get
        {
            foreach (var (name, value) in _variables)
            {
                if (name == parameterName) return value;
            }
            return _parent[parameterName];
        }
    }

    public IFormatProvider Culture => _parent.Culture;

    public IntrinsicError TryGetIntrinsicValue(string id, Index level, int? parameterIndex, out string? value)
        => _parent.TryGetIntrinsicValue(id, level, parameterIndex, out value);
}

namespace Generators.AttributeTemplates.Templates;

internal interface IExpressionEvaluationContext
{
    bool TryGetIntrinsicValue(string id, int level, int? parameterIndex, out string? value);
    object? this[string parameterName] { get; }
    IFormatProvider Culture { get; }
}

namespace Generators.AttributeTemplates.Templates;

internal interface IExpressionEvaluationContext
{
    bool TryGetIntrinsicValue(string id, Index level, int? parameterIndex, out string? value);
    object? this[string parameterName] { get; }
    IFormatProvider Culture { get; }
}

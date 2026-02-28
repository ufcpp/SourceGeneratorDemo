using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal readonly record struct InterpolationContent(string? Text = null, object? ConstantValue = null, string? Identifier = null, int Level = 0, int? Alignment = null, string? Format = null)
{
    public static InterpolationContent Create(SemanticModel semantics, InterpolatedStringContentSyntax c)
    {
        if (c is InterpolatedStringTextSyntax t)
        {
            return new(Text: t.TextToken.ValueText);
        }
        else if (c is InterpolationSyntax i)
        {
            int? align = i.AlignmentClause is { } a && semantics.GetConstantValue(a.Value) is { HasValue: true, Value: int x } ? x : null;
            var fmt = i.FormatClause?.FormatStringToken.ValueText;

            var e = i.Expression;
            (var level, e) = IntrinsicMethod.GetLevelAndExpression(semantics, e);

            if (e is IdentifierNameSyntax id)
            {
                return new(Identifier: id.Identifier.ValueText, Level: level, Alignment: align, Format: fmt);
            }
            else
            {
                var v = semantics.GetConstantValue(e);
                if (v.HasValue) return new(ConstantValue: v.Value, Level: level, Alignment: align, Format: fmt);
            }
        }
        return default;
    }

    public override string ToString()
    {
        if (Text is { } t) return t;
        if (ConstantValue is { } c) return $"{{{c},{Alignment}:{Format}}}";
        if (Identifier is { } i) return $"{{{i},{Alignment}:{Format}}}";
        return "";
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal record MemberTemplate(int Level, Interpolation? Interpolation = null, string? Constant = null, string? Identifier = null)
{
    public static MemberTemplate[]? Create(SemanticModel semantics, BaseListSyntax b, ParameterList parameters)
    {
        foreach (var t in b.Types)
        {
            if (t is not PrimaryConstructorBaseTypeSyntax pc) continue;

            var ti = semantics.GetTypeInfo(t.Type);
            if (ti.Type.IsTemplateAttribute())
            {
                var templates = new MemberTemplate[pc.ArgumentList.Arguments.Count];
                var i = 0;
                foreach (var arg in pc.ArgumentList.Arguments)
                {
                    templates[i++] = Create(semantics, parameters, arg.Expression);
                }
                return templates;
            }
        }

        return null;
    }

    public static MemberTemplate Create(SemanticModel semantics, ParameterList parameters, ExpressionSyntax e)
    {
        (var level, e) = Intrinsic.GetLevelAndExpression(semantics, e);

        if (e is InterpolatedStringExpressionSyntax i)
        {
            return new(level, Interpolation: new(semantics, i));
        }
        else if (e is IdentifierNameSyntax id)
        {
            var s0 = semantics.GetConstantValue(id);
            if (s0.HasValue && s0.Value is string s) return new(level, Constant: s);

            var name = id.Identifier.ValueText;
            if (parameters.Contains(name) || name == Intrinsic.Type || name == Intrinsic.Name)
                return new(level, Identifier: name);
        }
        else if (e is LiteralExpressionSyntax l)
        {
            var s0 = semantics.GetConstantValue(l);
            if (s0.HasValue && s0.Value is string s) return new(level, Constant: s);
        }

        //todo: error
        return new(0);
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal record MemberTemplate(Index Level, MemberExpression Expression)
{
    public static MemberTemplate[]? Create(SemanticModel semantics, BaseListSyntax b, ParameterList parameters)
    {
        foreach (var t in b.Types)
        {
            if (t is not PrimaryConstructorBaseTypeSyntax pc) continue;

            // Quick syntax-based filter: skip types that clearly aren't TemplateAttribute
            // This avoids expensive GetTypeInfo() calls for unrelated base types
            var typeString = t.Type.ToString();
            if (!typeString.Contains("Attribute") && !typeString.Contains("Template"))
            {
                continue;
            }

            var ti = semantics.GetTypeInfo(t.Type);
            if (ti.Type.IsTemplateAttribute())
            {
                var templates = new MemberTemplate[pc.ArgumentList.Arguments.Count];
                var i = 0;
                foreach (var arg in pc.ArgumentList.Arguments)
                {
                    templates[i++] = Create(semantics, arg.Expression, parameters);
                }
                return templates;
            }
        }

        return null;
    }

    public static MemberTemplate Create(SemanticModel semantics, ExpressionSyntax e, ParameterList parameters)
    {
        (var level, e) = Intrinsic.GetLevelAndExpression(semantics, e);
        return new(level, ExpressionWalker.Create(semantics, e, parameters));
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal record MemberTemplate(int Level, MemberExpression Expression)
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
        return new(level, MemberExpression.Create(semantics, e, parameters));
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal static class IntrinsicMethod
{
    public static (int level, ExpressionSyntax expression) GetLevelAndExpression(SemanticModel semantics, ExpressionSyntax e)
    {
        int? IntValue(ExpressionSyntax ex)
        {
            var v = semantics.GetConstantValue(ex);
            if (v.HasValue && v.Value is int i) return i;
            return null;
        }
        int level = 0;
        if (e is InvocationExpressionSyntax { Expression: IdentifierNameSyntax n, ArgumentList.Arguments: var args })
        {
            var name = n.Identifier.ValueText;
            if (name == "Parent" && args.Count == 1)
            {
                level = 1;
                e = args[0].Expression;
            }
            else if (name == "Global" && args.Count == 1)
            {
                level = -1;
                e = args[0].Expression;
            }
            if (name == "Up" && args.Count == 2 && IntValue(args[0].Expression) is int x)
            {
                level = x + 1;
                e = args[1].Expression;
            }
            else if (name == "Down" && args.Count == 2 && IntValue(args[0].Expression) is int y)
            {
                level = -y;
                e = args[1].Expression;
            }
            // else error?
        }
        return (level, e);
    }
}

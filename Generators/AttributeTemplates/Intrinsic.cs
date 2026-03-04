using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates;

internal static class Intrinsic
{
    public static void AddCommonSource(this IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
    "TemplateAttribute.g.cs",
/* lang=C# */
"""
#nullable enable
#pragma warning disable CS9113, IDE0060
using System.Diagnostics.CodeAnalysis;

namespace AttributeTemplateGenerator;

[System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
internal class TemplateAttribute([StringSyntax("C#")] params string[] templates) : Attribute
{
    protected static string Parent(string template) => template;
    protected static string Global(string template) => template;
    protected static ReadOnlySpan<Func<string, string>> Ancestor => default;
    protected static ReadOnlySpan<(string Name, string Type)> Param => default;
    protected const string Type = "";
    protected const string Name = "";
    public string? CultureName { get; set; }
}

"""));

        // should support?:
        // expression-bodied methods/properties
        // : base clause for classes/structs
        // : this, : base clause for constructors
    }

    public static bool IsTemplateAttribute(this ITypeSymbol? type)
    {
        return type is { ContainingNamespace.Name: "AttributeTemplateGenerator", Name: "TemplateAttribute" };
    }

    public const string Parent = nameof(Parent);
    public const string Global = nameof(Global);
    public const string Ancestor = nameof(Ancestor);
    public const string Param = nameof(Param);
    public const string Type = nameof(Type);
    public const string Name = nameof(Name);

    public static (int level, int? parameterIndex, string? kind) GetIntrinsicValue(SemanticModel semantics, ExpressionSyntax e)
    {
        (var level, e) = GetLevelAndExpression(semantics, e);

        int? index = null;

        // Param[n].
        if (e is MemberAccessExpressionSyntax
            {
                Name: var e1,
                Expression: ElementAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: Param },
                    ArgumentList.Arguments: [var arg]
                }
            }
            && IntValue(arg.Expression, semantics) is int paramIndex)
        {
            index = paramIndex;
            e = e1;
        }

        // Check for simple Name or Type identifier
        if (e is IdentifierNameSyntax id)
        {
            var name = id.Identifier.ValueText;
            if (name == Type || name == Name) return (level, index, name);
        }

        return default;
    }

    public static (int level, ExpressionSyntax expression) GetLevelAndExpression(SemanticModel semantics, ExpressionSyntax e)
    {
        int level = 0;
        if (e is InvocationExpressionSyntax i)
        {
            var name = i.Expression switch
            {
                IdentifierNameSyntax n => n.Identifier.ValueText,
                ElementAccessExpressionSyntax ea when ea.Expression is IdentifierNameSyntax ean => ean.Identifier.ValueText,
                _ => null
            };

            if (name is null) return (0, e);

            var args = i.ArgumentList.Arguments;
            if (name == Parent && args.Count == 1)
            {
                level = 1;
                e = args[0].Expression;
            }
            else if (name == Global && args.Count == 1)
            {
                level = -1;
                e = args[0].Expression;
            }
            else if (name == Ancestor && i.Expression is ElementAccessExpressionSyntax ancestorAccess && args.Count == 1)
            {
                // Ancestor[n]($"...") pattern
                if (ancestorAccess.ArgumentList.Arguments.Count != 1) return (0, e);

                var indexExpr = ancestorAccess.ArgumentList.Arguments[0].Expression;
                // Parse Index: either int literal or ^int
                if (indexExpr is LiteralExpressionSyntax literal && literal.Token.Value is int value)
                {
                    // Ancestor[0]($"...") -> level = 0 (current member)
                    // Ancestor[1]($"...") -> level = 1 (parent)
                    // Ancestor[2]($"...") -> level = 2 (grandparent)
                    level = value;
                    e = args[0].Expression;
                }
                else if (indexExpr is PrefixUnaryExpressionSyntax { OperatorToken.ValueText: "^" } hat
                    && hat.Operand is LiteralExpressionSyntax hatLiteral 
                    && hatLiteral.Token.Value is int hatValue)
                {
                    // Ancestor[^0]($"...") -> level = -1 (Global)
                    // Ancestor[^1]($"...") -> level = -2 (one level down from Global)
                    // Ancestor[^2]($"...") -> level = -3 (two levels down from Global)
                    level = -hatValue - 1;
                    e = args[0].Expression;
                }
                else if (IntValue(indexExpr, semantics) is int constValue)
                {
                    // Constant expression
                    level = constValue;
                    e = args[0].Expression;
                }
            }
        }

        return (level, e);
    }

    private static int? IntValue(ExpressionSyntax ex, SemanticModel semantics)
    {
        if (ex is LiteralExpressionSyntax l && l.Token.Value is int literalValue)
        {
            return literalValue;
        }

        var v = semantics.GetConstantValue(ex);
        if (v.HasValue && v.Value is int i) return i;
        return null;
    }
}

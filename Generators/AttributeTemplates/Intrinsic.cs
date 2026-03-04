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
    protected static string Up(int level, string template) => template;
    protected static string Down(int level, string template) => template;
    protected static ReadOnlySpan<(string Name, string Type> Param => default;
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
    public const string Up = nameof(Up);
    public const string Down = nameof(Down);
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
            if (i.Expression is not IdentifierNameSyntax n) return (0, e);

            var name = n.Identifier.ValueText;
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
            else if (name == Up && args.Count == 2 && IntValue(args[0].Expression, semantics) is int x)
            {
                level = x + 1;
                e = args[1].Expression;
            }
            else if (name == Down && args.Count == 2 && IntValue(args[0].Expression, semantics) is int y)
            {
                level = -y - 1;
                e = args[1].Expression;
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

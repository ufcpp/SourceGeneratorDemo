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
    protected static string Parent(string template) => Up(0, template);
    protected static string Global(string template) => Down(0, template);
    protected static string Up(int level, string template) => template;
    protected static string Down(int level, string template) => template;
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
    public const string Type = nameof(Type);
    public const string Name = nameof(Name);

    public static (int level, ExpressionSyntax expression) GetLevelAndExpression(SemanticModel semantics, ExpressionSyntax e)
    {
        int? IntValue(ExpressionSyntax ex)
        {
            if (ex is LiteralExpressionSyntax l && l.Token.Value is int literalValue)
            {
                return literalValue;
            }

            var v = semantics.GetConstantValue(ex);
            if (v.HasValue && v.Value is int i) return i;
            return null;
        }
        int level = 0;
        if (e is InvocationExpressionSyntax { Expression: IdentifierNameSyntax n, ArgumentList.Arguments: var args })
        {
            var name = n.Identifier.ValueText;
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
            if (name == Up && args.Count == 2 && IntValue(args[0].Expression) is int x)
            {
                level = x + 1;
                e = args[1].Expression;
            }
            else if (name == Down && args.Count == 2 && IntValue(args[0].Expression) is int y)
            {
                level = -y - 1;
                e = args[1].Expression;
            }
            // else error?
        }
        return (level, e);
    }
}

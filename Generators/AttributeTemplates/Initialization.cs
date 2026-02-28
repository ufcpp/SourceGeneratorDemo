using Microsoft.CodeAnalysis;

namespace Generators.AttributeTemplates;

internal static class Initialization
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
}

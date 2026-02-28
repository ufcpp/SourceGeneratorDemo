using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;
using Microsoft.CodeAnalysis;

namespace Generators.AttributeTemplates.Application;

internal static class ApplicationProvider
{
    public static void RegisterSourceOutput(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<TemplateDefinition> templateProvider,
        IncrementalValuesProvider<TemplateTarget> memberProvider)
    {
        context.RegisterSourceOutput(templateProvider.Combine(memberProvider));
    }

    public static IncrementalValuesProvider<GenerationInfo> Combine(
        this IncrementalValuesProvider<TemplateDefinition> templateProvider,
        IncrementalValuesProvider<TemplateTarget> memberProvider)
    {
        return memberProvider.Combine(templateProvider.Collect())
            .SelectMany((t, _) => t.Right
                .Where(x => x!.Attribute == t.Left!.Attribute)
                .Select(f => new GenerationInfo(f!.Attribute, new(f.Templates, t.Left!.Member), new(f.Params, t.Left.Args)))
            )
            .Where(x => x != null);
    }

    public static void RegisterSourceOutput(this IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<GenerationInfo> provider)
    {
        context.RegisterSourceOutput(provider, (context, info) =>
        {
            var code = info.Generate();
            context.AddSource($"ATG_{info.Attribute}_{info.Templates.GetId()}g.cs", code);
        });
    }
}

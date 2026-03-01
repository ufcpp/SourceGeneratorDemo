using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;
using Microsoft.CodeAnalysis;

namespace Generators.AttributeTemplates.Application;

internal static class ApplicationProvider
{
    public static void RegisterSourceOutput(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<TemplateDefinition> templateProvider,
        IncrementalValuesProvider<TemporaryTemplateTarget> memberProvider)
    {
        context.RegisterSourceOutput(templateProvider.Combine(memberProvider));
    }

    public static IncrementalValuesProvider<GenerationInfo> Combine(
        this IncrementalValuesProvider<TemplateDefinition> templateProvider,
        IncrementalValuesProvider<TemporaryTemplateTarget> memberProvider)
    {
        return memberProvider.Collect().Combine(templateProvider.Collect())
            .SelectMany((t, _) =>
            {
                var templates = t.Right.ToDictionary(t => t.AttributeId);
                var targets = TemplateTarget.Group(t.Left);

                return targets.Select(t =>
                {
                    var code = Generator.Generate(t, templates);
                    return new GenerationInfo(t.MemberId, code);
                }).ToArray();
            });
    }

    public static void RegisterSourceOutput(this IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<GenerationInfo> provider)
    {
        context.RegisterSourceOutput(provider, (context, info) =>
        {
            var code = info.GeneratedCode;
            context.AddSource($"ATG_{info.MemberId}.g.cs", code);
        });
    }
}

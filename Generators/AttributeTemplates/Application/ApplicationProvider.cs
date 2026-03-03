using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;
using Microsoft.CodeAnalysis;

namespace Generators.AttributeTemplates.Application;

internal static class ApplicationProvider
{
    public static void RegisterSourceOutput(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Result<TemplateDefinition>> templateProvider,
        IncrementalValuesProvider<TemporaryTemplateTarget> memberProvider)
    {
        context.RegisterSourceOutput(templateProvider
            .Select((t, _) => t.Error!)
            .Where(d => d != null),
            (c, d) => c.ReportDiagnostic(d));

        context.RegisterSourceOutput(templateProvider.Combine(memberProvider));
    }

    public static IncrementalValuesProvider<GenerationInfo> Combine(
        this IncrementalValuesProvider<Result<TemplateDefinition>> templateProvider,
        IncrementalValuesProvider<TemporaryTemplateTarget> memberProvider)
    {
        return memberProvider.Collect().Combine(templateProvider.Select((t, _) => t.Value!).Where(v => v is not null).Collect())
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

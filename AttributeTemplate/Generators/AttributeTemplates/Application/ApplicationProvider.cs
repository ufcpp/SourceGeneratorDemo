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

    public static IncrementalValuesProvider<Result<GenerationInfo>> Combine(
        this IncrementalValuesProvider<TemplateDefinition> templateProvider,
        IncrementalValuesProvider<TemporaryTemplateTarget> memberProvider)
    {
        return memberProvider.Collect().Combine(templateProvider.Where(v => v is not null).Collect())
            .SelectMany((t, _) =>
            {
                try
                {
                    var templates = t.Right.ToDictionary(t => t.AttributeId);
                    var targets = TemplateTarget.Group(t.Left);

                    return targets.Select(t =>
                    {
                        try
                        {
                            var code = Generator.Generate(t, templates);
                            return new Result<GenerationInfo>(new GenerationInfo(t.MemberId, code));
                        }
                        catch (AttributeTemplateException e)
                        {
                            return new Result<GenerationInfo>(e.Diagnostic);
                        }
                        catch (Exception e)
                        {
                            // Catch unexpected exceptions
                            var diagnostic = AttributeTemplateException.UnknownError(t.Member.Location, e.Message).Diagnostic;
                            return new Result<GenerationInfo>(diagnostic);
                        }
                    }).ToArray();
                }
                catch (AttributeTemplateException e)
                {
                    return [new Result<GenerationInfo>(e.Diagnostic)];
                }
                catch (Exception e)
                {
                    // Catch unexpected exceptions in TemplateTarget.Group
                    var location = t.Left.Length > 0 ? t.Left[0].Member.GetLocation() : Location.None;
                    var diagnostic = AttributeTemplateException.UnknownError(location, e.Message).Diagnostic;
                    return [new Result<GenerationInfo>(diagnostic)];
                }
            });
    }

    public static void RegisterSourceOutput(this IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<Result<GenerationInfo>> provider)
    {
        context.RegisterSourceOutput(provider
            .Select((r, _) => r.Error!)
            .Where(e => e is not null),
            (context, error) => context.ReportDiagnostic(error));

        context.RegisterSourceOutput(provider
            .Select((r, _) => r.Value!)
            .Where(x => x is not null),
            (context, info) =>
            {
                var code = info.GeneratedCode;
                context.AddSource($"ATG_{info.MemberId}.g.cs", code);
            });
    }
}

using Generators.AttributeTemplates;
using Generators.AttributeTemplates.Application;
using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;
using Microsoft.CodeAnalysis;

namespace Generators;

[Generator]
public class AttributeTemplateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.AddCommonSource();

        var templateProvider = context.CreateTemplateSyntaxProvider();
        var memberProvider = context.CreateTargetSyntaxProvider();
        context.RegisterSourceOutput(templateProvider, memberProvider);
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal static class TemplateProvider
{
    public static IncrementalValuesProvider<TemplateDefinition> CreateTemplateSyntaxProvider(this IncrementalGeneratorInitializationContext context)
    {
        var templates = context.SyntaxProvider.CreateTemplateSyntaxProvider();

        context.RegisterSourceOutput(templates
            .Select((t, _) => t.Error!)
            .Where(e => e is not null),
            (c, e) => c.ReportDiagnostic(e));

        return templates
            .Select((t, _) => t.Value!)
            .Where(x => x is not null);
    }

    public static IncrementalValuesProvider<Result<TemplateDefinition>> CreateTemplateSyntaxProvider(this SyntaxValueProvider syntaxProvider)
        => syntaxProvider.CreateSyntaxProvider(
            IsAttributeDeclaration,
            GetTemplateInfo
            )
        .Where(t => !t.IsNull)!;

    private static bool IsAttributeDeclaration(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax c
            && c.Identifier.ValueText.EndsWith("Attribute");
    }

    private static Result<TemplateDefinition> GetTemplateInfo(GeneratorSyntaxContext context, CancellationToken token)
        => TemplateDefinition.Create(context.SemanticModel, (ClassDeclarationSyntax)context.Node);

}

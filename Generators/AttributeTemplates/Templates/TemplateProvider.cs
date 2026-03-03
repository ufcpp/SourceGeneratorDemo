using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal static class TemplateProvider
{
    public static IncrementalValuesProvider<Result<TemplateDefinition>> CreateTemplateSyntaxProvider(this IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateTemplateSyntaxProvider();

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

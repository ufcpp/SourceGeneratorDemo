using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal static class TemplateProvider
{
    public static IncrementalValuesProvider<TemplateInfo> CreateTemplateSyntaxProvider(this IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateTemplateSyntaxProvider();

    public static IncrementalValuesProvider<TemplateInfo> CreateTemplateSyntaxProvider(this SyntaxValueProvider syntaxProvider)
        => syntaxProvider.CreateSyntaxProvider(
            IsAttributeDeclaration,
            GetTemplateInfo
            )
        .Where(t => t != null)!;

    private static bool IsAttributeDeclaration(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax c
            && c.Identifier.ValueText.EndsWith("Attribute");
    }

    private static TemplateInfo? GetTemplateInfo(GeneratorSyntaxContext context, CancellationToken token)
        => TemplateInfo.Create(context.SemanticModel, (ClassDeclarationSyntax)context.Node, token);

}

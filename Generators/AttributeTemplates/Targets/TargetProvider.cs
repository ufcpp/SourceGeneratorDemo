using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

internal static class TargetProvider
{
    public static IncrementalValuesProvider<TemporaryTemplateTarget> CreateTargetSyntaxProvider(this IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateTargetSyntaxProvider();

    public static IncrementalValuesProvider<TemporaryTemplateTarget> CreateTargetSyntaxProvider(this SyntaxValueProvider syntaxProvider)
        => syntaxProvider.CreateSyntaxProvider(
            IsTemplateMember,
            static (context, _) => TemporaryTemplateTarget.Create(context)
            )
        .Where(t => t != null)!;

    private static bool IsTemplateMember(SyntaxNode node, CancellationToken token)
    {
        return node is MemberDeclarationSyntax d
            && d.Modifiers.Any(SyntaxKind.PartialKeyword)
            && d.AttributeLists.Any()
            && (d is PropertyDeclarationSyntax or MethodDeclarationSyntax or TypeDeclarationSyntax);
    }
}

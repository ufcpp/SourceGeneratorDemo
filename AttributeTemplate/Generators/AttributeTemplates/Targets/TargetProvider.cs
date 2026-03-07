using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

internal static class TargetProvider
{
    public static IncrementalValuesProvider<TemporaryTemplateTarget> CreateTargetSyntaxProvider(this IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.CreateTargetSyntaxProvider();

        context.RegisterSourceOutput(targets
            .Select((r, _) => r.Error!)
            .Where(e => e is not null),
            (c, e) => c.ReportDiagnostic(e));

        return targets
            .Select((r, _) => r.Value!)
            .Where(x => x is not null);
    }

    public static IncrementalValuesProvider<Result<TemporaryTemplateTarget>> CreateTargetSyntaxProvider(this SyntaxValueProvider syntaxProvider)
        => syntaxProvider.CreateSyntaxProvider(
            IsTemplateMember,
            static (context, _) => TemporaryTemplateTarget.Create(context)
            )
        .Where(t => !t.IsNull);

    private static bool IsTemplateMember(SyntaxNode node, CancellationToken token)
    {
        return node is MemberDeclarationSyntax d
            && d.Modifiers.Any(SyntaxKind.PartialKeyword)
            && d.AttributeLists.Any()
            && (d is PropertyDeclarationSyntax or MethodDeclarationSyntax or TypeDeclarationSyntax);
    }
}

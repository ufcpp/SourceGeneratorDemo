using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

internal static class TargetProvider
{
    public static IncrementalValuesProvider<TemplateTarget> CreateTargetSyntaxProvider(this IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateTargetSyntaxProvider();

    public static IncrementalValuesProvider<TemplateTarget> CreateTargetSyntaxProvider(this SyntaxValueProvider syntaxProvider)
        => syntaxProvider.CreateSyntaxProvider(
            IsTemplateMember,
            static (context, _) => TemplateTarget.Create(context)
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

internal record TemplateTarget(string Attribute, ArgumentList Args, MemberDeclarationSyntax Member)
{
    public static TemplateTarget? Create(GeneratorSyntaxContext context)
    {
        var d = (MemberDeclarationSyntax)context.Node;
        var semantics = context.SemanticModel;

        if (GetAttribute(d, semantics) is not { } t) return null;

        return new(t.Name, t.Args, d);
    }

    private static (string Name, ArgumentList Args)? GetAttribute(MemberDeclarationSyntax d, SemanticModel semantics)
    {
        foreach (var list in d.AttributeLists)
        {
            foreach (var a in list.Attributes)
            {
                var at = semantics.GetTypeInfo(a);

                if (at.Type is not { } t) continue;

                var bat = at.Type?.BaseType;
                if (bat is { ContainingNamespace.Name: "AttributeTemplateGenerator", Name: "TemplateAttribute" })
                {
                    return (t.Name, new(semantics, a.ArgumentList));
                }
            }
        }

        return null;
    }
}


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators;

[Generator]
public class AttributeTemplateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "TemplateAttribute.g.cs",
            /* lang=C# */
            """
            namespace AttributeTemplateGenerator;

            [System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            internal class TemplateAttribute : Attribute;
            """));

        var templateProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsAttributeDeclaration,
            GetTemplateInfo)
            .Where(t => t != null);

        var memberProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsTemplateMember,
            GetMemberInfo)
            .Where(t => t != null);

        var provider = memberProvider.Combine(templateProvider.Collect())
            .Select((t, _) =>
            {
                if (t.Right.FirstOrDefault(x => x!.Attribute.Equals(t.Left!.Attribute)) is { } f)
                {
                    return new GenerationInfo(t.Left!.Member, t.Left.Args, f.Params, f.Parent, f.Body);
                }
                return null;
            })
            .Where(x => x != null);

        context.RegisterSourceOutput(provider, Execute!);
    }

    private bool IsAttributeDeclaration(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax c
            && c.Identifier.ValueText.EndsWith("Attribute");
    }

    private TemplateInfo? GetTemplateInfo(GeneratorSyntaxContext context, CancellationToken token)
    {
        var d = (ClassDeclarationSyntax)context.Node;

        if (d.BaseList is not { } b) return null;

        var semantics = context.SemanticModel;

        var any = false;
        foreach (var t in b.Types)
        {
            var ti = semantics.GetTypeInfo(t.Type);
            if (ti.Type is { ContainingNamespace.Name: "AttributeTemplateGenerator", Name: "TemplateAttribute" })
            {
                any = true;
                break;
            }
        }
        if (!any) return null;

        InterpolatedStringExpressionSyntax? body = null, parent = null;
        foreach (var m in d.Members)
        {
            if (m is not PropertyDeclarationSyntax p) continue;
            if (p.ExpressionBody?.Expression is not InterpolatedStringExpressionSyntax i) continue;
            if (p.Identifier.ValueText == "Body") body = i;
            else if (p.Identifier.ValueText == "Parent") parent = i;
            //todo: property setter
        }

        return new(d.Identifier.ValueText, d.ParameterList, parent, body);
    }

    private bool IsTemplateMember(SyntaxNode node, CancellationToken token)
    {
        return node is MemberDeclarationSyntax d
            && d.Modifiers.Any(SyntaxKind.PartialKeyword)
            && d.AttributeLists.Any()
            && (d is PropertyDeclarationSyntax or MethodDeclarationSyntax or TypeDeclarationSyntax);
    }

    private MemberInfo? GetMemberInfo(GeneratorSyntaxContext context, CancellationToken token)
    {
        var d = (MemberDeclarationSyntax)context.Node;
        var semantics = context.SemanticModel;

        if (GetAttribute(d, semantics) is not { } t) return null;

        return new (t.Name, t.Args, d);
    }

    private static (string Name, AttributeArgumentListSyntax? Args)? GetAttribute(MemberDeclarationSyntax d, SemanticModel semantics)
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
                    return (t.Name, a.ArgumentList);
                }
            }
        }

        return null;
    }

    private void Execute(SourceProductionContext context, GenerationInfo source)
    {
    }

    private record TemplateInfo(string Attribute, ParameterListSyntax? Params, InterpolatedStringExpressionSyntax? Parent, InterpolatedStringExpressionSyntax? Body);
    private record MemberInfo(string Attribute, AttributeArgumentListSyntax? Args, MemberDeclarationSyntax Member);
    private record GenerationInfo(MemberDeclarationSyntax Member, AttributeArgumentListSyntax? Args, ParameterListSyntax? Params, InterpolatedStringExpressionSyntax? Parent, InterpolatedStringExpressionSyntax? Body);
}

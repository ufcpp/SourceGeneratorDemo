using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

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
                if (bat.IsTemplateAttribute())
                {
                    return (t.Name, new(semantics, a.ArgumentList));
                }
            }
        }

        return null;
    }
}

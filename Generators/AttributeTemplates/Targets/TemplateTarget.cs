using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

internal record TemplateTarget(string MemberId, string AttributeId, ArgumentList Args, Member[] Members)
{
    public static TemplateTarget? Create(GeneratorSyntaxContext context)
    {
        var d = (MemberDeclarationSyntax)context.Node;
        var semantics = context.SemanticModel;

        if (GetAttribute(d, semantics) is not { } t) return null;

        return new(semantics.GetUniqueId(d), t.Signature, t.Args, Member.Hierarchy(d));
    }

    private static (string Signature, ArgumentList Args)? GetAttribute(MemberDeclarationSyntax d, SemanticModel semantics)
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
                    return (t.GetUniqueId(), new(semantics, a.ArgumentList));
                }
            }
        }

        return null;
    }
}

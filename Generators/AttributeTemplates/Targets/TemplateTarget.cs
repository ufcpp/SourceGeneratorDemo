using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

internal record TemplateTarget(MemberHierarchy Member, ArgumentList[] Args)
{
    public string MemberId => Member.Id;

    /// <summary>
    /// Groups members by <see cref="MemberId"/>
    /// <see cref="Args"/> are concatenated for members with the same <see cref="MemberId"/>.
    /// </summary>
    public static TemplateTarget[] Group(IEnumerable<TemporaryTemplateTarget> targets)
    {
        return [.. targets
            .GroupBy(t => t.MemberId)
            .Select(g => new TemplateTarget(new MemberHierarchy(g.Key, g.First().Member), [.. g.SelectMany(t => t.Args)]))
            ];
    }
}

/// <summary>
/// A temporary data for deferring <see cref="MemberItem.Hierarchy"/> until <see cref="TemplateTarget.Group"/> is called.
/// </summary>
internal record TemporaryTemplateTarget(string MemberId, MemberDeclarationSyntax Member, ArgumentList[] Args)
{
    public static TemporaryTemplateTarget? Create(GeneratorSyntaxContext context)
    {
        var d = (MemberDeclarationSyntax)context.Node;
        var semantics = context.SemanticModel;

        if (GetAttribute(d, semantics) is not { } args) return null;

        return new(semantics.GetUniqueId(d), d, [.. args]);
    }

    private static List<ArgumentList>? GetAttribute(MemberDeclarationSyntax d, SemanticModel semantics)
    {
        List<ArgumentList>? results = null;

        foreach (var list in d.AttributeLists)
        {
            foreach (var a in list.Attributes)
            {
                if (ArgumentList.Create(semantics, a) is { } args) (results ??= []).Add(args);
            }
        }

        return results;
    }
}

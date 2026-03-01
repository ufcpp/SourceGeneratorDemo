using Generators.AttributeTemplates.Targets;
using static Generators.AttributeTemplates.Targets.Member;
using Generators.AttributeTemplates.Templates;
using System.Text;

namespace Generators.AttributeTemplates.Application;

internal readonly struct TemplateHierarchy
{
    private readonly (Member Node, MemberTemplate? Template)[] _templates;

    public TemplateHierarchy(IEnumerable<MemberTemplate> templates, Member[] members)
    {
        var items = new (Member Node, MemberTemplate? Template)[members.Length];

        for (var i = 0; i < members.Length; ++i)
        {
            items[i].Node = members[i];
        }

        foreach (var t in templates)
        {
            var l = t.Level;
            var i = l < 0 ? members.Length + l : l;
            items[i].Template = t; // check if already exists at the same level? error? ignore first or last?
        }

        _templates = items;
    }

    public int Count => _templates.Length;
    public IEnumerator<(Member Node, MemberTemplate? Template)> GetEnumerator() => _templates.AsEnumerable().Reverse().GetEnumerator();

    public Member GetNode(int level)
    {
        var i = level < 0 ? _templates.Length + level : level;
        return _templates[i].Node;
    }

    public Parameter[]? MethodParameters => _templates[0].Node is Method m ? m.Parameters : default;

    public bool TryGetIntrinsicValue(string id, int? alignment, int level, out string? value)
    {
        // todo: error if null or out of range?

        if (id == Intrinsic.Type)
        {
            if (alignment is { } a) // {Type,a}
            {
                // combination with level? (primary constructor parameters?)
                // {Parent(Type),a}

                value = MethodParameters?[a].Type;
            }
            else // {Type}, {Parent(Type)}, {Up(level, Type)}, etc.
            {
                value = (GetNode(level) as TypedMember)?.Type;
            }
            return true;
        }
        else if (id == Intrinsic.Name)
        {
            if (alignment is { } a) // {Name,a}
            {
                value = MethodParameters?[a].Name;
            }
            else // {Name}, {Parent(Name)}, {Up(level, Name)}, etc.
            {
                value = (GetNode(level) as NamedMember)?.Name;
            }
            return true;
        }

        value = null;
        return false;

    }
}

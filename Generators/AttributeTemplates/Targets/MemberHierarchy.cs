using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Generators.AttributeTemplates.Targets.MemberItem;

namespace Generators.AttributeTemplates.Targets;

internal class MemberHierarchy(string id, MemberDeclarationSyntax member) : IEnumerable<MemberItem>
{
    public string Id { get; } = id;
    public Location Location { get; } = member.GetLocation();
    private readonly MemberItem[] _items = CreateItems(member);

    private static MemberItem[] CreateItems(MemberDeclarationSyntax member)
    {
        var list = new List<MemberItem>();

        // bottom (Member) to top (CompilationUnit)
        for (SyntaxNode? m = member; m != null; m = m.Parent)
        {
            list.Add(From(m));
        }
        return [.. list];
    }

    public int Count => _items.Length;
    public IEnumerator<MemberItem> GetEnumerator() => _items.AsEnumerable().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public MemberItem GetNode(Index level)
    {
        int i = GetIndex(level);
        return _items[i];
    }

    public int GetIndex(Index level)
    {
        // items[0] = member itself (bottom), items[count-1] = global (top)
        // ^0 means "one past end (top-level member)" which we map to global (count-1)
        return level.IsFromEnd 
            ? _items.Length - 1 - level.Value
            : level.Value;
    }

    public bool TryGetIntrinsicValue(string id, Index level, int? parameterIndex, out string? value)
    {
        var actualLevel = GetIndex(level);

        if (actualLevel < 0 || actualLevel >= _items.Length)
        {
            value = null;
            return false;
        }

        var member = _items[actualLevel];

        if (id == Intrinsic.Type)
        {
            if (parameterIndex is { } a)
            {
                value = (member as IHasParameters)?.Parameters?[a].Type;
            }
            else
            {
                value = (member as TypedMember)?.Type;
            }
            return true;
        }
        else if (id == Intrinsic.Name)
        {
            if (parameterIndex is { } a)
            {
                value = (member as IHasParameters)?.Parameters?[a].Name;
            }
            else
            {
                value = (member as NamedMember)?.Name;
            }
            return true;
        }

        value = null;
        return false;

    }
}

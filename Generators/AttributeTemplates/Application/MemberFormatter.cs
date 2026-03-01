using System.Text;
using M = Generators.AttributeTemplates.Targets.MemberItem;

namespace Generators.AttributeTemplates.Application;

internal class MemberFormatter
{
    public static void AppendDeclarationLine(StringBuilder s, M node)
    {
        void appendModifiers(string[] modifiers)
        {
            foreach (var token in modifiers)
            {
                s.Append(token);
                s.Append(' ');
            }
        }

        if (node is M.Root)
        {
        }
        else if (node is M.Namespace n)
        {
            s.Append($$"""
                        namespace {{n.Name}} {

                        """);
        }
        else if (node is M.TypeDeclaration type)
        {
            appendModifiers(type.Modifiers);

            s.Append($$"""
                        {{type.Keyword}} {{type.Name}} {

                        """);
        }
        else if (node is M.Property p)
        {
            appendModifiers(p.Modifiers);
            s.Append($$"""
                        {{p.Type}} {{p.Name}} {

                        """);
        }
        else if (node is M.Method m)
        {
            appendModifiers(m.Modifiers);
            s.Append($$"""
                        {{m.Type}} {{m.Name}}(
                        """);

            var first = true;
            foreach (var mp in m.Parameters)
            {
                if (first) first = false;
                else s.Append(", ");
                s.Append($"{mp.Type} {mp.Name}");
            }

            s.Append("""
                        ) {

                        """);
        }
        else { } // todo: error? never reachable?
    }

}

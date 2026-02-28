using Generators.AttributeTemplates.Targets;
using static Generators.AttributeTemplates.Targets.Member;
using System.Text;

namespace Generators.AttributeTemplates.Application;

internal record GenerationInfo(string Attribute, TemplateHierarchy Templates, ParameterMap Params)
{
    public string Generate()
    {
        var s = new StringBuilder();

        foreach (var (node, mt) in Templates)
        {
            GenerateMemberDeclaration(s, node);

            // apply member template
            if (mt is null) continue;

            GenerateContent(s, mt);
            newLine();
        }

        s.Append('}', Templates.Count - 1);
        newLine();

        void newLine()
        {
            // source-code-dependent LF.
            s.Append(@"
");
        }

        return s.ToString();
    }

    private static void GenerateMemberDeclaration(StringBuilder s, Member node)
    {
        void appendModifiers(string[] modifiers)
        {
            foreach (var token in modifiers)
            {
                s.Append(token);
                s.Append(' ');
            }
        }

        if (node is Root)
        {
        }
        else if (node is Namespace n)
        {
            s.Append($$"""
                        namespace {{n.Name}} {

                        """);
        }
        else if (node is TypeDeclaration type)
        {
            appendModifiers(type.Modifiers);

            s.Append($$"""
                        {{type.Keyword}} {{type.Name}} {

                        """);
        }
        else if (node is Property p)
        {
            appendModifiers(p.Modifiers);
            s.Append($$"""
                        {{p.Type}} {{p.Name}} {

                        """);
        }
        else if (node is Method m)
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

    private void GenerateContent(StringBuilder s, Templates.MemberTemplate mt)
    {
        void appendFormat(object? value, int? alignment = null, string? format = null)
        {
            var formatString = (alignment, format) switch
            {
                ({ } x, null) => $"{{0:{x}}}",
                (null, { } x) => $"{{0,{x}}}",
                ({ } x, { } y) => $"{{0,{x}:{y}}}",
                _ => "{0}",
            };
            s.AppendFormat(null, formatString, value); // todo: take culture
        }

        if (mt.Constant is { } constValue)
        {
            s.Append(constValue);
        }
        else if (mt.Identifier is { } id)
        {
            var value = Params[id];
            appendFormat(value);
        }
        else if (mt.Interpolation is { } i)
        {
            foreach (var c in i.Contents)
            {
                if (c.Text is { } text)
                {
                    s.Append(text);
                }
                else if (c.ConstantValue is { } c1)
                {
                    appendFormat(c1, c.Alignment, c.Format);
                }
                else if (c.Identifier is { } id1)
                {
                    if (Templates.TryGetIntrinsicValue(id1, c.Alignment, c.Level, out var iv))
                    {
                        s.Append(iv);
                    }
                    else
                    {
                        // must be distinguish "not found" and "found null"
                        var value = Params[id1];
                        appendFormat(value, c.Alignment, c.Format);
                    }
                }
            }
        }
        else { }
    }
}

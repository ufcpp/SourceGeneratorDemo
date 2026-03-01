using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;
using System.Text;
using M = Generators.AttributeTemplates.Targets.MemberItem;
using E = Generators.AttributeTemplates.Templates.MemberExpression;

namespace Generators.AttributeTemplates.Application;

internal record struct ContentCode(int Level, string Code);

internal static class Generator
{
    public static string Generate(TemplateTarget target, Dictionary<string, TemplateDefinition> templates)
    {
        var s = new StringBuilder();

        var contents = Apply(target, templates);
        foreach (var (node, code) in target.Member.Zip(contents, (x, y) => (x, y)).Reverse())
        {
            GenerateMemberDeclaration(s, node);
            s.Append(code);
        }

        s.Append('}', target.Member.Count - 1);
        // source-code-dependent LF.
        s.Append(@"
");

        return s.ToString();
    }

    private static void GenerateMemberDeclaration(StringBuilder s, M node)
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

    private static string?[] Apply(TemplateTarget target, Dictionary<string, TemplateDefinition> templates)
    {
        string?[] contents = new string[target.Member.Count];

        foreach (var args in target.Args)
        {
            if (templates.TryGetValue(args.AttributeId, out var template))
            {
                var map = new ParameterMap(template.Params, args);

                foreach (var mt in template.Templates)
                {
                    var c = Evaluate(mt.Expression, target.Member, map);
                    var i = target.Member.GetIndex(mt.Level);
                    contents[i] += c + @"
";
                }
            }
        }

        return contents;
    }

    private static object? Evaluate(E ex, MemberHierarchy member, ParameterMap map)
    {
        if (ex is E.Constant cv)
        {
            return cv.Value;
        }
        else if (ex is E.Parameter { Name: var id })
        {
            return map[id];
        }
        else if (ex is E.IntrinsicExpression ie)
        {
            if (member.TryGetIntrinsicValue(ie.Kind, ie.Level, ie.ParameterIndex, out var iv))
                return iv;

            return null; // else error?
        }
        else if (ex is E.InterpolatedString i)
        {
            var s = new StringBuilder();
            foreach (var c in i.Contents)
            {
                if (c is E.InterpolatedString.StringText text)
                {
                    s.Append(text.Text);
                }
                else if (c is E.InterpolatedString.Interpolation interpolation)
                {
                    var value = Evaluate(interpolation.Expression, member, map);

                    var formatString = (interpolation.Alignment, interpolation.Format) switch
                    {
                        ({ } x, null) => $"{{0,{x}}}",
                        (null, { } x) => $"{{0:{x}}}",
                        ({ } x, { } y) => $"{{0,{x}:{y}}}",
                        _ => "{0}",
                    };

                    s.AppendFormat(map.Culture, formatString, value);
                }
            }
            return s.ToString();
        }
        else { return null; } // error? unreachable?
    }
}

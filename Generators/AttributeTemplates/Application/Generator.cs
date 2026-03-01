using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;
using System.Text;

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
            MemberFormatter.AppendDeclarationLine(s, node);
            s.Append(code);
        }

        s.Append('}', target.Member.Count - 1);
        // source-code-dependent LF.
        s.Append(@"
");

        return s.ToString();
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
                    var c = ExpressionEvaluator.Evaluate(mt.Expression, target.Member, map);
                    var i = target.Member.GetIndex(mt.Level);
                    contents[i] += c + @"
";
                }
            }
        }

        return contents;
    }
}

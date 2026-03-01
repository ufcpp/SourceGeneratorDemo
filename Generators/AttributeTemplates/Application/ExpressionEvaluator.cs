using Generators.AttributeTemplates.Targets;
using System.Text;
using E = Generators.AttributeTemplates.Templates.MemberExpression;

namespace Generators.AttributeTemplates.Application;

internal static class ExpressionEvaluator
{
    public static object? Evaluate(E ex, MemberHierarchy member, ParameterMap map)
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Generators.AttributeTemplates.Templates;

internal abstract class MemberExpression
{
    public static MemberExpression Create(SemanticModel semantics, ExpressionSyntax e, ParameterList parameters)
    {
        if (e is LiteralExpressionSyntax literal)
        {
            return new Constant { Value = Variant.FromObject(literal.Token.Value) };
        }
        else if (e is IdentifierNameSyntax id)
        {
            var name = id.Identifier.ValueText;
            if (name is Intrinsic.Name or Intrinsic.Type)
            {
                return new IntrinsicExpression { Level = 0, Kind = name };
            }

            var v = semantics.GetConstantValue(id);
            if (v.HasValue) return new Constant { Value = Variant.FromObject(v.Value) };

            return new Parameter { Name = name };
        }
        else if (e is InvocationExpressionSyntax inv)
        {
            var (level, parameterIndex, kind) = Intrinsic.GetIntrinsicValue(semantics, inv);
            return new IntrinsicExpression { Level = level, ParameterIndex = parameterIndex, Kind = kind };
        }
        else if (e is MemberAccessExpressionSyntax ma)
        {
            // currently only supports constant values in member access.
            var v = semantics.GetConstantValue(ma);
            if (v.HasValue) return new Constant { Value = Variant.FromObject(v.Value) };

            //todo: error?
        }
        else if (e is InterpolatedStringExpressionSyntax interpolatedString)
        {
            return InterpolatedString.Create(semantics, interpolatedString, parameters);
        }

        return null!; // todo: error
    }

    public abstract Variant Evaluate(IExpressionEvaluationContext context);

    public class Constant : MemberExpression
    {
        public required Variant Value { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            return Value;
        }
    }

    public class Parameter : MemberExpression
    {
        public required string Name { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            return Variant.FromObject(context[Name]);
            //todo: use TryFromObject and report an error
        }
    }

    public class IntrinsicExpression : MemberExpression
    {
        public required int Level { get; init; }
        public int? ParameterIndex { get; init; }
        public required string Kind { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            if (context.TryGetIntrinsicValue(Kind, Level, ParameterIndex, out var iv))
                return Variant.FromObject(iv); //todo: TryFromObject

            return default; // else error?
        }
    }

    public class InterpolatedString : MemberExpression
    {
        public required Content[] Contents { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            var s = new StringBuilder();
            foreach (var c in Contents)
            {
                if (c is StringText text)
                {
                    s.Append(text.Text);
                }
                else if (c is Interpolation interpolation)
                {
                    var value = interpolation.Expression.Evaluate(context);

                    var formatString = (interpolation.Alignment, interpolation.Format) switch
                    {
                        ({ } x, null) => $"{{0,{x}}}",
                        (null, { } x) => $"{{0:{x}}}",
                        ({ } x, { } y) => $"{{0,{x}:{y}}}",
                        _ => "{0}",
                    };

                    s.AppendFormat(context.Culture, formatString, value);
                }
            }
            return new(s.ToString());
        }


        public static InterpolatedString Create(SemanticModel semantics, InterpolatedStringExpressionSyntax i, ParameterList parameters)
        {
            return new InterpolatedString
            {
                Contents = [.. i.Contents.Select(c => Create(semantics, c, parameters))],
            };
        }

        public static Content Create(SemanticModel semantics, InterpolatedStringContentSyntax content, ParameterList parameters)
        {
            if (content is InterpolatedStringTextSyntax text)
            {
                return new StringText { Text = text.TextToken.ValueText };
            }
            else if (content is InterpolationSyntax i)
            {
                int? align = i.AlignmentClause is { } a && semantics.GetConstantValue(a.Value) is { HasValue: true, Value: int x } ? x : null;
                var fmt = i.FormatClause?.FormatStringToken.ValueText;

                var me = MemberExpression.Create(semantics, i.Expression, parameters);

                return new Interpolation
                {
                    Expression = me, // should error if me is not Name nor Type?
                    Alignment = align,
                    Format = fmt
                };
            }
            return null!; // error? unreachable?
        }


        public class Content;

        public class StringText : Content
        {
            public required string Text { get; init; }
        }

        public class Interpolation : Content
        {
            public required MemberExpression Expression { get; init; }
            public required int? Alignment { get; init; }
            public required string? Format { get; init; }
        }
    }
}

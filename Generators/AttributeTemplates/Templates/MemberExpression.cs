using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal class MemberExpression
{
    public static MemberExpression Create(SemanticModel semantics, ExpressionSyntax e, ParameterList parameters)
    {
        if (e is LiteralExpressionSyntax literal)
        {
            return new Constant { Value = literal.Token.Value };
        }
        else if (e is IdentifierNameSyntax id)
        {
            var name = id.Identifier.ValueText;
            if (name is Intrinsic.Name or Intrinsic.Type)
            {
                return new IntrinsicExpression { Level = 0, Kind = name };
            }

            var v = semantics.GetConstantValue(id);
            if (v.HasValue) return new Constant { Value = v.Value };

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
            if (v.HasValue) return new Constant { Value = v.Value };

            //todo: error?
        }
        else if (e is InterpolatedStringExpressionSyntax interpolatedString)
        {
            return InterpolatedString.Create(semantics, interpolatedString, parameters);
        }

        return null!; // todo: error
    }

    public class Parameter : MemberExpression
    {
        public required string Name { get; init; }
    }

    public class Constant : MemberExpression
    {
        public required object? Value { get; init; }
    }

    public class IntrinsicExpression : MemberExpression
    {
        public required int Level { get; init; }
        public int? ParameterIndex { get; init; }
        public required string Kind { get; init; }
    }

    public class InterpolatedString : MemberExpression
    {
        public required Content[] Contents { get; init; }

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

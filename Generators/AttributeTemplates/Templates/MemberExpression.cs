using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Generators.AttributeTemplates.Templates;

internal enum UnaryOperator
{
    Plus,
    Minus,
    Not, // both ! and ~
}

internal enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    And, // both & and &&, logical and bitwise
    Or,
}

internal abstract class MemberExpression
{
    public static MemberExpression Create(SemanticModel semantics, ExpressionSyntax e, ParameterList parameters)
    {
        var (level, parameterIndex, kind) = Intrinsic.GetIntrinsicValue(semantics, e);

        if (kind is not null)
        {
            return new IntrinsicExpression { Level = level, ParameterIndex = parameterIndex, Kind = kind };
        }

        if (e is LiteralExpressionSyntax literal)
        {
            return new Constant { Value = Variant.FromObject(literal.Token.Value) };
        }
        else if (e is IdentifierNameSyntax id)
        {
            var name = id.Identifier.ValueText;
            var v = semantics.GetConstantValue(id);
            if (v.HasValue) return new Constant { Value = Variant.FromObject(v.Value) };

            return new Parameter { Name = name };
        }
        else if (e is MemberAccessExpressionSyntax ma)
        {
            // currently only supports constant values in member access.
            var v = semantics.GetConstantValue(ma);
            if (v.HasValue) return new Constant { Value = Variant.FromObject(v.Value) };
        }
        else if (e is InterpolatedStringExpressionSyntax interpolatedString)
        {
            return InterpolatedString.Create(semantics, interpolatedString, parameters);
        }
        else if (e is CastExpressionSyntax cast)
        {
            var innerExpr = Create(semantics, cast.Expression, parameters);
            var targetType = Variant.GetLiteralKind(cast.Type.ToString());
            return new CastExpression { Expression = innerExpr, TargetType = targetType };
        }
        else if (e is PrefixUnaryExpressionSyntax unary)
        {
            var operand = Create(semantics, unary.Operand, parameters);
            var op = unary.Kind() switch
            {
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnaryPlusExpression => UnaryOperator.Plus,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnaryMinusExpression => UnaryOperator.Minus,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.BitwiseNotExpression => UnaryOperator.Not,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalNotExpression => UnaryOperator.Not,
                var k => throw AttributeTemplateException.UnsupportedExpression(k, unary.GetLocation()),
            };
            return new UnaryExpression { Operand = operand, Operator = op };
        }
        else if (e is BinaryExpressionSyntax binary)
        {
            var left = Create(semantics, binary.Left, parameters);
            var right = Create(semantics, binary.Right, parameters);
            var op = binary.Kind() switch
            {
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddExpression => BinaryOperator.Add,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SubtractExpression => BinaryOperator.Subtract,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiplyExpression => BinaryOperator.Multiply,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.DivideExpression => BinaryOperator.Divide,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.ModuloExpression => BinaryOperator.Modulo,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.BitwiseAndExpression => BinaryOperator.And,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalAndExpression => BinaryOperator.And,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.BitwiseOrExpression => BinaryOperator.Or,
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalOrExpression => BinaryOperator.Or,
                var k => throw AttributeTemplateException.UnsupportedExpression(k, binary.GetLocation()),
            };
            return new BinaryExpression { Left = left, Right = right, Operator = op };
        }
        else if (e is ParenthesizedExpressionSyntax parenthesized)
        {
            return Create(semantics, parenthesized.Expression, parameters);
        }
        else if (e is ConditionalExpressionSyntax conditional)
        {
            var condition = Create(semantics, conditional.Condition, parameters);
            var whenTrue = Create(semantics, conditional.WhenTrue, parameters);
            var whenFalse = Create(semantics, conditional.WhenFalse, parameters);
            return new ConditionalExpression { Condition = condition, WhenTrue = whenTrue, WhenFalse = whenFalse };
        }

        throw AttributeTemplateException.UnsupportedExpression(e.Kind(), e.GetLocation());
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

    public class CastExpression : MemberExpression
    {
        public required MemberExpression Expression { get; init; }
        public required LiteralKind TargetType { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            var value = Expression.Evaluate(context);
            return value.Cast(TargetType);
        }
    }

    public class UnaryExpression : MemberExpression
    {
        public required MemberExpression Operand { get; init; }
        public required UnaryOperator Operator { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            var operand = Operand.Evaluate(context);
            return Operator switch
            {
                UnaryOperator.Plus => +operand,
                UnaryOperator.Minus => -operand,
                UnaryOperator.Not => ~operand,
                _ => throw new InvalidOperationException($"Unknown unary operator: {Operator}")
            };
        }
    }

    public class BinaryExpression : MemberExpression
    {
        public required MemberExpression Left { get; init; }
        public required MemberExpression Right { get; init; }
        public required BinaryOperator Operator { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
                var left = Left.Evaluate(context);
                    var right = Right.Evaluate(context);

            if (left.Kind == LiteralKind.String) return new(left._string + right.ToString(null, context.Culture));
            if (right.Kind == LiteralKind.String) return new(left.ToString(null, context.Culture) + right._string);

            return Operator switch
            {
                BinaryOperator.Add => left + right,
                BinaryOperator.Subtract => left - right,
                BinaryOperator.Multiply => left * right,
                BinaryOperator.Divide => left / right,
                BinaryOperator.Modulo => left % right,
                BinaryOperator.And => left & right,
                BinaryOperator.Or => left | right,
                _ => throw new InvalidOperationException($"Unknown binary operator: {Operator}")
            };
        }
    }

    public class ConditionalExpression : MemberExpression
    {
        public required MemberExpression Condition { get; init; }
        public required MemberExpression WhenTrue { get; init; }
        public required MemberExpression WhenFalse { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            var condition = Condition.Evaluate(context);
            return (bool)condition ? WhenTrue.Evaluate(context) : WhenFalse.Evaluate(context);
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

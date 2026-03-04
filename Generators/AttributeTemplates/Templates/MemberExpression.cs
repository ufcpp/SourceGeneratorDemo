using Microsoft.CodeAnalysis;
using System.Text;

namespace Generators.AttributeTemplates.Templates;

internal abstract class MemberExpression
{
    public required Location Location { get; init; }

    public abstract Variant Evaluate(IExpressionEvaluationContext context);

    public enum UnaryOperator
    {
        Plus,
        Minus,
        Not, // both ! and ~
    }

    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        And, // both & and &&, logical and bitwise
        Or,
    }

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
            return context[Name];
        }
    }

    public class IntrinsicExpression : MemberExpression
    {
        public required Index Level { get; init; }
        public int? ParameterIndex { get; init; }
        public required string Kind { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            var error = context.TryGetIntrinsicValue(Kind, Level, ParameterIndex, out var iv);

            if (error != IntrinsicError.None)
            {
                throw error switch
                {
                    IntrinsicError.LevelOutOfRange => AttributeTemplateException.LevelOutOfRange(Location),
                    IntrinsicError.MemberHasNoParameters => AttributeTemplateException.MemberHasNoParameters(Location),
                    IntrinsicError.ParameterIndexOutOfRange => AttributeTemplateException.ParameterIndexOutOfRange(Location),
                    _ => AttributeTemplateException.Unreachable(Location),
                };
            }

            return Variant.FromObject(iv); //todo: TryFromObject
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

    public class ElementAccessExpression : MemberExpression
    {
        public required MemberExpression Array { get; init; }
        public required MemberExpression Index { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            var array = Array.Evaluate(context);
            var index = Index.Evaluate(context);

            // String indexing
            if (array.Kind == LiteralKind.String)
            {
                if (array._string is not { } str) // unreachable
                {
                    throw AttributeTemplateException.Unreachable(Location);
                }

                var idx = (int)index;
                if (idx < 0 || idx >= str.Length)
                {
                    throw AttributeTemplateException.EvaluationOutOfRange(Location);
                }

                return new Variant(str[idx]);
            }

            // Array indexing
            if (array.Kind != LiteralKind.Array) // unreachable
            {
                throw AttributeTemplateException.Unreachable(Location);
            }

            if (array._array is not { } arr)
            {
                throw new InvalidOperationException("Array is null");
            }

            var index2 = (int)index;
            if (index2 < 0 || index2 >= arr.Length)
            {
                throw AttributeTemplateException.EvaluationOutOfRange(Location);
            }

            return arr[index2];
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

    public class QueryExpression : MemberExpression
    {
        public required string RangeVariable { get; init; }
        public required MemberExpression Source { get; init; }
        public required MemberExpression Selector { get; init; }

        public override Variant Evaluate(IExpressionEvaluationContext context)
        {
            var source = Source.Evaluate(context);

            if (source.Kind != LiteralKind.Array)
            {
                throw AttributeTemplateException.Unreachable(Location);
            }

            if (source._array is not { } sourceArray)
            {
                return new Variant(Array.Empty<Variant>());
            }

            var results = new Variant[sourceArray.Length];
            for (int i = 0; i < sourceArray.Length; i++)
            {
                var itemContext = new LocalSymbolContext(context, (RangeVariable, sourceArray[i]));
                results[i] = Selector.Evaluate(itemContext);
            }

            return new Variant(results);
        }
    }
}

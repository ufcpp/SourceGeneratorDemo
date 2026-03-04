using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Generators.AttributeTemplates.Templates.MemberExpression;

namespace Generators.AttributeTemplates.Templates;

internal static class ExpressionWalker
{
    public static MemberExpression Create(SemanticModel semantics, ExpressionSyntax e, ParameterList parameters)
    {
        var (level, parameterIndex, kind) = Intrinsic.GetIntrinsicValue(semantics, e);

        if (kind is not null)
        {
            return new IntrinsicExpression { Level = level, ParameterIndex = parameterIndex, Kind = kind, Location = e.GetLocation() };
        }

        if (TryGetConstantValue(semantics, e) is { } cv) return new Constant { Value = cv, Location = e.GetLocation() };

        if (e is IdentifierNameSyntax id)
        {
            var name = id.Identifier.ValueText;
            var v = semantics.GetConstantValue(id);
            if (v.HasValue) return new Constant { Value = Variant.FromObject(v.Value), Location = id.GetLocation() };

            return new Parameter { Name = name, Location = id.GetLocation() };
        }
        else if (e is InterpolatedStringExpressionSyntax interpolatedString)
        {
            return Create(semantics, interpolatedString, parameters);
        }
        else if (e is CastExpressionSyntax cast)
        {
            var innerExpr = Create(semantics, cast.Expression, parameters);
            var targetType = Variant.GetLiteralKind(cast.Type.ToString());
            return new CastExpression { Expression = innerExpr, TargetType = targetType, Location = cast.GetLocation() };
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
            return new UnaryExpression { Operand = operand, Operator = op, Location = unary.GetLocation() };
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
            return new BinaryExpression { Left = left, Right = right, Operator = op, Location = binary.GetLocation() };
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
            return new ConditionalExpression { Condition = condition, WhenTrue = whenTrue, WhenFalse = whenFalse, Location = conditional.GetLocation() };
        }
        else if (e is ElementAccessExpressionSyntax elementAccess)
        {
            var arrayExpr = Create(semantics, elementAccess.Expression, parameters);
            if (elementAccess.ArgumentList.Arguments.Count != 1)
            {
                throw AttributeTemplateException.UnsupportedExpression(e.Kind(), e.GetLocation());
            }
            var indexExpr = Create(semantics, elementAccess.ArgumentList.Arguments[0].Expression, parameters);
            return new ElementAccessExpression { Array = arrayExpr, Index = indexExpr, Location = elementAccess.GetLocation() };
        }

        throw AttributeTemplateException.UnsupportedExpression(e.Kind(), e.GetLocation());
    }
    private static Variant? TryGetConstantValue(SemanticModel semantics, ExpressionSyntax e)
    {
        if (e is LiteralExpressionSyntax literal)
        {
            return Variant.FromObject(literal.Token.Value);
        }
        else if (e is MemberAccessExpressionSyntax ma)
        {
            // currently only supports constant values in member access.
            var v = semantics.GetConstantValue(ma);
            if (v.HasValue) return Variant.FromObject(v.Value);
        }

        return null;
    }

    private static InterpolatedString Create(SemanticModel semantics, InterpolatedStringExpressionSyntax i, ParameterList parameters)
    {
        return new InterpolatedString
        {
            Contents = [.. i.Contents.Select(c => Create(semantics, c, parameters))],
            Location = i.GetLocation(),
        };
    }

    private static InterpolatedString.Content Create(SemanticModel semantics, InterpolatedStringContentSyntax content, ParameterList parameters)
    {
        if (content is InterpolatedStringTextSyntax text)
        {
            return new InterpolatedString.StringText { Text = text.TextToken.ValueText };
        }
        else if (content is InterpolationSyntax i)
        {
            int? align = i.AlignmentClause is { } a && semantics.GetConstantValue(a.Value) is { HasValue: true, Value: int x } ? x : null;
            var fmt = i.FormatClause?.FormatStringToken.ValueText;

            var me = Create(semantics, i.Expression, parameters);

            return new InterpolatedString.Interpolation
            {
                Expression = me, // should error if me is not Name nor Type?
                Alignment = align,
                Format = fmt
            };
        }
        return null!; // error? unreachable?
    }
}
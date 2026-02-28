using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal readonly record struct Interpolation(IEnumerable<InterpolationContent> Contents)
{
    public Interpolation(SemanticModel semantics, InterpolatedStringExpressionSyntax i)
        : this([.. i.Contents.Select(c => InterpolationContent.Create(semantics, c))]) { }
}

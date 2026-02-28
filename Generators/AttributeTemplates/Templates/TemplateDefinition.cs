using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal record TemplateDefinition(string Attribute, ParameterList Params, IEnumerable<MemberTemplate> Templates)
{
    public static TemplateDefinition? Create(SemanticModel semantics, ClassDeclarationSyntax node)
    {
        if (node.BaseList is not { } b) return null;

        var parameters = new ParameterList(node.ParameterList);

        var templates = MemberTemplate.Create(semantics, b, parameters);
        if (templates is null or []) return null;

        return new(node.Identifier.ValueText, parameters, templates);
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal record TemplateInfo(string Attribute, ParameterList Params, IEnumerable<Template> Templates)
{
    public static TemplateInfo? Create(SemanticModel semantics, ClassDeclarationSyntax node, CancellationToken token)
    {
        if (node.BaseList is not { } b) return null;

        var parameters = new ParameterList(node.ParameterList);

        var templates = Template.Create(semantics, b, parameters);
        if (templates is null or []) return null;

        return new(node.Identifier.ValueText, parameters, templates);
    }
}

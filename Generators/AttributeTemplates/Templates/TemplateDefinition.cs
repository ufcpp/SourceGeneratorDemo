using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Templates;

internal record TemplateDefinition(string AttributeId, ParameterList Params, IEnumerable<MemberTemplate> Templates)
{
    public static Result<TemplateDefinition> Create(SemanticModel semantics, ClassDeclarationSyntax node)
    {
        if (node.BaseList is not { } b) return default;

        var parameters = new ParameterList(semantics, node.ParameterList);

        try
        {
            var templates = MemberTemplate.Create(semantics, b, parameters);
            if (templates is null or []) return default;

            return new TemplateDefinition(semantics.GetUniqueId(node), parameters, templates);
        }
        catch (AttributeTemplateException e)
        {
            return e.Diagnostic;
        }
    }
}

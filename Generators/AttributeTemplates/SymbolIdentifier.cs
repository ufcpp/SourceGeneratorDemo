using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates;

internal static class SymbolIdentifier
{
    /// <summary>
    /// A format for unique identifiers of members.
    /// </summary>
    private static readonly SymbolDisplayFormat _uniqueIdFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeModifiers,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static string GetUniqueId(this ISymbol symbol) => symbol.ToDisplayString(_uniqueIdFormat);

    /// <inheritdoc cref="GetUniqueId(ISymbol)"/>
    public static string GetUniqueId(this SemanticModel semanticModel, MemberDeclarationSyntax member)
    {
        var symbol = semanticModel.GetDeclaredSymbol(member);
        // if (symbol is null) todo: error;
        return GetUniqueId(symbol!);
    }

    /// <summary>
    /// Escapes special characters in the identifier string to ensure it can be safely used as a hintPath.
    /// </summary>
    /// <remarks>
    /// There may be only two characters that need escaping: '<' and '>', which are replaced with '{' and '}' respectively.
    /// </remarks>
    public static string Escape(string id)
    {
        return id
            .Replace('<', '{')
            .Replace('>', '}');
    }
}

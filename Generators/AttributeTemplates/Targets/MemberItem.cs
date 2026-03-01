using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates.Targets;

internal abstract class MemberItem
{
    public static readonly Root RootInstance = new();

    internal class Root : MemberItem;

    internal abstract class NamedMember : MemberItem
    {
        public required string Name { get; init; }
    }
    internal class Namespace : NamedMember;

    internal abstract class ModifierMember: NamedMember
    {
        public required string[] Modifiers { get; init; }
    }

    internal class TypeDeclaration : ModifierMember
    {
        public required string Keyword { get; init; }
    }

    internal abstract class TypedMember : ModifierMember
    {
        public required string Type { get; init; }
    }

    internal class Property : TypedMember;

    internal class Method : TypedMember
    {
        public required Parameter[] Parameters { get; init; }
    }

    internal record Parameter(string Name, string Type);

    public static MemberItem From(SyntaxNode member) => member switch
    {
        PropertyDeclarationSyntax p => new Property
        {
            Name = p.Identifier.ValueText,
            Type = p.Type.ToString(),
            Modifiers = GetModifiers(p.Modifiers)
        },
        MethodDeclarationSyntax m => new Method
        {
            Name = m.Identifier.ValueText,
            Type = m.ReturnType.ToString(),
            Modifiers = GetModifiers(m.Modifiers),
            Parameters = GetParameters(m)
        },
        TypeDeclarationSyntax t => new TypeDeclaration
        {
            Name = t.Identifier.ValueText,
            Modifiers = GetModifiers(t.Modifiers),
            Keyword = GetKeyword(t),
        },
        NamespaceDeclarationSyntax n => new Namespace
        {
            Name = n.Name.ToString()
        },
        CompilationUnitSyntax => RootInstance,
        _ => null!, //todo: error? never reachable?
    };

    private static Parameter[] GetParameters(MethodDeclarationSyntax m)
    {
        return [.. m.ParameterList.Parameters.Select(p => new Parameter(p.Identifier.ValueText, p.Type!.ToString()))];
    }

    private static string[] GetModifiers(SyntaxTokenList modifiers) => [.. modifiers.Select(m => m.ValueText)];

    private static string GetKeyword(TypeDeclarationSyntax t) => t.Kind() switch
    {
        SyntaxKind.ClassDeclaration => "class",
        SyntaxKind.StructDeclaration => "struct",
        SyntaxKind.RecordDeclaration => "record",
        SyntaxKind.RecordStructDeclaration => "record struct",
        _ => "", //todo: error? never reachable?
    };
}

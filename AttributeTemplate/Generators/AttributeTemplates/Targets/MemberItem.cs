using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Generators.AttributeTemplates.Targets;

internal abstract class MemberItem
{
    public abstract void AppendDeclarationLine(StringBuilder s);

    protected const string BodyBraceStart = """
 {

""";

    protected static void AppendModifiers(StringBuilder s, string[] modifiers)
    {
        foreach (var token in modifiers)
        {
            s.Append(token);
            s.Append(' ');
        }
    }

    public static readonly Root RootInstance = new();

    internal class Root : MemberItem
    {
        public override void AppendDeclarationLine(StringBuilder s) { }
    }

    internal abstract class NamedMember : MemberItem
    {
        public required string Name { get; init; }
    }
    internal class Namespace : NamedMember
    {
        public override void AppendDeclarationLine(StringBuilder s)
        {
            s.Append($"namespace {Name}{BodyBraceStart}");
        }
    }

    internal abstract class ModifierMember: NamedMember
    {
        public required string[] Modifiers { get; init; }
    }

    internal interface IHasParameters
    {
        public Parameter[]? Parameters { get; }
    }

    internal class TypeDeclaration : ModifierMember, IHasParameters
    {
        public required string Keyword { get; init; }
        public Parameter[]? Parameters { get; init; }

        public override void AppendDeclarationLine(StringBuilder s)
        {
            AppendModifiers(s, Modifiers);
            s.Append($"{Keyword} {Name}{BodyBraceStart}");
        }
    }

    internal abstract class TypedMember : ModifierMember
    {
        public required string Type { get; init; }
    }

    internal class Property : TypedMember
    {
        public override void AppendDeclarationLine(StringBuilder s)
        {
            AppendModifiers(s, Modifiers);
            s.Append($"{Type} {Name}{BodyBraceStart}");
        }
    }

    internal class Method : TypedMember, IHasParameters
    {
        public required Parameter[] Parameters { get; init; }

        public override void AppendDeclarationLine(StringBuilder s)
        {
            AppendModifiers(s, Modifiers);
            s.Append($$"""
                        {{Type}} {{Name}}(
                        """);

            var first = true;
            foreach (var mp in Parameters)
            {
                if (first) first = false;
                else s.Append(", ");
                s.Append($"{mp.Type} {mp.Name}");
            }

            s.Append(")");
            s.Append(BodyBraceStart);
        }
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
            Parameters = GetParameters(t)
        },
        BaseNamespaceDeclarationSyntax n => new Namespace
        {
            Name = n.Name.ToString()
        },
        CompilationUnitSyntax => RootInstance,
        _ => throw AttributeTemplateException.Unreachable(member.GetLocation()),
    };

    private static Parameter[] GetParameters(MethodDeclarationSyntax m)
    {
        return [.. m.ParameterList.Parameters.Select(p => new Parameter(p.Identifier.ValueText, p.Type!.ToString()))];
    }

    private static Parameter[]? GetParameters(TypeDeclarationSyntax t)
    {
        if (t.ParameterList is null) return null;
        return [.. t.ParameterList.Parameters.Select(p => new Parameter(p.Identifier.ValueText, p.Type!.ToString()))];
    }

    private static string[] GetModifiers(SyntaxTokenList modifiers) => [.. modifiers.Select(m => m.ValueText)];

    private static string GetKeyword(TypeDeclarationSyntax t) => t.Kind() switch
    {
        SyntaxKind.ClassDeclaration => "class",
        SyntaxKind.StructDeclaration => "struct",
        SyntaxKind.RecordDeclaration => "record",
        SyntaxKind.RecordStructDeclaration => "record struct",
        _ => throw AttributeTemplateException.Unreachable(t.GetLocation()),
    };
}

using Generators.AttributeTemplates.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Generators.AttributeTemplates.Application;

internal readonly struct TemplateHierarchy
{
    private readonly (SyntaxNode Node, MemberTemplate? Template)[] _templates;

    public TemplateHierarchy(IEnumerable<MemberTemplate> templates, MemberDeclarationSyntax Member)
    {
        static IEnumerable<SyntaxNode> enumerates(SyntaxNode node)
        {
            for (SyntaxNode? n = node; n != null; n = n.Parent)
                yield return n;
        }

        // bottom (Member) to top (CompilationUnit)
        var nodes = enumerates(Member).ToArray();

        var items = new (SyntaxNode Node, MemberTemplate? Template)[nodes.Length];

        for (var i = 0; i < nodes.Length; ++i)
        {
            items[i].Node = nodes[i];
        }

        foreach (var t in templates)
        {
            var l = t.Level;
            var i = l < 0 ? nodes.Length + l : l;
            items[i].Template = t; // check if already exists at the same level? error? ignore first or last?
        }

        _templates = items;
    }

    public int Count => _templates.Length;
    public IEnumerator<(SyntaxNode Node, MemberTemplate? Template)> GetEnumerator() => _templates.AsEnumerable().Reverse().GetEnumerator();

    public MemberInfo GetNode(int level)
    {
        var i = level < 0 ? _templates.Length + level : level;
        return new(_templates[i].Node as MemberDeclarationSyntax);
    }

    public readonly struct MemberInfo(MemberDeclarationSyntax? m)
    {
        //todo: generic type, full name
        public string Type => m switch
        {
            PropertyDeclarationSyntax p => p.Type.ToString(),
            MethodDeclarationSyntax m => m.ReturnType.ToString(),
            TypeDeclarationSyntax t => t.Identifier.ValueText,
            _ => "", //todo: error? never reachable?
        };

        //todo: generic method, generic type
        public string Name => m switch
        {
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            MethodDeclarationSyntax m => m.Identifier.ValueText,
            TypeDeclarationSyntax t => t.Identifier.ValueText,
            _ => "", //todo: error? never reachable?
        };
    }

    public Parameters MethodParameters => _templates[0].Node is MethodDeclarationSyntax m ? new(m.ParameterList) : default;

    public readonly struct Parameters(ParameterListSyntax list)
    {
        public Parameter this[int i] => new(list.Parameters[i]);

        public IEnumerator<Parameter> GetEnumerator()
        {
            foreach (var p in list.Parameters)
            {
                yield return new Parameter(p);
            }
        }
    }

    public readonly struct Parameter(ParameterSyntax p)
    {
        public string Type => p.Type!.ToString();
        public string Name => p.Identifier.ValueText;
    }

    public string GetId()
    {
        var sb = new StringBuilder();

        foreach (var (node, _) in this)
        {
            if (node is CompilationUnitSyntax) continue;
            else if (node is NamespaceDeclarationSyntax n)
            {
                sb.Append(n.Name).Append('.');
            }
            else if (node is TypeDeclarationSyntax t)
            {
                sb.Append(t.Identifier.ValueText).Append('.');
            }
            else if (node is PropertyDeclarationSyntax p)
            {
                sb.Append(p.Identifier.ValueText).Append('.');
            }
            else if (node is MethodDeclarationSyntax m)
            {
                sb.Append(m.Identifier.ValueText).Append('.');
            }

            //todo: method arity
            //todo: generic type arity
            //todo: parameter types for method overloads
        }
        return sb.ToString();
    }
}

using Generators.AttributeTemplates;
using Generators.AttributeTemplates.Targets;
using Generators.AttributeTemplates.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Generators;

[Generator]
public class AttributeTemplateGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.AddCommonSource();

        var templateProvider = context.CreateTemplateSyntaxProvider();

        var memberProvider = context.CreateTargetSyntaxProvider();

        var provider = memberProvider.Combine(templateProvider.Collect())
            .SelectMany((t, _) => t.Right
                .Where(x => x!.Attribute == t.Left!.Attribute)
                .Select(f => new GenerationInfo(f!.Attribute, new(f.Templates, t.Left!.Member), new(f.Params, t.Left.Args)))
            )
            .Where(x => x != null);

        context.RegisterSourceOutput(provider, Execute!);
    }

    private void Execute(SourceProductionContext context, GenerationInfo info)
    {
        var s = new StringBuilder();
        info.Generate(s);
        context.AddSource($"ATG_{info.Attribute}_{info.Templates.GetId()}g.cs", s.ToString());
    }

    private readonly struct ParameterMap
    {
        private readonly Dictionary<string, object?>? _map;
        public ParameterMap(ParameterList parameters, ArgumentList arguments)
        {
            //if (parameters.Count != arguments.Count) todo: error

            for (var i = 0; i < parameters.Count; i++)
            {
                (_map ??= []).Add(parameters[i], arguments[i]);
            }
        }

        public object? this[string parameterName] => _map is { } t && t.TryGetValue(parameterName, out var v) ? v : null;
    }

    private readonly struct TemplateHierarchy
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

            for (var i = 0; i < nodes.Length ; ++i)
            {
                items[i].Node = nodes[i];
            }

            foreach (var t in templates)
            {
                var l = t.Level;
                var i = l < 0 ? nodes.Length + l : l;
                items[i].Template = t; // check if already exists at the same level? error? ignore first or last?
            }

            // top to bottom
            items.Reverse();

            _templates = items;
        }

        public int Count => _templates.Length;
        public IEnumerator<(SyntaxNode Node, MemberTemplate? Template)> GetEnumerator() => _templates.AsEnumerable().GetEnumerator();

        public MemberInfo GetNode(int level)
        {
            var i = level >= 0 ? _templates.Length - level - 1 : -level;
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

        public Parameters MethodParameters => _templates[^1].Node is MethodDeclarationSyntax m ? new(m.ParameterList) : default;

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

            foreach (var (node, _) in _templates)
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

    private record GenerationInfo(string Attribute, TemplateHierarchy Templates, ParameterMap Params)
    {
        public void Generate(StringBuilder s)
        {
            foreach (var t in Templates)
            {
                if (t.Node is CompilationUnitSyntax)
                {
                }
                else if (t.Node is NamespaceDeclarationSyntax n)
                {
                    s.Append($$"""
                        namespace {{n.Name}} {

                        """);
                }
                else if (t.Node is TypeDeclarationSyntax type)
                {
                    appendModifiers(type.Modifiers);

                    var keyword = type.Kind() switch
                    {
                        SyntaxKind.ClassDeclaration => "class",
                        SyntaxKind.StructDeclaration => "struct",
                        SyntaxKind.RecordDeclaration => "record",
                        SyntaxKind.RecordStructDeclaration => "record struct",
                        _ => null, //todo: error
                    };

                    s.Append($$"""
                        {{keyword}} {{type.Identifier.ValueText}} {

                        """);
                }
                else if (t.Node is PropertyDeclarationSyntax p)
                {
                    appendModifiers(p.Modifiers);
                    s.Append($$"""
                        {{p.Type}} {{p.Identifier.ValueText}} {

                        """);
                }
                else if (t.Node is MethodDeclarationSyntax m)
                {
                    appendModifiers(m.Modifiers);
                    s.Append($$"""
                        {{m.ReturnType}} {{m.Identifier.ValueText}}(
                        """);

                    var first = true;
                    foreach (var mp in new TemplateHierarchy.Parameters(m.ParameterList))
                    {
                        if (first) first = false;
                        else s.Append(", ");
                        s.Append($"{mp.Type} {mp.Name}");
                    }

                    s.Append("""
                        ) {

                        """);
                }
                else { } // todo: error? never reachable?

                // apply template t.Template to the current node (s.Append(...))
                if (t.Template is { } template)
                {
                    if (template.Constant is { } c)
                    {
                        s.Append(c);
                    }
                    else if (template.Identifier is { } id)
                    {
                        var value = Params[id];
                        s.Append(value); // todo: take culture
                    }
                    else if (template.Interpolation is { } i)
                    {
                        foreach (var node in i.Contents)
                        {
                            if (node.Text is { } text)
                            {
                                s.Append(text);
                            }
                            else if (node.ConstantValue is { } c1)
                            {
                                s.AppendFormat(null, formatString(node.Alignment, node.Format), c1); // todo: take culture
                            }
                            else if (node.Identifier is { } id1)
                            {
                                if (id1 == Intrinsic.Type)
                                {
                                    if (node.Alignment is { } a)
                                    {
                                        s.Append(Templates.MethodParameters[a].Type);
                                    }
                                    else
                                    {
                                        s.Append(Templates.GetNode(node.Level).Type);
                                    }
                                }
                                else if (id1 == Intrinsic.Name)
                                {
                                    if (node.Alignment is { } a)
                                    {
                                        s.Append(Templates.MethodParameters[a].Name);
                                    }
                                    else
                                    {
                                        s.Append(Templates.GetNode(node.Level).Name);
                                    }
                                }
                                else
                                {
                                    // must be distinguish "not found" and "found null"
                                    var value = Params[id1];
                                    s.AppendFormat(null, formatString(node.Alignment, node.Format), value); // todo: take culture
                                }
                            }
                        }
                    }
                    else { }
                    newLine();
                }
            }

            s.Append('}', Templates.Count - 1);
            newLine();

            void newLine()
            {
                // source-code-dependent LF.
                s.Append(@"
");
            }

            static string formatString(int? alignment, string? format) => (alignment, format) switch
            {
                ({ } x, null) => $"{{0:{x}}}",
                (null, { } x) => $"{{0,{x}}}",
                ({ } x, { } y) => $"{{0,{x}:{y}}}",
                _ => "{0}",
            };

            void appendModifiers(SyntaxTokenList modifiers)
            {
                foreach (var token in modifiers)
                {
                    s.Append(token.ValueText);
                    s.Append(' ');
                }
            }
        }
    }
}

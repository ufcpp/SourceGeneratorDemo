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
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "TemplateAttribute.g.cs",
/* lang=C# */
"""
#nullable enable
#pragma warning disable CS9113, IDE0060
using System.Diagnostics.CodeAnalysis;

namespace AttributeTemplateGenerator;

[System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
internal class TemplateAttribute([StringSyntax("C#")] params string[] templates) : Attribute
{
    protected static string Parent(string template) => Up(0, template);
    protected static string Global(string template) => Down(0, template);
    protected static string Up(int level, string template) => template;
    protected static string Down(int level, string template) => template;
    protected const string Type = "";
    protected const string Name = "";
    public string? CultureName { get; set; }
}

"""));
        // should support?:
        // expression-bodied methods/properties
        // : base clause for classes/structs
        // : this, : base clause for constructors

        var templateProvider = context.SyntaxProvider.CreateTemplateSyntaxProvider();

        var memberProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsTemplateMember,
            GetMemberInfo)
            .Where(t => t != null);

        var provider = memberProvider.Combine(templateProvider.Collect())
            .SelectMany((t, _) => t.Right
                .Where(x => x!.Attribute == t.Left!.Attribute)
                .Select(f => new GenerationInfo(f!.Attribute, new(f.Templates, t.Left!.Member), new(f.Params, t.Left.Args)))
            )
            .Where(x => x != null);

        context.RegisterSourceOutput(provider, Execute!);
    }

    private bool IsTemplateMember(SyntaxNode node, CancellationToken token)
    {
        return node is MemberDeclarationSyntax d
            && d.Modifiers.Any(SyntaxKind.PartialKeyword)
            && d.AttributeLists.Any()
            && (d is PropertyDeclarationSyntax or MethodDeclarationSyntax or TypeDeclarationSyntax);
    }

    private MemberInfo? GetMemberInfo(GeneratorSyntaxContext context, CancellationToken token)
    {
        var d = (MemberDeclarationSyntax)context.Node;
        var semantics = context.SemanticModel;

        if (GetAttribute(d, semantics) is not { } t) return null;

        return new(t.Name, t.Args, d);
    }

    private static (string Name, ArgumentList Args)? GetAttribute(MemberDeclarationSyntax d, SemanticModel semantics)
    {
        foreach (var list in d.AttributeLists)
        {
            foreach (var a in list.Attributes)
            {
                var at = semantics.GetTypeInfo(a);

                if (at.Type is not { } t) continue;
                
                var bat = at.Type?.BaseType;
                if (bat is { ContainingNamespace.Name: "AttributeTemplateGenerator", Name: "TemplateAttribute" })
                {
                    return (t.Name, new(semantics, a.ArgumentList));
                }
            }
        }

        return null;
    }

    private void Execute(SourceProductionContext context, GenerationInfo info)
    {
        var s = new StringBuilder();
        info.Generate(s);
        context.AddSource($"ATG_{info.Attribute}_{info.Templates.GetId()}g.cs", s.ToString());
    }

    private readonly struct ArgumentList
    {
        private readonly object?[]? _values;

        public ArgumentList(SemanticModel semantics, AttributeArgumentListSyntax? list)
        {
            if (list is null)
            {
                _values = null;
                return;
            }

            var values = new object?[list.Arguments.Count];
            var i = 0;

            foreach (var a in list.Arguments)
            {
                var v = semantics.GetConstantValue(a.Expression);
                //if (!v.HasValue) todo: error
                values[i++] = v;
            }

            _values = values;
        }

        public int Count => _values?.Length ?? 0;
        public object? this[int index] => _values![index];
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
        private readonly (SyntaxNode Node, Template? Template)[] _templates;

        public TemplateHierarchy(IEnumerable<Template> templates, MemberDeclarationSyntax Member)
        {
            static IEnumerable<SyntaxNode> enumerates(SyntaxNode node)
            {
                for (SyntaxNode? n = node; n != null; n = n.Parent)
                    yield return n;
            }

            // bottom (Member) to top (CompilationUnit)
            var nodes = enumerates(Member).ToArray();

            var items = new (SyntaxNode Node, Template? Template)[nodes.Length];

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
        public IEnumerator<(SyntaxNode Node, Template? Template)> GetEnumerator() => _templates.AsEnumerable().GetEnumerator();

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

    private record MemberInfo(string Attribute, ArgumentList Args, MemberDeclarationSyntax Member);
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
                                if (id1 == "Type")
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
                                else if (id1 == "Name")
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

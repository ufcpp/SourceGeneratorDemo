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
#pragma warning disable CS9113, IDE0060
using System.Diagnostics.CodeAnalysis;

namespace AttributeTemplateGenerator;

[System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
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

        var templateProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsAttributeDeclaration,
            GetTemplateInfo)
            .Where(t => t != null);

        var memberProvider = context.SyntaxProvider.CreateSyntaxProvider(
            IsTemplateMember,
            GetMemberInfo)
            .Where(t => t != null);

        var provider = memberProvider.Combine(templateProvider.Collect())
            .Select((t, _) =>
            {
                if (t.Right.FirstOrDefault(x => x!.Attribute.Equals(t.Left!.Attribute)) is { } f)
                {
                    return new GenerationInfo(new(f.Templates, t.Left!.Member), new(f.Params, t.Left.Args));
                }
                return null;
            })
            .Where(x => x != null);

        context.RegisterSourceOutput(provider, Execute!);
    }

    private bool IsAttributeDeclaration(SyntaxNode node, CancellationToken token)
    {
        return node is ClassDeclarationSyntax c
            && c.Identifier.ValueText.EndsWith("Attribute");
    }

    private TemplateInfo? GetTemplateInfo(GeneratorSyntaxContext context, CancellationToken token)
    {
        var d = (ClassDeclarationSyntax)context.Node;

        if (d.BaseList is not { } b) return null;

        var parameters = new ParameterList(d.ParameterList);

        var templates = GetTemplates(b, context.SemanticModel, parameters);
        if (templates is null or []) return null;

        return new(d.Identifier.ValueText, parameters, templates);
    }

    private static Template[]? GetTemplates(BaseListSyntax b, SemanticModel semantics, ParameterList parameters)
    {
        foreach (var t in b.Types)
        {
            if (t is not PrimaryConstructorBaseTypeSyntax pc) continue;

            var ti = semantics.GetTypeInfo(t.Type);
            if (ti.Type is { ContainingNamespace.Name: "AttributeTemplateGenerator", Name: "TemplateAttribute" })
            {
                var templates = new Template[pc.ArgumentList.Arguments.Count];
                var i = 0;
                foreach (var arg in pc.ArgumentList.Arguments)
                {
                    templates[i++] = Template.Create(semantics, parameters, arg.Expression);
                }
                return templates;
            }
        }

        return null;
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
        context.AddSource("AttributeTemplate_" + info.Templates.GetId() + "g.cs", s.ToString());
    }

    private readonly record struct InterpolationContent(string? Text = null, object? ConstantValue = null, string? Identifier = null, int? Alignment = null, string? Format = null)
    {
        public static InterpolationContent Create(SemanticModel semantics, InterpolatedStringContentSyntax c)
        {
            if (c is InterpolatedStringTextSyntax t)
            {
                return new(Text: t.TextToken.ValueText);
            }
            else if (c is InterpolationSyntax i)
            {
                int? align = i.AlignmentClause is { } a && semantics.GetConstantValue(a.Value) is { HasValue: true, Value: int x } ? x : null;
                var fmt = i.FormatClause?.FormatStringToken.ValueText;
                if (i.Expression is IdentifierNameSyntax id)
                {
                    return new(Identifier: id.Identifier.ValueText, Alignment: align, Format: fmt);
                }
                else
                {
                    var v = semantics.GetConstantValue(i.Expression);
                    if (v.HasValue) return new(ConstantValue: v.Value, Alignment: align, Format: fmt);
                }
            }
            return default;
        }

        public override string ToString()
        {
            if (Text is { } t) return t;
            if (ConstantValue is { } c) return $"{{{c},{Alignment}:{Format}}}";
            if (Identifier is { } i) return $"{{{i},{Alignment}:{Format}}}";
            return "";
        }
    }

    private readonly record struct Interpolation(IEnumerable<InterpolationContent> Contents)
    {
        public Interpolation(SemanticModel semantics, InterpolatedStringExpressionSyntax i)
            : this([.. i.Contents.Select(c => InterpolationContent.Create(semantics, c))]) { }
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

    private readonly struct ParameterList
    {
        private readonly string[]? _parameterNames;
        private readonly Dictionary<string, string>? _nameToTypeTable;

        public ParameterList(ParameterListSyntax? list)
        {
            if (list is null)
            {
                _parameterNames = null;
                _nameToTypeTable = null;
                return;
            }

            var names = new string[list.Parameters.Count];
            Dictionary<string, string> d = [];
            var i = 0;

            foreach (var p in list.Parameters)
            {
                if (p.Type is not PredefinedTypeSyntax pd) continue; // todo: error
                d.Add(p.Identifier.ValueText, pd.Keyword.ValueText);
                names[i++] = p.Identifier.ValueText;
            }

            _parameterNames = names;
            _nameToTypeTable = d;
        }

        public int Count => _parameterNames?.Length ?? 0;
        public string this[int index] => _parameterNames![index];
        public bool Contains(string parameterName) => _nameToTypeTable is { } t && t.ContainsKey(parameterName); 
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

    private record Template(int Level, Interpolation? Interpolation = null, string? Constant = null, string? Identifier = null)
    {
        public static Template Create(SemanticModel semantics, ParameterList parameters, ExpressionSyntax e)
        {
            int? IntValue(ExpressionSyntax ex)
            {
                var v = semantics.GetConstantValue(ex);
                if (v.HasValue && v.Value is int i) return i;
                return null;
            }

            int level = 0;

            if (e is InvocationExpressionSyntax { Expression: IdentifierNameSyntax n, ArgumentList.Arguments: var args })
            {
                var name = n.Identifier.ValueText;

                if (name == "Parent" && args.Count == 1)
                {
                    level = 1;
                    e = args[0].Expression;
                }
                else if (name == "Global" && args.Count == 1)
                {
                    level = -1;
                    e = args[0].Expression;
                }
                if (name == "Up" && args.Count == 2 && IntValue(args[0].Expression) is int x)
                {
                    level = x + 1;
                    e = args[1].Expression;
                }
                else if (name == "Down" && args.Count == 2 && IntValue(args[0].Expression) is int y)
                {
                    level = -y;
                    e = args[1].Expression;
                }
                //todo: error
            }

            if (e is InterpolatedStringExpressionSyntax i)
            {
                return new(level, Interpolation: new(semantics, i));
            }
            else if (e is IdentifierNameSyntax id)
            {
                var s0 = semantics.GetConstantValue(id);
                if (s0.HasValue && s0.Value is string s) return new(level, Constant: s);

                var name = id.Identifier.ValueText;
                if (parameters.Contains(name) || name == "Type" || name == "Name")
                    return new(level, Identifier: name);
            }
            else if (e is LiteralExpressionSyntax l)
            {
                var s0 = semantics.GetConstantValue(l);
                if (s0.HasValue && s0.Value is string s) return new(level, Constant: s);
            }

            //todo: error
            return new(0);
        }
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

        public MemberDeclarationSyntax Member => (MemberDeclarationSyntax)_templates[^1].Node;

        //todo: generic type, full name
        public string Type => Member switch
        {
            PropertyDeclarationSyntax p => p.Type.ToString(),
            MethodDeclarationSyntax m => m.ReturnType.ToString(),
            TypeDeclarationSyntax t => t.Identifier.ValueText,
            _ => "", //todo: error? never reachable?
        };

        //todo: generic method, generic type
        public string Name => Member switch
        {
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            MethodDeclarationSyntax m => m.Identifier.ValueText,
            TypeDeclarationSyntax t => t.Identifier.ValueText,
            _ => "", //todo: error? never reachable?
        };

        public Parameters MethodParameters => Member is MethodDeclarationSyntax m ? new(m.ParameterList) : default;

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

    private record TemplateInfo(string Attribute, ParameterList Params, IEnumerable<Template> Templates);
    private record MemberInfo(string Attribute, ArgumentList Args, MemberDeclarationSyntax Member);
    private record GenerationInfo(TemplateHierarchy Templates, ParameterMap Params)
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
                    var keyword = type.Kind() switch
                    {
                        SyntaxKind.ClassDeclaration => "class",
                        SyntaxKind.StructDeclaration => "struct",
                        SyntaxKind.RecordDeclaration => "record",
                        SyntaxKind.RecordStructDeclaration => "record struct",
                        _ => null, //todo: error
                    };

                    s.Append($$"""
                        partial {{keyword}} {{type.Identifier.ValueText}} {

                        """);
                }
                else if (t.Node is PropertyDeclarationSyntax p)
                {
                    s.Append($$"""
                        {{string.Join(" ", p.Modifiers.Select(m => m.ValueText))}} {{p.Type.GetText()}} {{p.Identifier.ValueText}} {

                        """);
                }
                else if (t.Node is MethodDeclarationSyntax m)
                {
                    s.Append($$"""
                        {{string.Join(" ", m.Modifiers.Select(m => m.ValueText))}} {{m.ReturnType.GetText()}} {{m.Identifier.ValueText}}(
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
                                        s.Append(Templates.Type);
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
                                        s.Append(Templates.Name);
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
        }
    }
}

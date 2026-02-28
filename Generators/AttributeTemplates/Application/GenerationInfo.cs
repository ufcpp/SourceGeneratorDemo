using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Generators.AttributeTemplates.Application;

internal record GenerationInfo(string Attribute, TemplateHierarchy Templates, ParameterMap Params)
{
    public string Generate()
    {
        var s = new StringBuilder();

        foreach (var (node, mt) in Templates)
        {
            if (node is CompilationUnitSyntax)
            {
            }
            else if (node is NamespaceDeclarationSyntax n)
            {
                s.Append($$"""
                        namespace {{n.Name}} {

                        """);
            }
            else if (node is TypeDeclarationSyntax type)
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
            else if (node is PropertyDeclarationSyntax p)
            {
                appendModifiers(p.Modifiers);
                s.Append($$"""
                        {{p.Type}} {{p.Identifier.ValueText}} {

                        """);
            }
            else if (node is MethodDeclarationSyntax m)
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

            // apply member template
            if (mt is null) continue;

            if (mt.Constant is { } constValue)
            {
                s.Append(constValue);
            }
            else if (mt.Identifier is { } id)
            {
                var value = Params[id];
                s.Append(value); // todo: take culture
            }
            else if (mt.Interpolation is { } i)
            {
                foreach (var c in i.Contents)
                {
                    if (c.Text is { } text)
                    {
                        s.Append(text);
                    }
                    else if (c.ConstantValue is { } c1)
                    {
                        s.AppendFormat(null, formatString(c.Alignment, c.Format), c1); // todo: take culture
                    }
                    else if (c.Identifier is { } id1)
                    {
                        if (id1 == Intrinsic.Type)
                        {
                            if (c.Alignment is { } a)
                            {
                                s.Append(Templates.MethodParameters[a].Type);
                            }
                            else
                            {
                                s.Append(Templates.GetNode(c.Level).Type);
                            }
                        }
                        else if (id1 == Intrinsic.Name)
                        {
                            if (c.Alignment is { } a)
                            {
                                s.Append(Templates.MethodParameters[a].Name);
                            }
                            else
                            {
                                s.Append(Templates.GetNode(c.Level).Name);
                            }
                        }
                        else
                        {
                            // must be distinguish "not found" and "found null"
                            var value = Params[id1];
                            s.AppendFormat(null, formatString(c.Alignment, c.Format), value); // todo: take culture
                        }
                    }
                }
            }
            else { }
            newLine();
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

        return s.ToString();
    }
}

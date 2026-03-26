using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Generators;

[Generator]
public class RecordEqualityGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generate the ExplicitKey attribute
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ExplicitKeyAttribute.g.cs",
            """
            using System;

            namespace RecordEqualityGenerator;

            [System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
            [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            internal class ExplicitKeyAttribute : Attribute
            {
            }
            """));

        // Generate the IgnoreKey attribute
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "IgnoreKeyAttribute.g.cs",
            """
            using System;

            namespace RecordEqualityGenerator;

            [System.Diagnostics.Conditional("COMPILE_TIME_ONLY")]
            [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            internal class IgnoreKeyAttribute : Attribute
            {
            }
            """));

        // Find record types with properties marked with [ExplicitKey] or [IgnoreKey]
        var recordDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) => IsRecordDeclaration(node),
            transform: (context, _) => GetRecordInfo(context))
            .Where(r => r is not null);

        context.RegisterSourceOutput(recordDeclarations, Execute!);
    }

    private static bool IsRecordDeclaration(SyntaxNode node)
    {
        return node is RecordDeclarationSyntax;
    }

    private static RecordInfo? GetRecordInfo(GeneratorSyntaxContext context)
    {
        var recordDecl = (RecordDeclarationSyntax)context.Node;
        var recordSymbol = context.SemanticModel.GetDeclaredSymbol(recordDecl);

        if (recordSymbol is null)
            return null;

        var allProperties = recordSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.Name != "EqualityContract") // Exclude EqualityContract
            .ToImmutableArray();

        // Check if any property has [ExplicitKey] or [IgnoreKey]
        var hasExplicitKey = allProperties.Any(p => p.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "RecordEqualityGenerator.ExplicitKeyAttribute"));

        var hasIgnoreKey = allProperties.Any(p => p.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "RecordEqualityGenerator.IgnoreKeyAttribute"));

        // If neither attribute is used, skip this record
        if (!hasExplicitKey && !hasIgnoreKey)
            return null;

        // If both attributes are used, ExplicitKey takes precedence
        ImmutableArray<PropertyInfo> equalityKeyProperties;

        if (hasExplicitKey)
        {
            // Use only properties with [ExplicitKey]
            equalityKeyProperties = [.. allProperties
                .Where(p => p.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == "RecordEqualityGenerator.ExplicitKeyAttribute"))
                .Select(p => new PropertyInfo(p.Name, GetCSharpTypeName(p.Type)))];
        }
        else
        {
            // Use all properties except those with [IgnoreKey]
            equalityKeyProperties = [.. allProperties
                .Where(p => !p.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == "RecordEqualityGenerator.IgnoreKeyAttribute"))
                .Select(p => new PropertyInfo(p.Name, GetCSharpTypeName(p.Type)))];
        }

        if (equalityKeyProperties.IsEmpty)
            return null;

        var namespaceName = recordSymbol.ContainingNamespace.IsGlobalNamespace
            ? ""
            : recordSymbol.ContainingNamespace.ToDisplayString();

        // Get type parameters
        var typeParameters = recordSymbol.TypeParameters
            .Select(tp => tp.Name)
            .ToImmutableArray();

        return new RecordInfo(
            recordSymbol.Name,
            namespaceName,
            recordSymbol.IsRecord,
            recordSymbol.Arity,
            typeParameters,
            equalityKeyProperties);
    }

    private static string GetCSharpTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining));
    }

    private static void Execute(SourceProductionContext context, RecordInfo recordInfo)
    {
        var source = GenerateSource(recordInfo);

        var typeNameWithArity = recordInfo.Arity > 0 
            ? $"{recordInfo.RecordName}`{recordInfo.Arity}"
            : recordInfo.RecordName;

        var hintName = string.IsNullOrEmpty(recordInfo.Namespace)
            ? $"{typeNameWithArity}.Equality.g.cs"
            : $"{recordInfo.Namespace}.{typeNameWithArity}.Equality.g.cs";
        context.AddSource(hintName, source);
    }

    private static string GenerateSource(RecordInfo recordInfo)
    {
        var (recordName, namespaceName, isRecord, _, typeParameters, properties) = recordInfo;

        // Build type parameter list for generic types
        var typeParameterList = typeParameters.IsEmpty
            ? ""
            : $"<{string.Join(", ", typeParameters)}>";

        var recordNameWithTypeParams = $"{recordName}{typeParameterList}";

        var sb = new StringBuilder();
        sb.Append("""
// <auto-generated/>
#nullable enable
#pragma warning disable

""");
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.Append($$"""
namespace {{namespaceName}};


""");
        }

        if (!isRecord)
        {
            // For classes, generate normal Equals/GetHashCode
            sb.Append($$"""
partial class {{recordNameWithTypeParams}}
{
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not {{recordNameWithTypeParams}} other) return false;

""");

            foreach (var property in properties)
            {
                sb.Append($$"""
        if (!global::System.Collections.Generic.EqualityComparer<{{property.PropertyType}}>.Default.Equals({{property.PropertyName}}, other.{{property.PropertyName}})) return false;

""");
            }

            sb.Append("""
        return true;
    }

    public override int GetHashCode()
    {
        var hash = new global::System.HashCode();

""");

            foreach (var property in properties)
            {
                sb.Append($$"""
        hash.Add({{property.PropertyName}});

""");
            }

            sb.Append("""
        return hash.ToHashCode();
    }
}
""");

            return sb.ToString();
        }

        // For records, we need to override the generated Equals methods
        sb.Append($$"""
partial record {{recordNameWithTypeParams}}
{
    public virtual bool Equals({{recordNameWithTypeParams}}? other)
    {
        return ReferenceEquals(this, other) || other is not null && EqualityContract == other.EqualityContract

""");

        foreach (var property in properties)
        {
            sb.Append($$"""
            && global::System.Collections.Generic.EqualityComparer<{{property.PropertyType}}>.Default.Equals({{property.PropertyName}}, other.{{property.PropertyName}})

""");
        }

        sb.Append("""
        ;
    }

    public override int GetHashCode()
    {
        var hash = new global::System.HashCode();
        hash.Add(EqualityContract);

""");

        foreach (var property in properties)
        {
            sb.Append($$"""
        hash.Add({{property.PropertyName}});

""");
        }

        sb.Append("""
        return hash.ToHashCode();
    }
}
""");

        return sb.ToString();
    }

    private record PropertyInfo(string PropertyName, string PropertyType);

    private record RecordInfo(
        string RecordName,
        string Namespace,
        bool IsRecord,
        int Arity,
        ImmutableArray<string> TypeParameters,
        ImmutableArray<PropertyInfo> Properties);
}


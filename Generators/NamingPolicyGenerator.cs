using Generators.NamingPolicyConverter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Frozen;

namespace Generators;

[Generator]
public class NamingPolicyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "NamingPolicyConverter.g.cs",
            /* lang=C# */
            """
            using System;

            namespace NamingPolicyGenerator;

            internal static class NamingPolicyConverter
            {
                extension(string s)
                {
                    public string CamelCase() => s;
                    public string PascalCase() => s;
                    public string CamelCaseLower() => s;
                    public string SnakeCaseLower() => s;
                    public string SnakeCaseUpper() => s;
                    public string KebabCaseLower() => s;
                    public string KebabCaseUpper() => s;
                }
            }

            """));

        var converterInvocations = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => IsConverterInvocation(node),
                GetSemanticTargetForGeneration)
            .Where(x => x != null);

        context.RegisterSourceOutput(converterInvocations, Execute!);
    }

    private static bool IsConverterInvocation(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberAccessExpressionSyntax ma
            && _policyNames.ContainsKey(ma.Name.Identifier.ValueText);
    }

    private static ConversionInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var ma = (MemberAccessExpressionSyntax)invocation.Expression;

        if (!_policyNames.TryGetValue(ma.Name.Identifier.ValueText, out var policy)) return null;

        var semantic = context.SemanticModel;

        var opt = semantic.GetConstantValue(ma.Expression);

        // Only support constant strings.
        if (!opt.HasValue || opt.Value is not string str) return null;

        // Empty string is identical to all of the policies, so we can skip generating a interceptor for it.
        if (str.Length == 0) return null;

        var loc = semantic.GetInterceptableLocation(invocation, ct);
        if (loc is null) return null;

        return new(loc, str, policy);
    }

    private static void Execute(SourceProductionContext context, ConversionInfo info)
    {
        var source = GenerateSource(info);

        // Should skip if String == GetConvertedString()?
        context.AddSource($"Naming.{info.UniqueId()}.g.cs", source);
    }

    private static string GenerateSource(ConversionInfo info)
    {
        return $$""""
namespace Interceptors.Generated
{
    internal static partial class NamingPolicy
    {
        [System.Runtime.CompilerServices.InterceptsLocation({{info.Location.Version}}, "{{info.Location.Data}}")]
        internal static string _{{info.UniqueId()}}(this string _) => """
{{info.GetConvertedString()}}
""";
    }
}

namespace System.Runtime.CompilerServices
{
#pragma warning disable CS9113
    [Diagnostics.Conditional("COMPILE_TIME_ONLY")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    file sealed class InterceptsLocationAttribute(int version, string data) : Attribute;
}

"""";
    }

    private static readonly FrozenDictionary<string, NamingPolicy> _policyNames = ((NamingPolicy[])Enum.GetValues(typeof(NamingPolicy)))
        .ToFrozenDictionary(x => x.ToString());

    private record ConversionInfo(
        InterceptableLocation Location,
        string String,
        NamingPolicy Policy)
    {
        private string? _uniqueId;

        public string UniqueId() => _uniqueId ??= Location.Data.GetHashCode().ToString("X8");

        public string GetConvertedString() => String.Convert(Policy);
    }
}

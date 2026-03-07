using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Generators;

[Generator]
public class ConstantInterpolationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ConstantInterpolation.g.cs",
            /* lang=C# */
            """
            using System;

            namespace ConstantInterpolationGenerator;

            internal static class Constant
            {
                extension(string s)
                {
                    public string Invariant() => s;
                    public string Local(string cultureName) => s;
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
            && (
            (ma.Name.Identifier.ValueText == "Invariant" && invocation.ArgumentList.Arguments.Count == 0)
            || (ma.Name.Identifier.ValueText == "Local" && invocation.ArgumentList.Arguments.Count == 1)
            );
    }

    private static ConversionInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var ma = (MemberAccessExpressionSyntax)invocation.Expression;

        static Error error(DiagnosticDescriptor descriptor, CSharpSyntaxNode node) => new(descriptor, node.GetLocation());

        if (ma.Expression is not InterpolatedStringExpressionSyntax i) return error(s_NotInterpolatedStringError, ma.Expression);

        var semantic = context.SemanticModel;

        // Interpolations like $"{"constant"}" are already constant, so we can skip generating an interceptor for them.
        if (semantic.GetConstantValue(i, ct).HasValue) return null;

        CultureInfo? culture = null;

        if (ma.Name.Identifier.ValueText == "Local")
        {
            var arg = invocation.ArgumentList.Arguments[0];
            var opt = semantic.GetConstantValue(arg.Expression, ct);
            if (!opt.HasValue || opt.Value is not string name) return error(s_NonConstantCultureNameError, arg);

            try
            {
                culture = CultureInfo.GetCultureInfo(name);
                if (culture.LCID == 4096) return error(s_CultureNotFoundError, arg);
            }
            catch (CultureNotFoundException)
            {
                return error(s_CultureNotFoundError, arg);
            }
        }
        else if (ma.Name.Identifier.ValueText != "Invariant") return null;

        culture ??= CultureInfo.InvariantCulture;

        // .NET 8 allows us to use DefaultInterpolatedStringHandler for better performance
        var literalLength = 0;
        var formattedCount = 0;

        foreach (var node in i.Contents)
        {
            if (node is InterpolatedStringTextSyntax text)
            {
                literalLength += text.TextToken.ValueText.Length;
            }
            else if (node is InterpolationSyntax)
            {
                formattedCount++;
            }
        }

        var handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, culture);

        foreach (var node in i.Contents)
        {
            if (node is InterpolatedStringTextSyntax text)
            {
                handler.AppendLiteral(text.TextToken.ValueText);
            }
            else if (node is InterpolationSyntax hole)
            {
                if (semantic.GetConstantValue(hole.Expression, ct) is not { HasValue: true, Value: var value }) return error(s_NonConstantError, hole.Expression);

                var alignment = hole.AlignmentClause is { } a && semantic.GetConstantValue(a.Value, ct) is { HasValue: true, Value: int align } ? align : 0;
                var format = hole.FormatClause?.FormatStringToken.ValueText;

                handler.AppendFormatted(value, alignment, format);
            }
        }

        var loc = semantic.GetInterceptableLocation(invocation, ct);
        if (loc is null) return null;

        return new Result(loc, handler.ToStringAndClear(), culture == CultureInfo.InvariantCulture);
    }

    private static void Execute(SourceProductionContext context, ConversionInfo info)
    {
        if (info is Result r)
        {
            var source = GenerateSource(r);
            context.AddSource($"ConstantInterpolation.{r.UniqueId()}.g.cs", source);
        }
        else if (info is Error e)
        {
            context.ReportDiagnostic(e.Diagnostic);
        }
    }

    private static string GenerateSource(Result info)
    {
        return $$""""
namespace Interceptors.Generated
{
    internal static partial class ConstantInterpolation
    {
        [System.Runtime.CompilerServices.InterceptsLocation({{info.Location.Version}}, "{{info.Location.Data}}")]
        internal static string _{{info.UniqueId()}}(this string _{{(info.Invariant ? "" : ", string _1")}}) => """
{{info.String}}
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

    private record ConversionInfo;

    private record Result(
        InterceptableLocation Location,
        string String,
        bool Invariant) : ConversionInfo
    {
        private string? _uniqueId;
        public string UniqueId() => _uniqueId ??= Location.Data.GetHashCode().ToString("X8");
    }

    private record Error(
        Diagnostic Diagnostic) : ConversionInfo
    {
        public Error(DiagnosticDescriptor descriptor, Location? location)
            : this(Diagnostic.Create(descriptor, location)) { }
    }

    private static readonly DiagnosticDescriptor s_NotInterpolatedStringError = new(
        "CIG001",
        "Not an interpolated string",
        "The method can only be called on interpolated strings",
        "ConstantInterpolationGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_NonConstantError = new(
        "CIG002",
        "Non-constant interpolation",
        "The interpolated string contains non-constant expressions, which cannot be converted",
        "ConstantInterpolationGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_NonConstantCultureNameError = new(
        "CIG003",
        "Non-constant culture name",
        "The argument to Local() must be a constant string representing a culture name",
        "ConstantInterpolationGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_CultureNotFoundError = new(
       "CIG004",
       "Invalid culture name",
       "The argument to Local() must be a valid culture name",
       "ConstantInterpolationGenerator",
       DiagnosticSeverity.Error,
       isEnabledByDefault: true);
}

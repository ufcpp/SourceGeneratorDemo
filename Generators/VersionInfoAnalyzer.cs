using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class VersionInfoAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: "VER001",
        title: "Build Environment Information",
        messageFormat: "Compiler: {0}, Runtime: {1}, OS: {2}",
        category: "Build",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeErrorDirective, SyntaxKind.ErrorDirectiveTrivia);
    }

    private void AnalyzeErrorDirective(SyntaxNodeAnalysisContext context)
    {
        var errorDirective = (ErrorDirectiveTriviaSyntax)context.Node;

        if (errorDirective.ToString().Contains("version"))
        {
            var compilerVersion = typeof(Compilation).Assembly.GetName().Version;
            var runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            var diagnostic = Diagnostic.Create(Rule, errorDirective.GetLocation(), compilerVersion, runtime, os);
            context.ReportDiagnostic(diagnostic);
        }
    }
}

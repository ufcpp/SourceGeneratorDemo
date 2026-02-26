using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace UnitTests;

internal record struct GeneratedSource(
    string FileName,
    [StringSyntax("C#")] string Source);

internal static class Helpers
{
    public static void RunGenerator(
    IIncrementalGenerator generator,
    [StringSyntax("C#")] string targetSource,
    params GeneratedSource[] generatedSources)
        => RunGenerator(generator, targetSource, generatedSources, null);

    public static void RunGenerator(
        IIncrementalGenerator generator,
        [StringSyntax("C#")] string targetSource,
        GeneratedSource[] generatedSources,
        string[]? errorIds)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var driver = CSharpGeneratorDriver.Create(generator).WithUpdatedParseOptions(parseOptions);

        var compilation = CSharpCompilation.Create("GeneratorTest",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(targetSource, parseOptions));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        var map = generatedSources.ToDictionary(s => s.FileName, s => s.Source!);
        var matched = 0;

        foreach (var t in newCompilation.SyntaxTrees)
        {
            var name = Path.GetFileName(t.FilePath);
            if (!name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) continue;
            name = name[..^".g.cs".Length];

            if (map.TryGetValue(name, out var expected))
            {
                var s = t.GetText().ToString();
                Assert.Equal(expected, s);
                matched++;
            }
        }

        Assert.Equal(generatedSources.Length, matched);

        if (errorIds is null)
        {
            Assert.Empty(diagnostics);
        }
        else
        {
            Assert.Equal(errorIds, diagnostics.Select(x => x.Id));
        }
    }
}
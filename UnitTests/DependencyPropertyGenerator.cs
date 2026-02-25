using Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace UnitTests;

public class DependencyPropertyGeneratorTests
{
    [Fact]
    public void X()
    {
        RunGenerator("""
            using DependencyPropertyGenerator;
            using DP = DependencyPropertyGenerator.DependencyProperty;

            class C
            {
                [DependencyPropertyGenerator.DependencyProperty]
                public partial int Integer { get; set; }

                [DependencyProperty]
                public partial string? Str { get; set; }

                [DependencyProperty]
                public partial TimeOnly Time { get; set; }

                [DependencyProperty]
                public partial DateOnly? Date { get; set; }
            }
            """,
            [
                new("C.Integer", """
                using System.Windows;

                partial class C
                {
                    public static readonly DependencyProperty IntegerProperty =
                        DependencyProperty.Register(
                            nameof(Integer),
                            typeof(int),
                            typeof(C),
                            new PropertyMetadata(default(int)));

                    public partial int Integer
                    {
                        get => (int)GetValue(IntegerProperty);
                        set => SetValue(IntegerProperty, value);
                    }
                }
                """),
                new("C.Str", """
                using System.Windows;

                partial class C
                {
                    public static readonly DependencyProperty StrProperty =
                        DependencyProperty.Register(
                            nameof(Str),
                            typeof(string),
                            typeof(C),
                            new PropertyMetadata(default(string)));

                    public partial string Str
                    {
                        get => (string)GetValue(StrProperty);
                        set => SetValue(StrProperty, value);
                    }
                }
                """),
                new("C.Time", """
                using System.Windows;

                partial class C
                {
                    public static readonly DependencyProperty TimeProperty =
                        DependencyProperty.Register(
                            nameof(Time),
                            typeof(TimeOnly),
                            typeof(C),
                            new PropertyMetadata(default(TimeOnly)));

                    public partial TimeOnly Time
                    {
                        get => (TimeOnly)GetValue(TimeProperty);
                        set => SetValue(TimeProperty, value);
                    }
                }
                """),
                new("C.Date", """
                using System.Windows;

                partial class C
                {
                    public static readonly DependencyProperty DateProperty =
                        DependencyProperty.Register(
                            nameof(Date),
                            typeof(DateOnly?),
                            typeof(C),
                            new PropertyMetadata(default(DateOnly?)));

                    public partial DateOnly? Date
                    {
                        get => (DateOnly?)GetValue(DateProperty);
                        set => SetValue(DateProperty, value);
                    }
                }
                """),
            ]);
    }

    private record struct GeneratedSource(
        string FileName,
        [StringSyntax("C#")] string Source);

    private static void RunGenerator(
        [StringSyntax("C#")] string targetSource,
        params GeneratedSource[] generatedSources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var driver = CSharpGeneratorDriver.Create(new DependencyPropertyGenerator()).WithUpdatedParseOptions(parseOptions);

        var compilation = CSharpCompilation.Create("GeneratorTest",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(targetSource, parseOptions));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        Assert.Empty(diagnostics);

        var map = generatedSources.ToDictionary(s => s.FileName, s => s.Source);
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
    }
}

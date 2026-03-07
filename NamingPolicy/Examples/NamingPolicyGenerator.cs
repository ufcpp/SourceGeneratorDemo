using NamingPolicyGenerator;

namespace Examples;

public class NamingPolicyGeneratorExample
{
    public static void Run()
    {
        var x = "abc-def".CamelCase();

        const string s = "ABC-DEF";
        var y = s.SnakeCaseLower();

        var z = A.X.KebabCaseUpper();

        Console.WriteLine((x, y, z));

        Console.WriteLine(@"verbatim""string".PascalCase());

        Console.WriteLine("""
            raw string
            """.KebabCaseLower());
        Console.WriteLine($"string interpolation with {"const string"}".SnakeCaseLower());

        Console.WriteLine($"""
            raw string interpolation with nameof {nameof(A.X)}
            """.SnakeCaseLower());

        // should warn on non-constants?
        var a = new string('x', 1).CamelCase();
        var b = $"a{1}b{2}".CamelCase();
        // should warn on empty string?
        var c = "".CamelCase();
        Console.WriteLine((a, b, c));
    }

    static class A
    {
        public const string X = "AbcDef";
    }
}

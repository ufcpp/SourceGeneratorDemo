using Generators;
using Xunit;

namespace UnitTests;

public class NamingPolicyGeneratorTests
{
    [Fact]
    public void Generate()
    {
        Helpers.RunGenerator(
            new NamingPolicyGenerator(),
            """
            using NamingPolicyGenerator;

            var x = "abc-def".CamelCase();

            const string s = "AbcDef";
            var y = s.SnakeCaseLower();

            var z = A.X.KebabCaseUpper();

            var a = new string().CamelCase();
            var b = $"{1}".CamelCase();

            static class A
            {
                public const string X = "ABC-DEF";
            }

            """);
    }
}

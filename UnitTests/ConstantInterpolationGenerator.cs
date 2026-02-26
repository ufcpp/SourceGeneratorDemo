using Generators;
using Xunit;

namespace UnitTests;

public class ConstantInterpolationGeneratorTests
{
    [Fact]
    public void Generate()
    {
        Helpers.RunGenerator(
            new ConstantInterpolationGenerator(),
            """
            using ConstantInterpolationGenerator;

            var a = $"ab{1}cd{1.2}ef{1.1m}".Invariant();
            var b = $"ab{1}cd{1.2}ef{1.1m}".Local("fr");
            var c = $"ab{"constant"}cd".Invariant();
            var d = $"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Invariant();
            var e = $"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Local("de");
            var f = $"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Local("fr");
            var x = 1;
            var g = $"ab{x}".Invariant();
            var h = $"ab{DateTime.Now}".Invariant();
            var y = "ja";
            var i = $"ab{1}".Local(y);
            var j = $"ab{1}".Local("xyz");
            var k = "abc".Invariant();

            """,
            [],
            ["CIG002", "CIG002", "CIG003", "CIG004", "CIG001"]);
    }
}

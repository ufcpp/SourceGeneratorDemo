using Generators;
using Xunit;

namespace UnitTests;

public class DependencyPropertyGeneratorTests
{
    [Fact]
    public void Generate()
    {
        Helpers.RunGenerator(
            new Generator(),
"""

""",
[
    new("filename", """

"""),
]);
    }
}

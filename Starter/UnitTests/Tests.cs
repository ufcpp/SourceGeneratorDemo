using Generators;
using Xunit;

namespace UnitTests;

public class Tests
{
    [Fact]
    public void Generate()
    {
        Helpers.RunGenerator(
            new Generator(),
"""

""",
[]);
    }
}

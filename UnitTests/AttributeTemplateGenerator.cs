using Generators;
using Xunit;

namespace UnitTests;

public class AttributeTemplateGeneratorTests
{
    [Fact]
    public void Generate()
    {
        Helpers.RunGenerator(
            new AttributeTemplateGenerator(),
            """"
            using AttributeTemplateGenerator;
            using T = MyTemplateAttribute;

            [MyTemplate(1, 2)]
            internal partial class Class1;
            
            [T(3, 5)]
            internal partial class Class2;
            
            internal class MyTemplateAttribute(int x, int y) : AttributeTemplateGenerator.TemplateAttribute
            {
                public string Body => $"""
                    public int X => {x};
                    public int Y => {y};
                """;

                public string Parent => $"""
                    public const string Id => "{x}.{y}";
                """;
            }

            """");
    }
}

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
"""""
using AttributeTemplateGenerator;
using T = TTemplateAttribute;

[T("// header")]
[ConstStr<double>("Pi", 3.14159, CultureName = "fr")]
internal partial class Class1;

internal partial class Class2
{
    [PTemplate(12)]
    public partial int X { get; }


    public partial class Inner
    {
        [MTemplate(34)]
        public partial int M(int a, int b);
    }
}

namespace A1
{
    namespace A2.A3
    {
        partial class C
        {
            partial struct S
            {
                partial record R
                {
                    partial record class RC
                    {
                        partial record struct RS
                        {
                            [MTemplate(56)]
                            public partial int M(int a, int b);
                        }
                    }
                }
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class TTemplateAttribute(string header) : TemplateAttribute(
$"""
public static readonly Type This = typeof({Type});
""",
Global(header));

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class ConstStrAttribute<T>(string name, T value) : TemplateAttribute(
$"""
public const string {name} = {value};
""");

[AttributeUsage(AttributeTargets.Property)]
internal class PTemplateAttribute(int n) : TemplateAttribute(
$"""
get => field + {n};
set => field = value - {n};
""",
Parent($"""
public const int {Name}Offset = {n};
"""));

[AttributeUsage(AttributeTargets.Method)]
internal class MTemplateAttribute(int n) : TemplateAttribute(
$"""
return {n} * {Name, 0} * {Name, 1};
""",
Down(1, "// comment"),
Up(2, $"""
// comment {Type} {Name}({Type,0} {Name,0}, {Type,1} {Name,1})
"""));

""""");
    }
}

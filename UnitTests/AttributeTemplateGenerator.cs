using Generators;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace UnitTests;

public class AttributeTemplateGeneratorTests
{
    private static readonly AttributeTemplateGenerator _generator = new();

    private static void Run(
        [StringSyntax("C#")] string targetSource,
        params GeneratedSource[] generatedSources)
        => Helpers.RunGenerator(_generator, targetSource, generatedSources);

    [Fact]
    public void Culture() => Run(
""""
using AttributeTemplateGenerator;

[ConstStr("Invariant", 1234.5)]
[ConstStr("De", 1234.5, CultureName = "de")]
[ConstStr("Fr", 1234.5, CultureName = "fr")]
internal partial class Class1;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class ConstStrAttribute(string name, double value) : TemplateAttribute(
$"""
public const string {name} = "{value:n1}";
""");

"""",
[
new("ATG_Class1", """
internal partial class Class1 {
public const string Invariant = "1,234.5";
public const string De = "1.234,5";
public const string Fr = "1 234,5";
}

"""),
]);

    [Fact]
    public void Alias() => Run(""""
using AttributeTemplateGenerator;
using B;
using T = B.TTemplateAttribute;

namespace A
{
    [B.TTemplate("x")]
    partial class X;

    [TTemplate("y")]
    partial class Y;

    [TTemplate("z")]
    partial class Z;
}

namespace B
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    internal class TTemplateAttribute(string id) : TemplateAttribute(
    $"""
    // generated code: {id}
    """);
}

"""", [
new("ATG_A.X","""
namespace A {
partial class X {
// generated code: x
}}

"""),
new("ATG_A.Y","""
namespace A {
partial class Y {
// generated code: y
}}

"""),
new("ATG_A.Z","""
namespace A {
partial class Z {
// generated code: z
}}

"""),
]);

    [Fact]
    public void MultipleTemplates() => Run(""""
using AttributeTemplateGenerator;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class AAttribute : TemplateAttribute(
$"""
// A
""");

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class BAttribute(string name) : TemplateAttribute(
$"""
// B: {name}
""");

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class CAttribute(int value) : TemplateAttribute(
$"""
// C: {value}
""");

[A, B("x"), C(1234)]
partial class Class1;

"""", [
new("ATG_Class1","""
partial class Class1 {
// A
// B: x
// C: 1234
}

"""),
]);

    [Fact]
    public void Literal() => Run(""""
class AAttribute : AttributeTemplateGenerator.TemplateAttribute(
"// \x61\u0061\U0000061\"",
@"// 1""",
"""
// 2
""",
$"""
// 3
"""
);

[A]
partial class Class1;

        
"""", [
new("ATG_Class1","""
partial class Class1 {
// aaa"
// 1"
// 2
// 3
}

"""),
]);

    [Fact]
    public void TemplateLevel() => Run(""""
using AttributeTemplateGenerator;

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
                            [A]
                            public partial int P { get; }
                        }
                    }
                }
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
internal class AAttribute : TemplateAttribute(
"get { return 0; }",
Parent("// Parent"),
Up(0, "// Up 0"),
Up(1, "// Up 1"),
Up(3, "// Up 3"),
Global("// Global"),
Down(0, "// Down 0"),
Down(1, "// Down 1"),
Down(3, "// Down 3")
);

"""", [
new("ATG_A1.A2.A3.C.S.R.RC.RS.P","""
// Global
// Down 0
namespace A1 {
// Down 1
namespace A2.A3 {
partial class C {
// Down 3
partial struct S {
// Up 3
partial record R {
partial record class RC {
// Up 1
partial record struct RS {
// Parent
// Up 0
public partial int P {
get { return 0; }
}}}}}}}}

"""),
]);

    [Fact]
    public void Generate()
    {
        Run(
"""""
using AttributeTemplateGenerator;
using T = TTemplateAttribute;

[T("// header")]
[ConstStr("Invariant", 1234.5)]
[ConstStr("De", 1234.5, CultureName = "de")]
[ConstStr("Fr", 1234.5, CultureName = "fr")]
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

[Unrelated]
internal partial class Unrelated
{
    [Unrelated]
    internal partial class A
    {
        [Unrelated]
        public partial int P { get; }
    }
}


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class TTemplateAttribute(string header) : TemplateAttribute(
$"""
public static readonly Type This = typeof({Name});
""",
Global(header));

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class ConstStrAttribute(string name, double value) : TemplateAttribute(
$"""
public const string {name} = "{value:n}";
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
// comment {Type} {Parent(Name)}.{Name}({Type,0} {Name,0}, {Type,1} {Name,1})
"""));

internal class UnrelatedAttribute : Attribute;

""""");
    }
}

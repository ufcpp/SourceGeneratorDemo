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
    public void IgnoreUnrelatedAttribute() => Run(""""

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

internal class UnrelatedAttribute : Attribute;

        
"""");

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
    public void ConstInterpolation() => Run(""""
class AAttribute : AttributeTemplateGenerator.TemplateAttribute($"// {1}{'2'}{"34"}");

[A]
partial class Class1;

        
"""", [
new("ATG_Class1","""
partial class Class1 {
// 1234
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
partial record RC {
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
    public void NameIntrinsic() => Run(""""
class AAttribute : AttributeTemplateGenerator.TemplateAttribute(
$"// {Name}"
);

[A]
partial class Class1
{
    [A]
    partial class Class2
    {
        [A]
        public partial int P { get; }
    }
}

        
"""", [
new("ATG_Class1","""
partial class Class1 {
// Class1
}

"""),
new("ATG_Class1.Class2","""
partial class Class1 {
partial class Class2 {
// Class2
}}

"""),
new("ATG_Class1.Class2.P","""
partial class Class1 {
partial class Class2 {
public partial int P {
// P
}}}

"""),
]);

    [Fact]
    public void TypeIntrinsic() => Run(""""
class AAttribute : AttributeTemplateGenerator.TemplateAttribute(
$"// {Type}"
);

partial class Class1
{
    [A]
    public partial int P { get; }

    [A]
    public partial string M();
}

        
"""", [
new("ATG_Class1.M()","""
partial class Class1 {
public partial string M() {
// string
}}

"""),
new("ATG_Class1.P","""
partial class Class1 {
public partial int P {
// int
}}

"""),
]);

    [Fact]
    public void LevelIntrinsic() => Run(""""
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
$"// {Name} {Parent(Name)} {Up(1,Name)} {Down(1, Name)}"
);

"""", [
new("ATG_A1.A2.A3.C.S.R.RC.RS.P","""
namespace A1 {
namespace A2.A3 {
partial class C {
partial struct S {
partial record R {
partial record RC {
partial record struct RS {
public partial int P {
// P RS RC A1
}}}}}}}}

"""),
]);

    [Fact]
    public void TemplateParameter() => Run(""""
[AttributeUsage(AttributeTargets.Property)]
internal class AAttribute(int n) : AttributeTemplateGenerator.TemplateAttribute(
$"""
get => field + {n};
set => field = value - {n};
""",
Parent($"""
public const int {Name}Offset = {n};
"""));

partial class Class1
{
    [A(1)]
    public partial int P { get; }

    [A(2)]
    public partial int Q { get; }
}

        
"""", [
new("ATG_Class1.P","""
partial class Class1 {
public const int POffset = 1;
public partial int P {
get => field + 1;
set => field = value - 1;
}}

"""),
new("ATG_Class1.Q","""
partial class Class1 {
public const int QOffset = 2;
public partial int Q {
get => field + 2;
set => field = value - 2;
}}

"""),
]);

    [Fact]
    public void ParameterIntrinsic()
    {
        Run(
"""""
class AAttribute(int n) : AttributeTemplateGenerator.TemplateAttribute($"""
// {Type,0} * {Type,1}
return {n}  * {Name, 0} * {Name, 1};
"""
);

partial class Class1
{
    [A(34)]
    public partial double M(int a, float b);
}

""""", [
new("ATG_Class1.M(int, float)", """
partial class Class1 {
public partial double M(int a, float b) {
// int * float
return 34  * a * b;
}}

"""),
]);
    }

#if false
    [Fact]
    public void X() => Run(""""
class AAttribute : AttributeTemplateGenerator.TemplateAttribute(
);

[A]
partial class Class1;

        
"""", [
new("ATG_Class1","""
partial class Class1 {
}

"""),
]);
#endif
}

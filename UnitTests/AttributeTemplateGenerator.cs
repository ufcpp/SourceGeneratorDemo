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

    private static void Run(
        [StringSyntax("C#")] string targetSource,
        GeneratedSource[] generatedSources,
        string[] errorIds)
        => Helpers.RunGenerator(_generator, targetSource, generatedSources, errorIds);

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
internal class AAttribute() : TemplateAttribute(
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
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
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
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute($"// {1}{'2'}{"34"}");

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
    public void RefersConstMember() => Run(""""
class Constants
{
    public const int N = 1;
}

class AAttribute() : AttributeTemplateGenerator.TemplateAttribute($"// {Constants.N}{C}{S}")
{
    public const char C = '2';
    public const string S = "34";
}

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
    public void FormatClause() => Run(
""""
class AAttribute(double value) : AttributeTemplateGenerator.TemplateAttribute(
$"""
// ({value:n1})
// ({value,8:n1})
// ({value,-8:n1})
// ({value:000.000})
// ({value:e2})
""");

[A(12.34)]
internal partial class Class1;

"""",
[
new("ATG_Class1", """
internal partial class Class1 {
// (12.3)
// (    12.3)
// (12.3    )
// (012.340)
// (1.23e+001)
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
internal class AAttribute() : TemplateAttribute(
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
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
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
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
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
internal class AAttribute() : TemplateAttribute(
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
// ({Param(0, Type),8}/{Param(0, Name),-8})
return {n} * {Param(0, Name)} * {Param(1, Name)};
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
// (     int/a       )
return 34 * a * b;
}}

"""),
]);
    }

    [Fact]
    public void PrimaryConstructorParameter()
    {
        Run(
"""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute($"""
// {Up(1, Param(0, Type))}/{Up(1, Param(0, Name))}
// {Parent(Param(0, Type))}/{Parent(Param(0, Name))}
// {Param(0, Type)}/{Param(0, Name)}
"""
);

partial class X(bool x)
{
    partial class Y(string y)
    {
        [A]
        partial void Z(int z);
    }
}

""""", [
new("ATG_X.Y.Z(int)", """
partial class X {
partial class Y {
partial void Z(int z) {
// bool/x
// string/y
// int/z
}}}

"""),
]);
    }

    [Fact]
    public void Cast() => Run(""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
$"// {(byte)'c'}, {(System.UInt16)123}, {(decimal)1.2}, {(Char)0x61}"
);

[A]
partial class Class1;

        
"""", [
new("ATG_Class1","""
partial class Class1 {
// 99, 123, 1.2, a
}

"""),
]);

    [Fact]
    public void ArithmeticOperators() => Run(""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
$"// {(byte)('a' + 2)}, {(System.UInt16)(100 + 30 - 7)}, {-(decimal)-1.2}, {(Char)(0x60 + (10 / 5 - 1))}"
);

[A]
partial class Class1;

        
"""", [
new("ATG_Class1","""
partial class Class1 {
// 99, 123, 1.2, a
}

"""),
]);

    [Fact]
    public void StringPlus() => Run(""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
$"// {"a" + 1.2}{'b' + "c"}",
Parent("//" + 1.2)
);

[A]
[A(CultureName = "de")]
[A(CultureName = "fr")]
partial class Class1;

        
"""", [
new("ATG_Class1","""
//1.2
//1,2
//1,2
partial class Class1 {
// a1.2bc
// a1,2bc
// a1,2bc
}

"""),
]);

    [Fact]
    public void LogicalOperator() => Run(""""
class AAttribute(bool a, bool b) : AttributeTemplateGenerator.TemplateAttribute(
$"// {a} {!a} {a & b} {a && b} {a | b} {a || b}"
);

[A(false, false)]
[A(true, false)]
[A(false, true)]
[A(true, true)]
partial class Class1;


"""", [
new("ATG_Class1","""
partial class Class1 {
// False True False False False False
// True False False False True True
// False True False False True True
// True False True True True True
}

"""),
]);

    [Fact]
    public void BitwiseOperator() => Run(""""
class AAttribute(byte a, byte b) : AttributeTemplateGenerator.TemplateAttribute(
$"// {a} {~a} {a & b} {a | b}"
);

[A(1, 2)]
[A(2, 5)]
[A(7, 10)]
partial class Class1;


"""", [
new("ATG_Class1","""
partial class Class1 {
// 1 -2 0 3
// 2 -3 0 7
// 7 -8 2 15
}

"""),
]);

    [Fact]
    public void ConditionalOperator() => Run(""""
class AAttribute(bool condition, int a, int b) : AttributeTemplateGenerator.TemplateAttribute(
$"// {(condition ? a : b)}"
);

[A(true, 1, 2)]
[A(false, 10, 20)]
partial class Class1;


"""", [
new("ATG_Class1","""
partial class Class1 {
// 1
// 20
}

"""),
]);

    [Fact]
    public void UnsupportedExpression()
    {
        Run(""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
$"""{""[0]}"""
);

"""", [], ["ATG001"]);
        Run(""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
$"""{""[string.Empty]}"""
);

"""", [], ["ATG001"]);
        Run(""""
class AAttribute(int x) : AttributeTemplateGenerator.TemplateAttribute(
$"""{""[&x]}"""
);

"""", [], ["ATG001"]);
    }

    [Fact]
    public void GenericAttribute() => Run(""""
class AAttribute<T>(T x) : AttributeTemplateGenerator.TemplateAttribute(
$"// {x}"
);

[A<int>(1)]
partial class Class1;


"""", [
new("ATG_Class1","""
partial class Class1 {
// 1
}

"""),
]);

    [Fact]
    public void UnknownCultureName()
    {
        Run(""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute();

[A(CultureName = "non")]
partial class Class1;

"""", [], ["ATG002"]);

        Run(""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute();

[A(CultureName = "awqsedrftgyh")]
partial class Class1;

"""", [], ["ATG002"]);
    }

#if false
    [Fact]
    public void X() => Run(""""
class AAttribute() : AttributeTemplateGenerator.TemplateAttribute(
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

    //todo: erroneous case
    // class AAttribute : TemplateAttribute($"template"); // correctly: class AAttribute() <- paren needed
}

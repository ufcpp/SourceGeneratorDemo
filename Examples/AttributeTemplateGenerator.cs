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
    public partial int X { get; set; }


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
public static readonly Type This = typeof({Name});
""",
Global(header));

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
internal class ConstStrAttribute(string name, double value) : TemplateAttribute(
$"""
public const string {name} = "{value}";
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
return {n} * {Param[0].Name} * {Param[1].Name};
""",
Ancestor[^1]("// comment"),
Ancestor[3]($"""
// comment {Type} {Name}({Param[0].Type} {Param[0].Name}, {Param[1].Type} {Param[1].Name})
"""));

#if false
internal class AAttribute() : TemplateAttribute(
    $"""{""[0]}"""
    );

internal class FirstElementAttribute(int[] array) : TemplateAttribute($"// {array[0]}");

[FirstElement([1])]
partial class SingleElementArray;

[FirstElement([])]
partial class EmptyArray;
#endif

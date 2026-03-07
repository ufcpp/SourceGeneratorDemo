using AttributeTemplateGenerator;

internal partial class ParameterizedTemplate
{
    [PTemplate(12)]
    public partial int X { get; set; }


    public partial class Inner
    {
        [MTemplate(34)]
        public partial int M(int a, int b);
    }
}

[AttributeUsage(AttributeTargets.Property)]
file class PTemplateAttribute(int n) : TemplateAttribute(
$"""
get => field + {n};
set => field = value - {n};
""",
Parent($"""
public const int {Name}Offset = {n};
"""));

[AttributeUsage(AttributeTargets.Method)]
file class MTemplateAttribute(int n) : TemplateAttribute(
$"""
return {n} * {Param[0].Name} * {Param[1].Name};
""",
Ancestor[^1]("// comment"),
Ancestor[3]($"""
// comment {Type} {Name}({Param[0].Type} {Param[0].Name}, {Param[1].Type} {Param[1].Name})
"""));

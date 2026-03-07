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
                            [MyTemplate(56)]
                            public readonly partial int M(int a, int b);
                        }
                    }
                }
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
file class MyTemplateAttribute(int n) : AttributeTemplateGenerator.TemplateAttribute(
$"""
return {n} * {Param[0].Name} * {Param[1].Name};
""",
Ancestor[^1]("// comment"),
Ancestor[3]($"""
// comment {Type} {Name}({Param[0].Type} {Param[0].Name}, {Param[1].Type} {Param[1].Name})
"""));

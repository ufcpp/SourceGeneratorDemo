[AttributeUsage(AttributeTargets.Class)]
class StringEnumAttribute(string[] names) : AttributeTemplateGenerator.TemplateAttribute(
$"{from name in names select $"    public const string {name} = nameof({name});"}"
);

[StringEnum(["X", "Abc", "AbdXyz"])]
partial class Strings;

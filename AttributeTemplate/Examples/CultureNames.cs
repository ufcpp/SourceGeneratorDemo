using AttributeTemplateGenerator;

namespace CultureNames;

[ConstStr("Invariant", 1234.5)]
[ConstStr("De", 1234.5, CultureName = "de")]
[ConstStr("Fr", 1234.5, CultureName = "fr")]
internal partial class Class1;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
internal class ConstStrAttribute(string name, double value) : TemplateAttribute(
$"""
public const string {name} = "{value}";
""");

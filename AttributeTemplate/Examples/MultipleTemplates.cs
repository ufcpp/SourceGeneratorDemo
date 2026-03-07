using AttributeTemplateGenerator;

namespace MultipleTemplates;

using T = TTemplateAttribute;

[T("// comment 1")]
[TTemplate("// comment 2")]
[TTemplateAttribute("// comment 3")]
[MultipleTemplates.TTemplateAttribute("// comment 4")]
[global::MultipleTemplates.TTemplateAttribute("// comment 5")]
partial class X;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal class TTemplateAttribute(string header) : TemplateAttribute(Global(header));

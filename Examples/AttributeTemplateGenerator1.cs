using AttributeTemplateGenerator;
using System.Windows;

namespace Examples
{
    public partial class DependencyPropertyAttributeTemplateExample : DependencyObject
    {
        [DependencyPropertyTemplate]
        public partial int X { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class DependencyPropertyTemplateAttribute() : TemplateAttribute(
    $"""
    get => ({Type})GetValue({Name}Property);
    set => SetValue({Name}Property, value);
""",
    Parent($"""
public static readonly DependencyProperty {Name}Property =
    DependencyProperty.Register(
        nameof({Name}),
        typeof({Type}),
        typeof({Parent(Name)}),
        new PropertyMetadata(default({Type})));

"""),
    Global("""
using System.Windows;

"""));
}

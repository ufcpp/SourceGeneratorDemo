using AttributeTemplateGenerator;

namespace Examples;

[NotifyClass]
partial class NotifyPropertyChangedExample
{
    [NotifyProperty]
    public partial int Integer { get; set; }

    [NotifyProperty]
    public partial string? Str { get; set; }

    [NotifyProperty]
    public partial TimeOnly Time { get; set; }

    [NotifyProperty]
    public partial DateOnly? Date { get; set; }

    [NotifyProperty]
    public partial (int, string?, DateTimeOffset) Tuple { get; set; }

    [NotifyProperty]
    public partial List<int> List { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
class NotifyClassAttribute() : TemplateAttribute(
    Global("using System.ComponentModel;"),
    Parent($"partial class {Name} : INotifyPropertyChanged;"),
"""
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }
    
    protected bool SetProperty<T>(ref T storage, T value, PropertyChangedEventArgs e)
    {
        if (global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }
        storage = value;
        OnPropertyChanged(e);
        return true;
    }
"""
    );

[AttributeUsage(AttributeTargets.Property)]
class NotifyPropertyAttribute() : TemplateAttribute(
$"""
    get => field;
    set => SetProperty(ref field, value, {Name}PropertyChangedEventArgs);
""",
Parent($"""
    private static readonly System.ComponentModel.PropertyChangedEventArgs {Name}PropertyChangedEventArgs = new(nameof({Name}));
""")
    );

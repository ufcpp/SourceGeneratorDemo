using System.ComponentModel;
using INPC = NotifyPropertyChangedGenerator.NotifyPropertyChangedAttribute;

namespace ClassLibrary1;

public partial class NotifyPropertyChangedExample
{
    [INPC]
    public partial int Integer { get; set; }

    [INPC]
    public partial string? Str { get; set; }

    [INPC]
    public partial TimeOnly Time { get; set; }

    [INPC]
    public partial DateOnly? Date { get; set; }

    [INPC]
    public partial (int, string?, DateTimeOffset) Tuple { get; set; }

    [INPC]
    public partial List<int> List { get; set; }
}

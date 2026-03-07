using System.Windows;
using DP = DependencyPropertyGenerator.DependencyPropertyAttribute;

namespace ClassLibrary1;

public partial class DependencyPropertyExample : DependencyObject
{
    [DP]
    public partial int Integer { get; set; }

    [DP]
    public partial string? Str { get; set; }

    [DP]
    public partial TimeOnly Time { get; set; }

    [DP]
    public partial DateOnly? Date { get; set; }

    [DP]
    public partial (int, string?, DateTimeOffset) Tuple { get; set; }

    [DP]
    public partial List<int> List { get; set; }
}

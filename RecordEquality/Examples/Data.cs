using RecordEqualityGenerator;

namespace Examples;

partial record Data
{
    public int Id { get; init; }

    [SequenceKey]
    public int[]? Values { get; init; }

    [IgnoreKey]
    public string? Name { get; init; }
}

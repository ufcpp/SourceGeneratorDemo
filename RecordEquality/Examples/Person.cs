namespace RecordEqualityGenerator;

// Only Name is used for equality
partial record Person
{
    [ExplicitKey]
    public required string Name { get; init; }

    public int Id { get; init; }
    public int Age { get; init; }
}

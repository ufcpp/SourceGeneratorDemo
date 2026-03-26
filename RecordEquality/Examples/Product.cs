namespace RecordEqualityGenerator;

// Both Id and SKU are used for equality
partial record Product
{
    [ExplicitKey]
    public int Id { get; init; }

    [ExplicitKey]
    public required string SKU { get; init; }

    public required string Name { get; init; }
    public decimal Price { get; init; }
}

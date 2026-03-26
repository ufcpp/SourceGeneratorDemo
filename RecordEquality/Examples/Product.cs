namespace RecordEqualityGenerator;

// Both Id and SKU are used for equality
partial record Product
{
    [EqualityKey]
    public int Id { get; init; }

    [EqualityKey]
    public required string SKU { get; init; }

    public required string Name { get; init; }
    public decimal Price { get; init; }
}

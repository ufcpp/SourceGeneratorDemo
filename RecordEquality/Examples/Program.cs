using RecordEqualityGenerator;
using Examples;

// Example: Person record with Name as equality key
var person1 = new Person { Id = 1, Name = "Alice", Age = 30 };
var person2 = new Person { Id = 2, Name = "Alice", Age = 25 };
var person3 = new Person { Id = 1, Name = "Bob", Age = 30 };

Console.WriteLine($"person1.Equals(person2): {person1.Equals(person2)}"); // True (same Name)
Console.WriteLine($"person1.Equals(person3): {person1.Equals(person3)}"); // False (different Name)
Console.WriteLine($"person1 == person2: {person1 == person2}");           // True
Console.WriteLine($"person1.GetHashCode() == person2.GetHashCode(): {person1.GetHashCode() == person2.GetHashCode()}"); // True

// Example: Product with both Id and SKU as equality keys
var product1 = new Product { Id = 1, SKU = "ABC123", Name = "Widget", Price = 10.99m };
var product2 = new Product { Id = 1, SKU = "ABC123", Name = "Different Widget", Price = 15.99m };
var product3 = new Product { Id = 2, SKU = "ABC123", Name = "Widget", Price = 10.99m };

Console.WriteLine($"product1.Equals(product2): {product1.Equals(product2)}"); // True (same Id and SKU)
Console.WriteLine($"product1.Equals(product3): {product1.Equals(product3)}"); // False (different Id)

// Example: Generic Pair types
Console.WriteLine("\n--- Pair examples ---");

var pair1 = new Pair(1, "A", "Other1");
var pair2 = new Pair(1, "A", "Other2");
var pair3 = new Pair(2, "A", "Other1");
Console.WriteLine($"pair1.Equals(pair2): {pair1.Equals(pair2)}"); // True (same X and Y)
Console.WriteLine($"pair1.Equals(pair3): {pair1.Equals(pair3)}"); // False (different X)

var pairT1 = new Pair<int>(1, "A", "Other1");
var pairT2 = new Pair<int>(1, "A", "Other2");
var pairT3 = new Pair<int>(2, "A", "Other1");
Console.WriteLine($"pairT1.Equals(pairT2): {pairT1.Equals(pairT2)}"); // True (same X and Y)
Console.WriteLine($"pairT1.Equals(pairT3): {pairT1.Equals(pairT3)}"); // False (different X)

var pairT2_1 = new Pair<int, string>(1, "A", "Other1");
var pairT2_2 = new Pair<int, string>(1, "A", "Other2");
var pairT2_3 = new Pair<int, string>(1, "B", "Other1");
Console.WriteLine($"pairT2_1.Equals(pairT2_2): {pairT2_1.Equals(pairT2_2)}"); // True (same X and Y)
Console.WriteLine($"pairT2_1.Equals(pairT2_3): {pairT2_1.Equals(pairT2_3)}"); // False (different Y)


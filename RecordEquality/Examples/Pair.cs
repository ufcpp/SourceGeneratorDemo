using RecordEqualityGenerator;

namespace Examples;

partial record Pair(
    [property: EqualityKey] int X,
    [property: EqualityKey] string Y,
    string Other);

partial record Pair<T>(
    [property: EqualityKey] T X,
    [property: EqualityKey] string Y,
    string Other);

partial record Pair<T1, T2>(
    [property: EqualityKey] T1 X,
    [property: EqualityKey] T2 Y,
    string Other);

using RecordEqualityGenerator;

namespace Examples;

partial record Pair(
    [property: ExplicitKey] int X,
    [property: ExplicitKey] string Y,
    string Other);

partial record Pair<T>(
    [property: ExplicitKey] T X,
    [property: ExplicitKey] string Y,
    string Other);

partial record Pair<T1, T2>(
    [property: ExplicitKey] T1 X,
    [property: ExplicitKey] T2 Y,
    string Other);

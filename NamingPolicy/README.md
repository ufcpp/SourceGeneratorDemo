# Naming Policy Generator

This project demonstrates a Source Generator that uses **Interceptors** to convert string literals between different naming conventions at compile-time.

Converting between naming conventions (e.g., `camelCase`, `snake_case`, `kebab-case`) is a common task in software development, especially when working with APIs, databases, or configuration files. This generator uses the Interceptor feature to perform these conversions at compile-time for constant strings, eliminating runtime overhead.

## Supported Naming Conventions

The generator provides the following extension methods:

- **`CamelCase()`** - Converts to camelCase (e.g., `myVariableName`)
- **`PascalCase()`** - Converts to PascalCase (e.g., `MyVariableName`)
- **`SnakeCaseLower()`** - Converts to snake_case (e.g., `my_variable_name`)
- **`SnakeCaseUpper()`** - Converts to SNAKE_CASE (e.g., `MY_VARIABLE_NAME`)
- **`KebabCaseLower()`** - Converts to kebab-case (e.g., `my-variable-name`)
- **`KebabCaseUpper()`** - Converts to KEBAB-CASE (e.g., `MY-VARIABLE-NAME`)

## Usage

Add the generator to your project and use the extension methods on constant strings:

```csharp
using NamingPolicyGenerator;

// Convert to different naming conventions
var x = "abc-def".CamelCase();                  // abcDef
var y = "ABC-DEF".SnakeCaseLower();             // abc_def
var z = "AbcDef".KebabCaseUpper();              // ABC-DEF

// Works with const strings
const string s = "ABC-DEF";
var result = s.SnakeCaseLower();                // abc_def

// Works with verbatim and raw strings
var a = @"verbatim""string".PascalCase();
var b = """
        raw string
        """.KebabCaseLower();

// Works with interpolated strings containing only const strings
var c = $"string interpolation with {"const string"}".SnakeCaseLower();
```

## Generated Code

The generator creates interceptors that replace the method calls with pre-converted constant strings:

```csharp
// For: "abc-def".CamelCase()
namespace Interceptors.Generated
{
    internal static partial class NamingPolicy
    {
        [System.Runtime.CompilerServices.InterceptsLocation(1, "...")]
        internal static string _A1B2C3D4(this string _) => """
abcDef
""";
    }
}

// For: "ABC-DEF".SnakeCaseLower()
namespace Interceptors.Generated
{
    internal static partial class NamingPolicy
    {
        [System.Runtime.CompilerServices.InterceptsLocation(1, "...")]
        internal static string _E5F6G7H8(this string _) => """
abc_def
""";
    }
}
```

## How It Works

This generator leverages C#'s Interceptor feature:

1. The generator analyzes method calls like `.CamelCase()`, `.SnakeCaseLower()`, etc.
2. It verifies that the string expression is a compile-time constant
3. It converts the string to the target naming convention at compile-time
4. It generates an interceptor method marked with `[InterceptsLocation]` that returns the converted string
5. The compiler redirects the original method call to the interceptor

## Known Limitations

Due to current limitations in the Interceptor feature:

1. **Dead code remains in the compiled output**: The original string literal used for code generation remains in the compiled assembly, resulting in unnecessary code bloat.

2. **Not recognized as compile-time constants**: The generated interceptor method simply returns a `const string`, but there is no mechanism for the call site to recognize this as a constant expression. This means the result cannot be used in contexts that require constant expressions (e.g., attribute arguments).

These limitations affect all similar use cases where compile-time evaluation replaces runtime computation.

## References

- [Interceptors Documentation](https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md) - Official C# Interceptors feature documentation
- [Source Generators Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) - Overview of Source Generators in C#

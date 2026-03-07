# Constant Interpolation Generator

This project demonstrates a Source Generator that uses **Interceptors** to convert interpolated strings with constant expressions into compile-time constant strings.

In C#, interpolated strings containing only `const string` components are compile-time constants. However, the interpolated string is evaluated at runtime even if those values are constants of primitive types like `int` or `double`. This is because the formatting is culture-dependent in .NET. This generator uses the Interceptor feature to pre-compute these culture-specific formatted strings at compile-time. Many developers have encountered this limitation, and this project demonstrates one approach to address it using Interceptors and Source Generators.

The generator provides two extension methods:
- **`Invariant()`** - Uses invariant culture for formatting
- **`Local(cultureName)`** - Uses a specific culture for formatting

## Usage

Add the generator to your project and use the extension methods on interpolated strings containing only constant expressions:

```csharp
using ConstantInterpolationGenerator;

// Invariant culture formatting
var a = $"ab{1}cd{1.2}ef{1.1m}".Invariant();

// Culture-specific formatting
var b = $"ab{1}cd{1.2}ef{1.1m}".Local("fr");

// With format specifiers
var c = $"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Invariant();
var d = $"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Local("de");
```

## Generated Code

The generator creates interceptors that replace the method calls with constant string literals:

```csharp
// For: $"ab{1}cd{1.2}ef{1.1m}".Invariant()
namespace Interceptors.Generated
{
    internal static partial class ConstantInterpolation
    {
        [System.Runtime.CompilerServices.InterceptsLocation(1, "...")]
        internal static string _5A96EE59(this string _) => """
ab1cd1.2ef1.1
""";
    }
}

// For: $"ab{1}cd{1.2}ef{1.1m}".Local("fr")
namespace Interceptors.Generated
{
    internal static partial class ConstantInterpolation
    {
        [System.Runtime.CompilerServices.InterceptsLocation(1, "...")]
        internal static string _97171565(this string _, string _1) => """
ab1cd1,2ef1,1
""";
    }
}

// For: $"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Invariant()
namespace Interceptors.Generated
{
    internal static partial class ConstantInterpolation
    {
        [System.Runtime.CompilerServices.InterceptsLocation(1, "...")]
        internal static string _2F931D28(this string _) => """
/  1,234.5/1,234.5  //1,234.5/
""";
    }
}
```

## How It Works

This generator leverages C#'s Interceptor feature:

1. The generator analyzes interpolated string expressions followed by `.Invariant()` or `.Local(cultureName)` calls
2. It verifies that all interpolation holes contain constant expressions
3. It evaluates the interpolated string at compile-time using the specified culture
4. It generates an interceptor method marked with `[InterceptsLocation]` that returns the pre-computed constant string
5. At compile-time, the compiler redirects the original method call to the interceptor, eliminating runtime overhead entirely

## Known Limitations

Due to current limitations in the Interceptor feature:

1. **Dead code remains in the compiled output**: The original method arguments used for code generation remain in the compiled assembly. Even though the generator produces a constant string, the interpolated string's `AppendLiteral`/`AppendFormatted` operations are still present in the IL. This means:
   - The pre-computed constant string is used at runtime (zero cost)
   - However, the original interpolated string expression still exists in the compiled assembly
       - The actual runtime cost is not zero
   - This results in unnecessary code bloat in the final binary

2. **Not recognized as compile-time constants**: The generated interceptor method simply returns a `const string`, but there is no mechanism for the call site to recognize this as a constant expression. This means:
   - The result cannot be used in contexts that require constant expressions (e.g., attribute arguments)

These limitations affect all similar use cases where compile-time evaluation replaces runtime computation.

## References

- [Interceptors Documentation](https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md) - Official C# Interceptors feature documentation
- [Source Generators Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) - Overview of Source Generators in C#

# .NET 8 Source Generators

## Overview

This project demonstrates Source Generators targeting .NET 8, leveraging modern .NET features and APIs.

## Why .NET 8 Instead of .NET Standard 2.0?

Historically, Source Generators were recommended to target .NET Standard 2.0. This recommendation existed because **Visual Studio for Windows ran on the .NET Framework**, and analyzers/generators executed in-process within the Visual Studio host. This meant they were constrained by Visual Studio's .NET Framework runtime limitations.

### What Changed in Recent Visual Studio Versions

Modern versions of Visual Studio have introduced significant architectural changes for C# Analyzers and Source Generators:

1. **Out-of-Process Execution**: Analyzers and generators now run in a separate process, independent from the Visual Studio host
2. **Modern .NET Runtime**: This separate process executes on a modern .NET runtime (currently .NET 8) rather than .NET Framework

These changes mean:

- Analyzers/generators are no longer constrained by the Visual Studio host's .NET Framework limitations
- Source Generators can leverage modern .NET APIs and features
- Better performance and access to the latest runtime optimizations

> **Note**: The specific version requirements may vary. Verify that your development environment supports .NET 8 runtime for analyzers before targeting .NET 8 in your Source Generator projects.

### Benefits of Targeting .NET 8

By targeting .NET 8 directly, Source Generators can leverage:

- **Modern APIs**: Access to APIs not available in .NET Standard 2.0
  - Example: `DefaultInterpolatedStringHandler` for efficient string building
- **Better Performance**: Runtime optimizations and newer BCL implementations
- **Modern C# Features**: Full support for latest C# language features
- **Simplified Code**: No need for compatibility shims or workarounds

### Trade-offs

**Advantages:**
- Access to modern .NET 8 APIs and performance improvements
- Cleaner, more maintainable code
- Better developer experience

**Limitations:**
- Requires a modern development environment with out-of-process analyzer support
- Not compatible with older development environments still using .NET Framework-based hosts
- Verify your tooling supports .NET 8 runtime for analyzers before adopting

> **Note**: Targeting .NET 8 for Source Generators may trigger **RS1041** warning (analyzer compatibility warning). This warning is suppressed in `.editorconfig` for this project, as it's expected behavior when intentionally targeting modern .NET versions.

## Example: ConstantInterpolationGenerator

This project includes `ConstantInterpolationGenerator`, which is a **port from the `Generators` project** (targeting .NET Standard 2.0). 

Both implementations provide the same functionality, but this .NET 8 version leverages modern APIs like `DefaultInterpolatedStringHandler` that aren't available in .NET Standard 2.0, resulting in better performance and more maintainable code.

## References

- [Visual Studio 2022 Architecture Changes](https://devblogs.microsoft.com/visualstudio/visual-studio-2022/)
- [Source Generators Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)

# Source Generator Demo

Demonstrating that Source Generators aren't so hard.

## Concept

This repository is built around the concept of **"Source Generators Are Approachable"**. It stems from a recurring conversation in the [csharplang repository](https://github.com/dotnet/csharplang):

> **Developer**: "I want macros!"
> 
> **Response**: "Use Source Generators."
> 
> **Developer**: "That's too hard. Having to split into separate projects is tough. I want to write templates in the same project."
> 
> **Response**: "Just write one attribute-based Source Generator at the start, and use it to define your templates."
> 
> **Developer**: "If it's so easy, show me a working example."
> 
> **Response**: "I just wrote one." *(this repository)*

## Repository Structure

Each project in this repository follows a consistent three-folder structure:

- **Generators** - The Source Generator implementation
- **Examples** - Sample code demonstrating the generator's usage
- **UnitTests** - Tests for the generator

This structure makes it easy to understand how each generator works and how to use it.

**Note:** These projects are designed to be referenced via `<ProjectReference>`, not published as NuGet packages. The Examples projects demonstrate the typical usage pattern:

```xml
<ProjectReference Include="..\Generators\Generators.csproj" 
                  OutputItemType="Analyzer" 
                  ReferenceOutputAssembly="false" />
```

## Projects

### [AttributeTemplate](./AttributeTemplate/README.md)

This is the "attribute-based Source Generator" mentioned in the Concept section—one generator that enables you to define templates using custom attributes. **Templates live in your application code**, not in separate projects.

**Features:**
- Template expressions with interpolation: `$"{Name}"`, `$"{Type}"`, `$"{Param[0].Name}"`
- Hierarchical code generation: `Parent(...)`, `Ancestor[n](...)`, `Global(...)`
- Culture-specific formatting
- Some C# expression support in templates

### [DependencyProperty](./DependencyProperty/README.md)

Two generators for WPF/MVVM development:
1. **DependencyPropertyGenerator** - Generates WPF DependencyProperty boilerplate
2. **NotifyPropertyChangedGenerator** - Generates `INotifyPropertyChanged` implementation

### [ConstantInterpolation](./ConstantInterpolation/README.md)

Uses **Interceptors** to convert interpolated strings with constant expressions into compile-time constants with culture-specific formatting.

**Features:**
- `.Invariant()` - Invariant culture formatting
- `.Local(cultureName)` - Culture-specific formatting

### [NamingPolicy](./NamingPolicy/README.md)

Uses **Interceptors** to convert between naming conventions (camelCase, snake_case, kebab-case, etc.) at compile-time.

**Supported conventions:**
- `CamelCase()`, `PascalCase()`
- `SnakeCaseLower()`, `SnakeCaseUpper()`
- `KebabCaseLower()`, `KebabCaseUpper()`

### [VersionInfo](./VersionInfo/README.md)

A diagnostic analyzer that reports your analyzer runtime environment. Use `#error version` to see:
- Compiler version (Roslyn version)
- Runtime version (.NET version)
- Operating system

### [Net8.0](./Net8.0/README.md)

Demonstrates Source Generators targeting **.NET 8** instead of .NET Standard 2.0.

Modern Visual Studio runs analyzers out-of-process on .NET 8, enabling generators to leverage modern .NET APIs and performance improvements.

### [Starter](./Starter/README.md)

A minimal starter template for creating new Source Generators.

Copy this folder to begin developing your own generator.

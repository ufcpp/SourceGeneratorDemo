# Verifying Analyzer Runtime Environment

The `Generators` project includes `VersionInfoAnalyzer`, a diagnostic analyzer that helps you verify which runtime environment your analyzers are running on.

> **Note**: Despite the project name "Generators", this project contains only an **Analyzer** (not a Source Generator). The project was initially created as a copy of the Starter folder, hence the naming.

## Why This is Useful

When developing Source Generators or Analyzers, it's important to know which runtime environment they're executing in. This information helps you:

- Verify that your development environment runs analyzers on the expected runtime
- Understand the capabilities and limitations of your tooling
- Make informed decisions about which .NET APIs you can use in your analyzers

## How to Use

Add the following directive to any C# file in your project:

```csharp
#error version
```

When you build, this will trigger a diagnostic error (VER001) that displays your analyzer runtime environment:

- **Compiler version** (Roslyn version) - from `typeof(Compilation).Assembly.GetName().Version`
- **Runtime** (e.g., ".NET 8.0.24", ".NET 10.0.3") - from `RuntimeInformation.FrameworkDescription`
- **Operating System** - from `RuntimeInformation.OSDescription`

The key information here is the **Runtime**, which tells you exactly which .NET runtime your analyzer is executing on. This technique is inspired by Roslyn's own version reporting mechanism.

## Example Output

```
Error VER001: Build Environment Information
Compiler: 4.8.0.0, Runtime: .NET 8.0.24, OS: Microsoft Windows 10.0.22631
```

## Verified Environments

As of this writing, here are some verified runtime environments:

- **Visual Studio 2026**: Runs analyzers on .NET 8.0.24
- **VS Code with C# Dev Kit**: Runs analyzers on .NET 10.0.3

Use this analyzer to verify your own tooling setup before deciding which .NET version to target in your Source Generators and Analyzers.

## Related

For an example of leveraging modern .NET runtime capabilities in Source Generators, see [Net8Generators](../Net8Generators/README.md).

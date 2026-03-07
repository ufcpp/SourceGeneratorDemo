# Attribute Template Generator

This project demonstrates a Source Generator that enables **compile-time code generation** using custom attributes with template expressions. The generator evaluates template expressions at compile-time and generates partial member implementations based on attribute parameters.

Attribute-based code generation is useful when you need to generate repetitive boilerplate code (property implementations, method bodies, constants) based on metadata. This generator uses C# expressions in attribute parameters to define templates, providing a type-safe and refactoring-friendly approach to code generation.

## Features

The generator supports:

- **Template expressions** with interpolation: `$"{Name}"`, `$"{Type}"`, `$"{Param[0].Name}"`
- **Hierarchical intrinsics**: `Parent(...)`, `Ancestor[n](...)`, `Global(...)`
- **Arithmetic and logical operators**: `+`, `-`, `*`, `/`, `%`, `&`, `|`, `!`, `~`
- **Type casting**: `(byte)value`, `(int)expression`
- **Conditional expressions**: `condition ? whenTrue : whenFalse`
- **Array operations**: Array literals, indexing (`array[0]`), string indexing (`"abc"[1]`)
- **Query expressions**: `from x in array select expression`
- **Format specifiers**: `{value:n2}`, `{value,10}`, `{value,-8:x}`
- **Culture-specific formatting**: `CultureName = "fr"`
- **Multiple template levels**: Generate code at different hierarchy levels

## Usage

### Basic Template

```csharp
using AttributeTemplateGenerator;

[AttributeUsage(AttributeTargets.Class)]
internal class TTemplateAttribute(string header) : TemplateAttribute(
    $"public static readonly Type This = typeof({Name});"
);

[TTemplate("// Generated from template")]
partial class MyClass;
```

**Generated code:**
```csharp
partial class MyClass {
    public static readonly Type This = typeof(MyClass);
}
```

### Property Template with Parameters

```csharp
[AttributeUsage(AttributeTargets.Property)]
internal class OffsetPropertyAttribute(int offset) : TemplateAttribute(
    $"""
    get => field + {offset};
    set => field = value - {offset};
    """,
    Parent($"public const int {Name}Offset = {offset};")
);

partial class Data
{
    [OffsetProperty(100)]
    public partial int X { get; set; }

    [OffsetProperty(200)]
    public partial int Y { get; set; }
}
```

**Generated code:**
```csharp
partial class Data {
    public const int XOffset = 100;
    public const int YOffset = 200;
    public partial int X {
        get => field + 100;
        set => field = value - 100;
    }
    public partial int Y {
        get => field + 200;
        set => field = value - 200;
    }
}
```

### Method Template with Parameter Intrinsics

```csharp
[AttributeUsage(AttributeTargets.Method)]
internal class MultiplyAttribute(int factor) : TemplateAttribute(
    $"return {factor} * {Param[0].Name} * {Param[1].Name};"
);

partial class Calculator
{
    [Multiply(10)]
    public partial int Calc(int a, int b);
}
```

**Generated code:**
```csharp
partial class Calculator {
    public partial int Calc(int a, int b) {
        return 10 * a * b;
    }
}
```

### Hierarchical Templates

```csharp
[AttributeUsage(AttributeTargets.Property)]
internal class LevelDemoAttribute() : TemplateAttribute(
    "get => 0;",
    Parent($"// Parent: {Name}"),
    Ancestor[2]($"// Grandparent: {Name}"),
    Global("// Global scope")
);

namespace MyNamespace
{
    partial class Outer
    {
        partial class Inner
        {
            [LevelDemo]
            public partial int P { get; }
        }
    }
}
```

**Generated code:**
```csharp
// Global scope
namespace MyNamespace {
// Grandparent: Outer
partial class Outer {
// Parent: Inner
partial class Inner {
    public partial int P {
        get => 0;
    }
}}}
```

### Query Expressions

```csharp
[AttributeUsage(AttributeTargets.Class)]
class StringEnumAttribute(string[] names) : TemplateAttribute(
    $"{from name in names select $"    public const string {name} = nameof({name});"}"
);

[StringEnum(["Success", "Warning", "Error"])]
partial class StatusCodes;
```

**Generated code:**
```csharp
partial class StatusCodes {
    public const string Success = nameof(Success);
    public const string Warning = nameof(Warning);
    public const string Error = nameof(Error);
}
```

### Culture-Specific Formatting

```csharp
[AttributeUsage(AttributeTargets.Class)]
internal class LocalizedConstAttribute(string name, double value) : TemplateAttribute(
    $"public const string {name} = \"{value:n2}\";"
);

[LocalizedConst("InvariantValue", 1234.5)]
[LocalizedConst("GermanValue", 1234.5, CultureName = "de")]
[LocalizedConst("FrenchValue", 1234.5, CultureName = "fr")]
partial class LocalizedData;
```

**Generated code:**
```csharp
partial class LocalizedData {
    public const string InvariantValue = "1,234.50";
    public const string GermanValue = "1.234,50";
    public const string FrenchValue = "1 234,50";
}
```

## Intrinsic Values

### Member Intrinsics

| Intrinsic | Description | Example |
|-----------|-------------|---------|
| `Name` | Member name | `MyProperty` |
| `Type` | Member type | `int`, `string` |

### Hierarchy Intrinsics

| Syntax | Description | Example |
|--------|-------------|---------|
| `Parent(template)` | Generate code in parent scope | `Parent($"// {Name}")` |
| `Ancestor[n](template)` | Generate code n-levels up from the member | `Ancestor[2]($"// {Name}")` |
| `Ancestor[^n](template)` | Generate code n-levels down from topmost (global scope) | `Ancestor[^1]($"// {Name}")` |
| `Global(template)` | Generate code at global scope | `Global("// Header")` |

**Index notation:**
- `Ancestor[0]` - Current member
- `Ancestor[1]` or `Parent` - Parent
- `Ancestor[2]` - Grandparent
- `Ancestor[^0]` or `Global` - Top-level (global namespace)
- `Ancestor[^1]` - One level below global

### Parameter Intrinsics

| Intrinsic | Description | Example |
|-----------|-------------|---------|
| `Param[n].Name` | Parameter name | `a`, `b` |
| `Param[n].Type` | Parameter type | `int`, `float` |

Apply hierarchy levels to parameter intrinsics:
```csharp
Parent(Param[0].Name)           // Parent's first parameter name
Ancestor[2](Param[0].Type)      // Grandparent's first parameter type
```

## Supported Expressions

The generator supports most C# constant expressions:

### Literals and Constants
- Numeric literals: `123`, `1.5`, `1.5m`
- String literals: `"text"`, `@"verbatim"`, `"""raw"""`
- Character literals: `'a'`, `'\n'`
- Boolean literals: `true`, `false`
- Array literals: `[1, 2, 3]`, `["a", "b"]`

### Operators
- **Arithmetic**: `+`, `-`, `*`, `/`, `%`
- **Bitwise**: `&`, `|`, `~`
- **Logical**: `&&`, `||`, `!`
- **Unary**: `+value`, `-value`, `!flag`, `~bits`
- **Conditional**: `condition ? whenTrue : whenFalse`

### Type Operations
- **Casting**: `(byte)value`, `(decimal)1.2`

### Element Access
- **Array indexing**: `array[0]`
- **String indexing**: `"string"[1]`

### Query Expressions
```csharp
from item in array select expression
```

## How It Works

The generator operates in three phases:

### 1. Template Definition Analysis

The generator identifies classes that inherit from `TemplateAttribute`:

```csharp
internal class MyTemplateAttribute(...) : TemplateAttribute(
    // Template expressions analyzed here
)
```

It parses the template expressions and builds an abstract syntax tree (AST) representing the template logic.

### 2. Target Member Collection

The generator finds members decorated with template attributes:

```csharp
[MyTemplate(...)]
partial class MyClass { }
```

It collects attribute arguments and member hierarchy information.

### 3. Template Application

For each target member, the generator:
1. Evaluates template expressions using attribute arguments and intrinsic values
2. Generates code at appropriate hierarchy levels
3. Outputs partial class/struct implementations

## Architecture

The generator is organized into three layers:

```
Templates/     - Expression AST and evaluation engine
Targets/       - Target member hierarchy and attribute arguments
Application/   - Template application logic
```

### Key Components

- **`Variant`** - Type-safe union for C# literal values
- **`MemberExpression`** - Abstract syntax tree for template expressions
- **`IExpressionEvaluationContext`** - Context for expression evaluation
- **`LocalSymbolContext`** - Scoped context for query variables
- **`MemberHierarchy`** - Member containment hierarchy
- **`ArgumentList`** - Typed attribute arguments

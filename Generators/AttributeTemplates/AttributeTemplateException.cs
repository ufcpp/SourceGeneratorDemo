using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generators.AttributeTemplates;

internal class AttributeTemplateException(Diagnostic diagnostic) : Exception
{
    public Diagnostic Diagnostic { get; } = diagnostic;

    public static AttributeTemplateException UnsupportedExpression(SyntaxKind kind, Location location)
        => new(Diagnostic.Create(_unsupportedExpression, location, kind));

    private static readonly DiagnosticDescriptor _unsupportedExpression = new(
        "ATG001",
        "Unsupported expression",
        "The expression '{0}' is not supported in template definitions",
        "AttributeTemplateGenerator",
        DiagnosticSeverity.Error,
        true);

    public static AttributeTemplateException UnknownCultureName(string cultureName, Location location)
        => new(Diagnostic.Create(_unknownCultureName, location, cultureName));

    private static readonly DiagnosticDescriptor _unknownCultureName = new(
        "ATG002",
        "Unknown culture name",
        "The culture name '{0}' is not recognized",
        "AttributeTemplateGenerator",
        DiagnosticSeverity.Error,
        true);

    public static AttributeTemplateException UnknownError(Location location)
        => new(Diagnostic.Create(_unknownError, location));

    private static readonly DiagnosticDescriptor _unknownError = new(
        "ATG999",
        "Unknown error",
        "An unknown error occurred during template processing",
        "AttributeTemplateGenerator",
        DiagnosticSeverity.Error,
        true);

    public static AttributeTemplateException Unreachable(Location location)
        => new(Diagnostic.Create(_unreachable, location));

    private static readonly DiagnosticDescriptor _unreachable = new(
        "ATG998",
        "Unreachable code",
        "Reached unreachable code. This should not happen with current C# syntax, but may occur if new language features are added.",
        "AttributeTemplateGenerator",
        DiagnosticSeverity.Error,
        true);
}

internal readonly struct Result<T> where T : class
{
    private readonly object? _value;

    public Result(T value) => _value = value;
    public Result(Diagnostic error) => _value = error;

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Diagnostic error) => new(error);

    public bool IsNull => _value == null;
    public T? Value => _value as T;
    public Diagnostic? Error => _value as Diagnostic;
}

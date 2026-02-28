using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators.AttributeTemplates;

/// <summary>
/// Map from parameter names to their values for a given attribute.
/// This is used to generate the code for the attribute template, allowing users to refer to the parameters by name in the template body.
/// </summary>
/// <example>
/// Consider the following code:
/// 
/// <code><![CDATA[
/// class SomeAttribute(int arg1, string arg2) : Attribute;
///
/// [Some(42, "hello")]
/// partial T SomeMember() => ...
/// ]]></code>
///
/// Creates a map from the parameter names to their values. In the above example, the map would be:
/// [ "arg1": 42, "arg2": "hello" ]
/// </example>
internal class ParameterMap
{
    //private readonly Dictionary<string, object?>? _map;

    //public ParameterMap(SemanticModel semantics, ParameterListSyntax parameters, ArgumentListSyntax arguments)
    //{
    //    //if (parameters.Count != arguments.Count) todo: error

    //    var n = parameters.Parameters.Count;

    //    for (var i = 0; i < n; i++)
    //    {
    //        var p = parameters.Parameters[i];
    //        var a = arguments.Arguments[i];

    //        //todo: check if the types of the parameter and argument match

    //        var name = p.Identifier.ValueText;
    //        var value = semantics.GetConstantValue(a.Expression);

    //        //if (!value.HasValue) todo: error

    //        (_map ??= []).Add(name, value.Value);
    //    }
    //}

    //public int Count => _map?.Count ?? 0;
    //public object? this[string parameterName] => _map is { } t && t.TryGetValue(parameterName, out var v) ? v : null;
}

using System.Text.Json;
using System.Text.RegularExpressions;

namespace BinaryLane.Cli.Generator;

/// <summary>
/// Represents a parsed OpenAPI operation with all information needed for code generation.
/// </summary>
public sealed class OperationInfo
{
    public required string CliCommand { get; init; }
    public required string HttpMethod { get; init; }
    public required string Path { get; init; }
    public required string Summary { get; init; }
    public required List<ParameterInfo> PathParameters { get; init; }
    public required List<ParameterInfo> QueryParameters { get; init; }
    public required List<PropertyInfo> BodyProperties { get; init; }
    public required string? BodySchemaRef { get; init; }
    public required bool IsPaginated { get; init; }
    public required string? ResponseSchemaRef { get; init; }
    public required List<string> DefaultFields { get; init; }
    public required Dictionary<string, string> FieldDescriptions { get; init; }
    public required bool IsActionResponse { get; init; }
    public required bool HasActionsLink { get; init; }

    /// <summary>
    /// For polymorphic action endpoints (e.g. #PowerOn), the discriminator type value (e.g. "power_on").
    /// When set, the 'type' body property is excluded from CLI options and auto-set in the request.
    /// </summary>
    public string? DiscriminatorValue { get; init; }

    /// <summary>
    /// Get the class name for this command (e.g. "ServerListCommand")
    /// </summary>
    public string ClassName
    {
        get
        {
            var parts = CliCommand.Split(' ');
            return string.Join("", parts.Select(ToPascalCase)) + "Command";
        }
    }

    /// <summary>
    /// Get the namespace-relative path for this command's directory.
    /// First part of CliCommand becomes the subdirectory.
    /// </summary>
    public string SubDirectory
    {
        get
        {
            var parts = CliCommand.Split(' ');
            return ToPascalCase(parts[0]);
        }
    }

    public static string ToPascalCase(string kebab)
    {
        return string.Join("", kebab.Split('-').Select(s =>
            s.Length == 0 ? "" : char.ToUpperInvariant(s[0]) + s[1..]));
    }

    public static string ToCamelCase(string kebab)
    {
        var pascal = ToPascalCase(kebab);
        return pascal.Length == 0 ? "" : char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    /// <summary>
    /// Normalize description: convert title case to sentence case.
    /// Matches Python CLI behavior.
    /// </summary>
    public static string NormalizeDescription(string desc)
    {
        return Regex.Replace(desc, @" ([A-Z])([a-z]+\b)", m =>
            " " + char.ToLowerInvariant(m.Groups[1].Value[0]) + m.Groups[2].Value);
    }
}

public sealed class ParameterInfo
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required bool Required { get; init; }
    public required string Description { get; init; }
    public bool IsEnum { get; init; }
    public List<string>? EnumValues { get; init; }
}

public sealed class PropertyInfo
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required bool Required { get; init; }
    public required string Description { get; init; }
    public bool IsEnum { get; init; }
    public List<string>? EnumValues { get; init; }
    public bool IsArray { get; init; }
    public string? ArrayItemType { get; init; }
    public bool IsUnionType { get; init; }
}

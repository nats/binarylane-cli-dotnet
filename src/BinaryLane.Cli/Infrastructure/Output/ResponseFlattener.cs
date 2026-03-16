using System.Text.Json;

namespace BinaryLane.Cli.Infrastructure.Output;

/// <summary>
/// Extracts and flattens JSON response data for display.
/// Matches the Python CLI's formatter behavior.
/// </summary>
public static class ResponseFlattener
{
    /// <summary>
    /// Given a JSON response, extract the primary payload (ignoring meta/links wrappers).
    /// </summary>
    public static JsonElement ExtractPrimary(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return root;

        // Look for the "interesting" property - skip meta, links
        JsonElement? candidate = null;
        int propertyCount = 0;

        foreach (var prop in root.EnumerateObject())
        {
            propertyCount++;
            if (prop.Name is "meta" or "links") continue;
            candidate = prop.Value;
        }

        // If there's exactly one non-meta/links property, unwrap it
        if (propertyCount > 1 && candidate.HasValue)
            return candidate.Value;

        return root;
    }

    /// <summary>
    /// Convert a JSON array to rows for table display, selecting specific fields.
    /// </summary>
    public static (IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows) FlattenList(
        JsonElement array, IReadOnlyList<string> fields)
    {
        if (array.ValueKind != JsonValueKind.Array)
            return (fields, []);

        var rows = new List<IReadOnlyList<string?>>();
        foreach (var item in array.EnumerateArray())
        {
            var row = new List<string?>();
            foreach (var field in fields)
            {
                row.Add(FlattenValue(GetNestedProperty(item, field)));
            }
            rows.Add(row);
        }

        return (fields, rows);
    }

    /// <summary>
    /// Convert a JSON object to name-value pairs for object display.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> FlattenObject(JsonElement obj)
    {
        var result = new Dictionary<string, string?>();
        if (obj.ValueKind != JsonValueKind.Object) return result;

        foreach (var prop in obj.EnumerateObject())
        {
            result[prop.Name] = FlattenValue(prop.Value);
        }
        return result;
    }

    /// <summary>
    /// Get available field names from the first element of an array.
    /// </summary>
    public static IReadOnlyList<string> GetAvailableFields(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array) return [];

        var first = array.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object) return [];

        return first.EnumerateObject().Select(p => p.Name).ToList();
    }

    private static JsonElement? GetNestedProperty(JsonElement element, string path)
    {
        var parts = path.Split('.');
        var current = element;
        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object) return null;
            if (!current.TryGetProperty(part, out var next)) return null;
            current = next;
        }
        return current;
    }

    private static string? FlattenValue(JsonElement? element)
    {
        if (element == null) return null;
        var el = element.Value;

        return el.ValueKind switch
        {
            JsonValueKind.String => Truncate(el.GetString(), 80),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Null => null,
            JsonValueKind.Array => FlattenArray(el),
            JsonValueKind.Object => el.GetRawText(),
            _ => el.GetRawText(),
        };
    }

    private static string FlattenArray(JsonElement array)
    {
        var items = array.EnumerateArray().Take(6).Select(e => FlattenValue(e) ?? "").ToList();
        if (items.Count > 5)
        {
            items = items.Take(5).ToList();
            items.Add("...");
        }
        return string.Join(", ", items);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value == null || value.Length <= maxLength) return value;
        return value[..(maxLength - 3)] + "...";
    }
}

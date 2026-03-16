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

        // Unwrap if there's a single interesting property (either the original had
        // meta/links siblings, or the combined pagination response has just the data)
        if (candidate.HasValue && (propertyCount > 1 || candidate.Value.ValueKind == JsonValueKind.Array))
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
            JsonValueKind.Object => FlattenObject(el),
            _ => el.GetRawText(),
        };

        // Pick the best scalar display value from a nested object.
        // Matches Python _flatten_dict priority: display_name > full_name > name > slug > id
        static string FlattenObject(JsonElement obj)
        {
            foreach (var candidate in (ReadOnlySpan<string>)["display_name", "full_name", "name", "slug", "id"])
            {
                if (obj.TryGetProperty(candidate, out var val) && val.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    return FlattenValue(val) ?? "";
            }

            // Map 'networks' dictionary to first v4 + first v6 ip_address
            if (obj.TryGetProperty("v4", out var v4) && v4.ValueKind == JsonValueKind.Array &&
                obj.TryGetProperty("v6", out var v6) && v6.ValueKind == JsonValueKind.Array)
            {
                var ips = new List<string>();
                foreach (var entry in v4.EnumerateArray().Take(1))
                {
                    if (entry.TryGetProperty("ip_address", out var ip))
                        ips.Add(ip.GetString() ?? "");
                }
                foreach (var entry in v6.EnumerateArray().Take(1))
                {
                    if (entry.TryGetProperty("ip_address", out var ip))
                        ips.Add(ip.GetString() ?? "");
                }
                return string.Join("\n", ips);
            }

            return "<object>";
        }
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

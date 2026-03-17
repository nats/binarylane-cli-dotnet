using System.Text.Json;

namespace BinaryLane.Cli.Generator;

/// <summary>
/// Parses an OpenAPI 3.0 JSON spec into OperationInfo objects for code generation.
/// </summary>
public static class OpenApiParser
{
    public static List<OperationInfo> Parse(string specPath)
    {
        var json = File.ReadAllText(specPath);
        var doc = JsonSerializer.Deserialize<JsonElement>(json);
        var operations = new List<OperationInfo>();

        var paths = doc.GetProperty("paths");
        var schemas = doc.GetProperty("components").GetProperty("schemas");

        foreach (var pathEntry in paths.EnumerateObject())
        {
            var apiPath = pathEntry.Name;
            foreach (var methodEntry in pathEntry.Value.EnumerateObject())
            {
                var httpMethod = methodEntry.Name.ToUpperInvariant();
                if (httpMethod is not ("GET" or "POST" or "PUT" or "DELETE" or "PATCH"))
                    continue;

                var op = methodEntry.Value;
                if (!op.TryGetProperty("x-cli-command", out var cliCmd))
                    continue;

                var cliCommand = cliCmd.GetString()!;
                var summary = op.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";

                // Parse parameters
                var allParams = new List<JsonElement>();
                if (op.TryGetProperty("parameters", out var parameters))
                {
                    foreach (var p in parameters.EnumerateArray())
                        allParams.Add(p);
                }

                var pathParams = allParams
                    .Where(p => p.GetProperty("in").GetString() == "path")
                    .Select(p => ParseParameter(p, schemas))
                    .ToList();

                var queryParams = allParams
                    .Where(p => p.GetProperty("in").GetString() == "query")
                    .Where(p => p.GetProperty("name").GetString() is not ("page" or "per_page"))
                    .Select(p => ParseParameter(p, schemas))
                    .ToList();

                var isPaginated = allParams.Any(p =>
                    p.GetProperty("in").GetString() == "query" &&
                    p.GetProperty("name").GetString() == "page");

                // Parse request body
                var bodyProperties = new List<PropertyInfo>();
                string? bodySchemaRef = null;
                if (op.TryGetProperty("requestBody", out var reqBody))
                {
                    var content = reqBody.GetProperty("content");
                    if (content.TryGetProperty("application/json", out var jsonContent))
                    {
                        var bodySchema = jsonContent.GetProperty("schema");
                        bodySchemaRef = ResolveRef(bodySchema);
                        var resolved = ResolveSchema(bodySchema, schemas);
                        if (resolved.TryGetProperty("properties", out var props))
                        {
                            var requiredProps = GetRequiredList(resolved);
                            foreach (var prop in props.EnumerateObject())
                            {
                                // Skip the 'type' discriminator field for polymorphic types
                                if (prop.Name == "type" && resolved.TryGetProperty("discriminator", out _))
                                    continue;

                                bodyProperties.Add(ParseProperty(prop, requiredProps, schemas));
                            }
                        }

                        // Handle oneOf/anyOf with discriminator (polymorphic request bodies)
                        if (resolved.TryGetProperty("oneOf", out var oneOf))
                        {
                            // For polymorphic bodies, extract properties from the referenced schema
                            // The Python CLI handles this by looking at the discriminator
                            // For now, get common properties from the base
                        }
                    }
                }

                // Parse response
                string? responseSchemaRef = null;
                var defaultFields = new List<string>();
                var fieldDescriptions = new Dictionary<string, string>();
                bool isActionResponse = false;
                bool hasActionsLink = false;

                var okResponse = GetOkResponse(op);
                if (okResponse.HasValue && okResponse.Value.TryGetProperty("content", out var respContent))
                {
                    if (respContent.TryGetProperty("application/json", out var respJson))
                    {
                        var respSchema = respJson.GetProperty("schema");
                        responseSchemaRef = ResolveRef(respSchema);
                        var resolved = ResolveSchema(respSchema, schemas);

                        // Determine if it's an ActionResponse
                        if (responseSchemaRef?.EndsWith("/ActionResponse") == true)
                            isActionResponse = true;

                        // Check for ActionsLinks
                        if (resolved.TryGetProperty("properties", out var respProps))
                        {
                            if (respProps.TryGetProperty("links", out var linksSchema))
                            {
                                var linksRef = ResolveRef(linksSchema);
                                if (linksRef?.Contains("ActionsLinks") == true)
                                    hasActionsLink = true;
                            }

                            // Find the primary list property for default fields
                            foreach (var prop in respProps.EnumerateObject())
                            {
                                if (prop.Name is "meta" or "links") continue;
                                var propResolved = ResolveSchema(prop.Value, schemas);
                                if (propResolved.TryGetProperty("type", out var t) && t.GetString() == "array")
                                {
                                    // Get item schema for field names
                                    if (propResolved.TryGetProperty("items", out var items))
                                    {
                                        var itemSchema = ResolveSchema(items, schemas);
                                        if (itemSchema.TryGetProperty("properties", out var itemProps))
                                        {
                                            var formatEntries = new List<(int order, string name)>();
                                            foreach (var itemProp in itemProps.EnumerateObject())
                                            {
                                                fieldDescriptions[itemProp.Name] = GetDescription(itemProp.Value);

                                                // Use x-cli-format sort order if present
                                                if (itemProp.Value.TryGetProperty("x-cli-format", out var fmt) &&
                                                    fmt.TryGetInt32(out var order))
                                                {
                                                    formatEntries.Add((order, itemProp.Name));
                                                }
                                            }

                                            if (formatEntries.Count > 0)
                                            {
                                                defaultFields.AddRange(formatEntries.OrderBy(e => e.order).Select(e => e.name));
                                            }
                                            else
                                            {
                                                // Fallback: required primitive fields
                                                var requiredSet = GetRequiredList(itemSchema);
                                                foreach (var itemProp in itemProps.EnumerateObject())
                                                {
                                                    if (!requiredSet.Contains(itemProp.Name)) continue;
                                                    var fieldResolved = ResolveSchema(itemProp.Value, schemas);
                                                    var fieldType = fieldResolved.TryGetProperty("type", out var ft) ? ft.GetString() : null;
                                                    if (fieldType is "string" or "integer" or "number" or "boolean")
                                                        defaultFields.Add(itemProp.Name);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                // Detect discriminator from URL fragment (e.g. #PowerOn → power_on)
                string? discriminatorValue = null;
                var fragmentIdx = apiPath.IndexOf('#');
                if (fragmentIdx >= 0)
                {
                    var fragment = apiPath[(fragmentIdx + 1)..];
                    // PascalCase to snake_case
                    discriminatorValue = System.Text.RegularExpressions.Regex
                        .Replace(fragment, @"(?<!^)(?=[A-Z])", "_").ToLowerInvariant();
                    // Remove 'type' from body properties since it's auto-set
                    bodyProperties.RemoveAll(p => p.Name == "type");
                }

                operations.Add(new OperationInfo
                {
                    CliCommand = cliCommand,
                    HttpMethod = httpMethod,
                    Path = apiPath,
                    Summary = summary,
                    PathParameters = pathParams,
                    QueryParameters = queryParams,
                    BodyProperties = bodyProperties,
                    BodySchemaRef = bodySchemaRef,
                    IsPaginated = isPaginated,
                    ResponseSchemaRef = responseSchemaRef,
                    DefaultFields = defaultFields,
                    FieldDescriptions = fieldDescriptions,
                    IsActionResponse = isActionResponse,
                    HasActionsLink = hasActionsLink,
                    DiscriminatorValue = discriminatorValue,
                });
            }
        }

        return operations;
    }

    private static ParameterInfo ParseParameter(JsonElement param, JsonElement schemas)
    {
        var name = param.GetProperty("name").GetString()!;
        var required = param.TryGetProperty("required", out var r) && r.GetBoolean();
        var description = GetDescription(param);
        var schema = param.TryGetProperty("schema", out var s) ? s : default;
        var (type, isEnum, enumValues) = GetTypeInfo(schema, schemas);

        return new ParameterInfo
        {
            Name = name,
            Type = type,
            Required = required,
            Description = description,
            IsEnum = isEnum,
            EnumValues = enumValues,
        };
    }

    private static PropertyInfo ParseProperty(JsonProperty prop, HashSet<string> requiredProps, JsonElement schemas)
    {
        var name = prop.Name;
        var description = GetDescription(prop.Value);
        var required = requiredProps.Contains(name);
        var resolved = ResolveSchema(prop.Value, schemas);
        var (type, isEnum, enumValues) = GetTypeInfo(prop.Value, schemas);

        bool isArray = false;
        string? arrayItemType = null;
        if (resolved.TryGetProperty("type", out var t) && t.GetString() == "array")
        {
            isArray = true;
            if (resolved.TryGetProperty("items", out var items))
            {
                var (itemType, _, _) = GetTypeInfo(items, schemas);
                arrayItemType = itemType;
            }
        }

        return new PropertyInfo
        {
            Name = name,
            Type = type,
            Required = required,
            Description = description,
            IsEnum = isEnum,
            EnumValues = enumValues,
            IsArray = isArray,
            ArrayItemType = arrayItemType,
        };
    }

    private static (string type, bool isEnum, List<string>? enumValues) GetTypeInfo(JsonElement schema, JsonElement schemas)
    {
        var resolved = ResolveSchema(schema, schemas);

        if (resolved.TryGetProperty("enum", out var enumArray))
        {
            var values = enumArray.EnumerateArray().Select(e => e.GetString()!).ToList();
            return ("string", true, values);
        }

        var type = resolved.TryGetProperty("type", out var t) ? t.GetString() : null;

        // Handle oneOf/anyOf unions
        foreach (var key in new[] { "oneOf", "anyOf" })
        {
            if (!resolved.TryGetProperty(key, out var unionArray)) continue;
            var nonNullTypes = new List<string>();
            foreach (var option in unionArray.EnumerateArray())
            {
                var optResolved = ResolveSchema(option, schemas);
                if (optResolved.TryGetProperty("type", out var ot) && ot.GetString() is string optType && optType != "null")
                    nonNullTypes.Add(optType);
            }
            if (nonNullTypes.Count == 0) continue;
            // If union includes string (e.g. Union[int,str]), use string to accept both
            if (nonNullTypes.Contains("string"))
                return ("string", false, null);
            // Otherwise use the first non-null type
            return (nonNullTypes[0] switch
            {
                "integer" => "int",
                "number" => "double",
                "boolean" => "bool",
                _ => "string",
            }, false, null);
        }

        return type switch
        {
            "string" => resolved.TryGetProperty("format", out var f) && f.GetString() == "date-time"
                ? ("DateTime", false, null)
                : ("string", false, null),
            "integer" => ("int", false, null),
            "number" => ("double", false, null),
            "boolean" => ("bool", false, null),
            "array" => ("array", false, null),
            _ => ("string", false, null),
        };
    }

    private static JsonElement ResolveSchema(JsonElement schema, JsonElement schemas)
    {
        if (schema.TryGetProperty("$ref", out var refProp))
        {
            var refPath = refProp.GetString()!;
            var schemaName = refPath.Split('/').Last();
            if (schemas.TryGetProperty(schemaName, out var resolved))
                return resolved;
        }

        // Handle allOf: merge all referenced schemas (common in OpenAPI 3.0)
        if (schema.TryGetProperty("allOf", out var allOf))
        {
            foreach (var item in allOf.EnumerateArray())
            {
                var resolved = ResolveSchema(item, schemas);
                if (resolved.TryGetProperty("properties", out _))
                    return resolved;
            }
        }

        return schema;
    }

    private static string? ResolveRef(JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var refProp))
            return refProp.GetString();
        return null;
    }

    private static string GetDescription(JsonElement element)
    {
        if (element.TryGetProperty("description", out var d))
            return d.GetString() ?? "";
        return "";
    }

    private static HashSet<string> GetRequiredList(JsonElement schema)
    {
        var result = new HashSet<string>();
        if (schema.TryGetProperty("required", out var req))
        {
            foreach (var item in req.EnumerateArray())
            {
                if (item.GetString() is string s)
                    result.Add(s);
            }
        }
        return result;
    }

    private static JsonElement? GetOkResponse(JsonElement operation)
    {
        if (!operation.TryGetProperty("responses", out var responses))
            return null;

        foreach (var code in new[] { "200", "201" })
        {
            if (responses.TryGetProperty(code, out var resp))
                return resp;
        }
        return null;
    }
}

using System.Net;
using System.Text.Json;
using BinaryLane.Cli.Infrastructure.Json;
using BinaryLane.Cli.Infrastructure.Output;

namespace BinaryLane.Cli.Commands;

/// <summary>
/// Handles HTTP execution, --curl, output formatting, and error handling for API commands.
/// </summary>
public abstract class ApiCommandBase
{
    public static async Task<int> ExecuteApiCallAsync(
        CommandContext ctx,
        Func<HttpClient, Task<HttpResponseMessage>> sendRequest,
        Func<JsonElement, (IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows)>? formatAsTable = null,
        Func<JsonElement, IReadOnlyDictionary<string, string?>>? formatAsObject = null)
    {
        using var httpClient = ctx.GetHttpClient();
        var client = httpClient.Client;

        var response = await sendRequest(client);

        if (ctx.Curl) return 0;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Console.Error.WriteLine("Error: API token is not configured or is invalid. Please run 'bl configure'.");
            return 3;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            try
            {
                var errorJson = JsonSerializer.Deserialize(errorBody, CliJsonContext.Default.JsonElement);
                var message = errorJson.TryGetProperty("message", out var msg) ? msg.GetString()
                    : errorJson.TryGetProperty("detail", out var det) ? det.GetString()
                    : errorBody;
                Console.Error.WriteLine($"Error: {(int)response.StatusCode} - {message}");
            }
            catch
            {
                Console.Error.WriteLine($"Error: {(int)response.StatusCode} - {errorBody}");
            }
            return 4;
        }

        if (response.StatusCode == HttpStatusCode.NoContent) return 0;

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return 0;

        if (ctx.OutputFormat == OutputFormat.Json)
        {
            var parsed = JsonSerializer.Deserialize(body, CliJsonContext.Default.JsonElement);
            var formatted = JsonSerializer.Serialize(parsed, CliJsonPrettyContext.Default.JsonElement);
            Console.WriteLine(formatted);
            return 0;
        }

        var json = JsonSerializer.Deserialize(body, CliJsonContext.Default.JsonElement);
        var primary = ResponseFlattener.ExtractPrimary(json);
        var formatter = ctx.GetFormatter();

        if (primary.ValueKind == JsonValueKind.Array || formatAsTable != null)
        {
            if (formatAsTable != null)
            {
                var (columns, rows) = formatAsTable(json);
                formatter.PrintTable(columns, rows, ctx.NoHeader);
            }
            else
            {
                var fields = ResponseFlattener.GetAvailableFields(primary);
                var (columns, rows) = ResponseFlattener.FlattenList(primary, fields.ToList());
                formatter.PrintTable(columns, rows, ctx.NoHeader);
            }
        }
        else if (formatAsObject != null)
        {
            var nameValues = formatAsObject(json);
            formatter.PrintObject(nameValues, ctx.NoHeader);
        }
        else
        {
            var nameValues = ResponseFlattener.FlattenObject(primary);
            formatter.PrintObject(nameValues, ctx.NoHeader);
        }

        return 0;
    }

    public static async Task<HttpResponseMessage> ExecutePaginatedAsync(
        HttpClient client,
        string path,
        int perPage = 25)
    {
        var allItems = new List<JsonElement>();
        int page = 0;
        string? primaryKey = null;
        JsonElement lastResponse = default;

        while (true)
        {
            page++;
            var separator = path.Contains('?') ? '&' : '?';
            var url = $"{path}{separator}page={page}&per_page={perPage}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return response;

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return response;

            var json = JsonSerializer.Deserialize(body, CliJsonContext.Default.JsonElement);
            lastResponse = json;

            if (primaryKey == null && json.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in json.EnumerateObject())
                {
                    if (prop.Name is "meta" or "links") continue;
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        primaryKey = prop.Name;
                        break;
                    }
                }
            }

            if (primaryKey != null && json.TryGetProperty(primaryKey, out var items))
            {
                foreach (var item in items.EnumerateArray())
                    allItems.Add(item);
            }

            bool hasNext = json.TryGetProperty("links", out var links) &&
                links.TryGetProperty("pages", out var pages) &&
                pages.TryGetProperty("next", out var next) &&
                next.ValueKind == JsonValueKind.String;

            if (!hasNext) break;
        }

        if (primaryKey != null)
        {
            var combined = new Dictionary<string, object>
            {
                [primaryKey] = allItems
            };
            var combinedJson = JsonSerializer.Serialize(combined, CliJsonContext.Default.DictionaryStringObject);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(combinedJson, System.Text.Encoding.UTF8, "application/json")
            };
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(lastResponse, CliJsonContext.Default.JsonElement),
                System.Text.Encoding.UTF8, "application/json")
        };
    }
}

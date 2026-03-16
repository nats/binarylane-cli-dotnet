using System.Text.Json;
using System.Text.Json.Serialization;

namespace BinaryLane.Cli.Infrastructure.Json;

[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<JsonElement>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string[]))]
[JsonSourceGenerationOptions(WriteIndented = false)]
public partial class CliJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(JsonElement))]
public partial class CliJsonPrettyContext : JsonSerializerContext;

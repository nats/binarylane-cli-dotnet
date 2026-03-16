using System.Text;

namespace BinaryLane.Cli.Infrastructure.Http;

/// <summary>
/// DelegatingHandler that intercepts HTTP requests and outputs curl commands instead of executing them.
/// </summary>
public sealed class CurlGenerator : DelegatingHandler
{
    public bool Enabled { get; set; }
    public string? GeneratedCommand { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!Enabled) return base.SendAsync(request, cancellationToken);

        GeneratedCommand = BuildCurlCommand(request);
        Console.WriteLine(GeneratedCommand);

        // Return a dummy response to short-circuit the pipeline
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    private static string BuildCurlCommand(HttpRequestMessage request)
    {
        var sb = new StringBuilder();
        sb.Append("curl");

        if (request.Method != HttpMethod.Get)
        {
            sb.Append($" --request {request.Method}");
        }

        sb.Append($" {ShellEscape(request.RequestUri?.ToString() ?? "")}");

        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
            {
                if (header.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var value in header.Value)
                {
                    sb.Append($" --header {ShellEscape($"{header.Key}: {value}")}");
                }
            }
        }

        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                foreach (var value in header.Value)
                {
                    sb.Append($" --header {ShellEscape($"{header.Key}: {value}")}");
                }
            }

            var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(body))
            {
                sb.Append($" --data {ShellEscape(body)}");
            }
        }

        return sb.ToString();
    }

    private static string ShellEscape(string value)
    {
        if (!value.Any(c => c is ' ' or '\'' or '"' or '\\' or '$' or '!' or '`' or '{' or '}' or '(' or ')' or '[' or ']' or '|' or '&' or ';' or '<' or '>' or '?' or '*' or '#' or '~'))
            return value;

        return "'" + value.Replace("'", "'\\''") + "'";
    }
}

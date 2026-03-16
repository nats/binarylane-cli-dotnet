using System.CommandLine;
using System.Net.Http.Headers;

namespace BinaryLane.Cli.Commands;

public static class ConfigureCommand
{
    public static Command Create()
    {
        var command = new Command("configure", "Configure access to BinaryLane API");
        command.SetAction(async (parseResult, ct) =>
        {
            var cmdCtx = ContextBinder.Bind(parseResult);
            var appConfig = cmdCtx.Config;

            Console.Write("Enter your API token: ");
            var token = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(token))
            {
                Console.Error.WriteLine("No token provided.");
                return 1;
            }

            using var client = new HttpClient();
            client.BaseAddress = new Uri(appConfig.ApiUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await client.GetAsync("v2/account", ct);
                if (response.IsSuccessStatusCode)
                {
                    appConfig.Save(apiToken: token);
                    Console.WriteLine("API token validated and saved.");
                    return 0;
                }

                Console.Error.WriteLine($"Token validation failed: HTTP {(int)response.StatusCode}");
                Console.Write("Save anyway? [y/N] ");
                var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (answer is "y" or "yes")
                {
                    appConfig.Save(apiToken: token);
                    Console.WriteLine("API token saved.");
                    return 0;
                }
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Connection error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }
}

using System.Diagnostics;

namespace BinaryLane.Cli.IntegrationTests;

/// <summary>
/// Runs bl (Python) and blnet (.NET) CLI executables and captures output.
/// Both must be installed in ~/.local/bin (via 'make install' for blnet).
/// </summary>
public static class CliRunner
{
    private static readonly string LocalBin = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin");

    public static Task<CliResult> BlpyAsync(params string[] args) => RunAsync("bl", args);
    public static Task<CliResult> BlnetAsync(params string[] args) => RunAsync("blnet", args);

    private static async Task<CliResult> RunAsync(string command, string[] args)
    {
        var psi = new ProcessStartInfo(Path.Combine(LocalBin, command))
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi)!;
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();

        return new CliResult(stdout.Trim(), stderr.Trim(), p.ExitCode);
    }
}

public record CliResult(string Stdout, string Stderr, int ExitCode);

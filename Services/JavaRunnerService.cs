using System.Diagnostics;
using System.Text;

namespace JavaMaster.Api.Services;

public enum RunStage { Ok, Busy, CompileError, CompileTimedOut, RunTimedOut, RuntimeError }

public class RunResult
{
    public RunStage Stage { get; init; }
    public string StdOut { get; init; } = "";
    public string StdErr { get; init; } = "";
}

/// <summary>
/// Compiles and runs one Practice.java file. Needs a JDK (javac and java on PATH, or Java:BinPath in appsettings).
/// WARNING: this runs learner code on the server machine. Fine for development and a school demo.
/// For a public site, run javac/java inside Docker (--network none, memory and CPU limits) or use a service such as Judge0.
/// </summary>
public class JavaRunnerService
{
    private readonly SemaphoreSlim _gate = new(2);   // at most 2 programs at the same time
    private readonly string? _bin;

    public JavaRunnerService(IConfiguration config) => _bin = config["Java:BinPath"];

    public async Task<RunResult> CompileAndRunAsync(string code)
    {
        if (!await _gate.WaitAsync(TimeSpan.FromSeconds(10))) return new RunResult { Stage = RunStage.Busy };
        var dir = Path.Combine(Path.GetTempPath(), "jm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "Practice.java"), code, new UTF8Encoding(false));

            var compile = await RunAsync("javac", new[] { "-encoding", "UTF-8", "Practice.java" }, dir, 15000);
            if (compile.TimedOut) return new RunResult { Stage = RunStage.CompileTimedOut };
            if (compile.ExitCode != 0) return new RunResult { Stage = RunStage.CompileError, StdErr = compile.StdErr };

            var run = await RunAsync("java", new[] { "-Xmx64m", "-cp", dir, "Main" }, dir, 5000);
            if (run.TimedOut) return new RunResult { Stage = RunStage.RunTimedOut };
            if (run.ExitCode != 0) return new RunResult { Stage = RunStage.RuntimeError, StdErr = run.StdErr };

            return new RunResult { Stage = RunStage.Ok, StdOut = run.StdOut.Replace("\r\n", "\n").TrimEnd() };
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* temp folder, ignore */ }
            _gate.Release();
        }
    }

    private async Task<(int ExitCode, bool TimedOut, string StdOut, string StdErr)> RunAsync(
        string tool, string[] args, string workDir, int timeoutMs)
    {
        var psi = new ProcessStartInfo(string.IsNullOrEmpty(_bin) ? tool : Path.Combine(_bin, tool))
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start " + tool);
        var outTask = p.StandardOutput.ReadToEndAsync();
        var errTask = p.StandardError.ReadToEndAsync();

        var timedOut = false;
        using var cts = new CancellationTokenSource(timeoutMs);
        try { await p.WaitForExitAsync(cts.Token); }
        catch (OperationCanceledException)
        {
            timedOut = true;
            try { p.Kill(entireProcessTree: true); } catch { }
        }
        var stdout = await outTask;
        var stderr = await errTask;
        return (timedOut ? -1 : p.ExitCode, timedOut, Clip(stdout), Clip(stderr));
    }

    private static string Clip(string s) => s.Length > 10_000 ? s[..10_000] : s;
}
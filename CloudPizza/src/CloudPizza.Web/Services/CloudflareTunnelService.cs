namespace CloudBurger.Web.Services;

using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

public sealed class CloudflareTunnelService(ILogger<CloudflareTunnelService> logger) : IAsyncDisposable
{
    private static readonly Regex TunnelUrlRegex = new(@"https://[a-zA-Z0-9-]+\.trycloudflare\.com", RegexOptions.Compiled);
    private readonly SemaphoreSlim syncLock = new(1, 1);

    private Process? process;
    private string? tunnelUrl;

    public async Task<string> StartOrGetTunnelAsync(string targetUrl, CancellationToken cancellationToken = default)
    {
        await syncLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(tunnelUrl) && process is { HasExited: false })
            {
                return tunnelUrl;
            }

            await StopTunnelInternalAsync();

            var args = BuildArguments(targetUrl);
            logger.LogInformation("Starting Cloudflare tunnel with args: {Args}", args);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cloudflared",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start cloudflared process.");

            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(25));

            tunnelUrl = await WaitForTunnelUrlAsync(process, timeoutCts.Token);
            logger.LogInformation("Cloudflare tunnel created: {TunnelUrl}", tunnelUrl);
            return tunnelUrl;
        }
        catch (Win32Exception ex)
        {
            logger.LogError(ex, "cloudflared executable not found");
            throw new InvalidOperationException("cloudflared was not found on PATH. Open a terminal and run 'cloudflared --version' to verify installation.", ex);
        }
        finally
        {
            syncLock.Release();
        }
    }

    public async Task StopTunnelAsync()
    {
        await syncLock.WaitAsync();
        try
        {
            await StopTunnelInternalAsync();
        }
        finally
        {
            syncLock.Release();
        }
    }

    private static string BuildArguments(string targetUrl)
    {
        var arguments = $"tunnel --url {targetUrl}";

        if (targetUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
            targetUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            arguments += " --no-tls-verify";
        }

        return arguments;
    }

    private static async Task<string> WaitForTunnelUrlAsync(Process process, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        void HandleLine(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var match = TunnelUrlRegex.Match(line);
            if (match.Success)
            {
                tcs.TrySetResult(match.Value);
            }
        }

        DataReceivedEventHandler outputHandler = (_, e) => HandleLine(e.Data);
        DataReceivedEventHandler errorHandler = (_, e) => HandleLine(e.Data);

        process.OutputDataReceived += outputHandler;
        process.ErrorDataReceived += errorHandler;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(() =>
            tcs.TrySetException(new TimeoutException("Timed out waiting for Cloudflare tunnel URL.")));

        try
        {
            return await tcs.Task;
        }
        finally
        {
            process.OutputDataReceived -= outputHandler;
            process.ErrorDataReceived -= errorHandler;
        }
    }

    private async Task StopTunnelInternalAsync()
    {
        if (process is null)
        {
            tunnelUrl = null;
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to stop existing cloudflared process cleanly");
        }
        finally
        {
            process.Dispose();
            process = null;
            tunnelUrl = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopTunnelAsync();
        syncLock.Dispose();
    }
}

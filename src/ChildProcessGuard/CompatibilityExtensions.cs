using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChildProcessGuard;

/// <summary>
/// Extension methods to provide .NET 5+ functionality for .NET Standard 2.1
/// </summary>
internal static class CompatibilityExtensions
{
    /// <summary>
    /// Asynchronously waits for the process to exit (compatibility method for .NET Standard 2.1)
    /// </summary>
    /// <param name="process">The process to wait for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when the process exits</returns>
    public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
    {
        if (process.HasExited)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        void ProcessExited(object sender, EventArgs e) => tcs.TrySetResult(true);

        process.EnableRaisingEvents = true;
        process.Exited += ProcessExited;

        if (process.HasExited)
        {
            tcs.TrySetResult(true);
        }

        // Handle cancellation
        cancellationToken.Register(() =>
        {
            process.Exited -= ProcessExited;
            tcs.TrySetCanceled(cancellationToken);
        });

        return tcs.Task;
    }

    /// <summary>
    /// Kills the process and optionally its entire process tree (compatibility method)
    /// </summary>
    /// <param name="process">The process to kill</param>
    /// <param name="entireProcessTree">Whether to kill the entire process tree</param>
    public static void KillProcessTree(this Process process, bool entireProcessTree = true)
    {
        if (process.HasExited)
            return;

        if (!entireProcessTree)
        {
            process.Kill();
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                KillProcessTreeWindows(process.Id);
            }
            else
            {
                KillProcessTreeUnix(process.Id);
            }
        }
        catch
        {
            // Fallback to simple kill
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
    }

    /// <summary>
    /// Kills process tree on Windows using taskkill
    /// </summary>
    /// <param name="processId">Process ID to kill</param>
    private static void KillProcessTreeWindows(int processId)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/F /T /PID {processId}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var killProcess = Process.Start(startInfo);
            killProcess?.WaitForExit(5000);
        }
        catch
        {
            // Fallback to direct kill
            try
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
            }
            catch
            {
                // Process might have already exited
            }
        }
    }

    /// <summary>
    /// Kills process tree on Unix systems
    /// </summary>
    /// <param name="processId">Process ID to kill</param>
    private static void KillProcessTreeUnix(int processId)
    {
        try
        {
            // Try to get process group and kill the group
            var pgid = NativeMethods.GetProcessGroup(processId);
            if (pgid > 0)
            {
                NativeMethods.KillProcessGroup(pgid, NativeMethods.SIGTERM);

                // Wait a bit, then force kill if needed
                Task.Delay(2000).Wait();

                try
                {
                    var process = Process.GetProcessById(processId);
                    if (!process.HasExited)
                    {
                        NativeMethods.KillProcessGroup(pgid, NativeMethods.SIGKILL);
                    }
                }
                catch
                {
                    // Process might have exited
                }
            }
            else
            {
                // Fallback to kill just the process
                var process = Process.GetProcessById(processId);
                process.Kill();
            }
        }
        catch
        {
            // Final fallback
            try
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
            }
            catch
            {
                // Process might have already exited
            }
        }
    }

    /// <summary>
    /// Gets child processes of a given process (helper method)
    /// </summary>
    /// <param name="parentId">Parent process ID</param>
    /// <returns>List of child process IDs</returns>
    public static List<int> GetChildProcessIds(int parentId)
    {
        var childIds = new List<int>();

        try
        {
            var allProcesses = Process.GetProcesses();

            foreach (var process in allProcesses)
            {
                try
                {
                    if (GetParentProcessId(process.Id) == parentId)
                    {
                        childIds.Add(process.Id);
                        // Recursively get grandchildren
                        childIds.AddRange(GetChildProcessIds(process.Id));
                    }
                }
                catch
                {
                    // Skip processes we can't access
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return childIds;
    }

    /// <summary>
    /// Gets the parent process ID (platform-specific implementation)
    /// </summary>
    /// <param name="processId">Process ID</param>
    /// <returns>Parent process ID or -1 if not found</returns>
    private static int GetParentProcessId(int processId)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetParentProcessIdWindows(processId);
        }
        else
        {
            return GetParentProcessIdUnix(processId);
        }
    }

    /// <summary>
    /// Gets parent process ID on Windows using WMI-like approach
    /// </summary>
    /// <param name="processId">Process ID</param>
    /// <returns>Parent process ID</returns>
    private static int GetParentProcessIdWindows(int processId)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = $"process where processid={processId} get parentprocessid /format:csv",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();

            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out var parentId))
                    {
                        return parentId;
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return -1;
    }

    /// <summary>
    /// Gets parent process ID on Unix systems
    /// </summary>
    /// <param name="processId">Process ID</param>
    /// <returns>Parent process ID</returns>
    private static int GetParentProcessIdUnix(int processId)
    {
        try
        {
            var statFile = $"/proc/{processId}/stat";
            if (File.Exists(statFile))
            {
                var stat = File.ReadAllText(statFile);
                var parts = stat.Split(' ');
                if (parts.Length >= 4 && int.TryParse(parts[3], out var parentId))
                {
                    return parentId;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return -1;
    }
}
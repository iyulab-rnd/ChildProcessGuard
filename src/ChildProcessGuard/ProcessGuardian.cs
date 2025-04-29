using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChildProcessGuard;

/// <summary>
/// A cross-platform process management library that ensures child processes
/// automatically terminate when the parent process exits unexpectedly.
/// </summary>
public class ProcessGuardian : IDisposable
{
    private readonly List<Process> _childProcesses = [];
    private IntPtr _jobHandle = IntPtr.Zero;
    private bool _isDisposed;
    private readonly bool _isWindows;

    /// <summary>
    /// Initializes a new instance of the ProcessGuardian class.
    /// </summary>
    public ProcessGuardian()
    {
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Register process exit event handler
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Dispose();

        // Initialize Job Object on Windows
        if (_isWindows)
        {
            InitializeWindowsJobObject();
        }
    }

    /// <summary>
    /// Starts a new process and registers it for management.
    /// </summary>
    /// <param name="fileName">The path to the program to execute</param>
    /// <param name="arguments">The command line arguments</param>
    /// <param name="workingDirectory">The working directory</param>
    /// <param name="environmentVariables">A dictionary of environment variables</param>
    /// <returns>The started process</returns>
    public Process StartProcess(string fileName, string arguments = "", string? workingDirectory = null, Dictionary<string, string>? environmentVariables = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };

        // Set working directory
        if (!string.IsNullOrEmpty(workingDirectory))
        {
            processStartInfo.WorkingDirectory = workingDirectory;
        }

        // Set environment variables
        if (environmentVariables != null)
        {
            foreach (var kvp in environmentVariables)
            {
                processStartInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        return StartProcessWithStartInfo(processStartInfo);
    }

    /// <summary>
    /// Starts a new process with the provided ProcessStartInfo and registers it for management.
    /// This allows full control over all ProcessStartInfo properties including Verb, CreateNoWindow, etc.
    /// </summary>
    /// <param name="startInfo">The ProcessStartInfo instance to use</param>
    /// <returns>The started process</returns>
    public Process StartProcessWithStartInfo(ProcessStartInfo startInfo)
    {
        var process = new Process { StartInfo = startInfo };
        process.Start();

        lock (_childProcesses)
        {
            _childProcesses.Add(process);

            // Assign to Job Object on Windows
            if (_isWindows && _jobHandle != IntPtr.Zero)
            {
                NativeMethods.AssignProcessToJobObject(_jobHandle, process.Handle);
            }
        }

        return process;
    }

    /// <summary>
    /// Terminates all managed child processes.
    /// </summary>
    public void KillAllProcesses()
    {
        lock (_childProcesses)
        {
            foreach (var process in _childProcesses)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error terminating process: {ex.Message}");
                }
            }
            _childProcesses.Clear();
        }
    }

    /// <summary>
    /// Removes a specific process from the management list.
    /// </summary>
    /// <param name="process">The process to remove</param>
    public void RemoveProcess(Process process)
    {
        lock (_childProcesses)
        {
            _childProcesses.Remove(process);
        }
    }

    /// <summary>
    /// Releases all resources used by the ProcessGuardian.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the managed and unmanaged resources used by the ProcessGuardian.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            KillAllProcesses();
        }

        // Dispose unmanaged resources (Windows Job Object)
        if (_isWindows && _jobHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_jobHandle);
            _jobHandle = IntPtr.Zero;
        }

        _isDisposed = true;
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~ProcessGuardian()
    {
        Dispose(false);
    }

    /// <summary>
    /// Initializes a Windows Job Object.
    /// </summary>
    private void InitializeWindowsJobObject()
    {
        if (!_isWindows)
            return;

        _jobHandle = NativeMethods.CreateJobObject(IntPtr.Zero, null);

        var info = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        int infoSize = Marshal.SizeOf<NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        IntPtr extendedInfoPtr = Marshal.AllocHGlobal(infoSize);

        try
        {
            Marshal.StructureToPtr(info, extendedInfoPtr, false);
            NativeMethods.SetInformationJobObject(_jobHandle, NativeMethods.JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)infoSize);
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }
    }

    /// <summary>
    /// Returns a read-only list of all currently managed processes.
    /// </summary>
    /// <returns>A list of managed processes</returns>
    public IReadOnlyList<Process> GetManagedProcesses()
    {
        lock (_childProcesses)
        {
            return [.. _childProcesses];
        }
    }
}

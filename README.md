# ChildProcessGuard

A robust, cross-platform .NET library that ensures child processes automatically terminate when the parent process exits unexpectedly. Enhanced with advanced process management, monitoring, and configuration options.

## Features

- **Cross-Platform Support**: Works seamlessly on Windows, Linux, and macOS
- **Automatic Cleanup**: Child processes are automatically terminated when the parent process exits
- **Windows Job Object**: Utilizes Windows Job Objects for reliable process management on Windows
- **Unix Process Groups**: Uses process groups on Unix systems for better process tree management
- **Process Tree Termination**: Ensures all descendant processes are also terminated
- **Async/Await Support**: Full asynchronous API with cancellation token support
- **Advanced Configuration**: Flexible options for timeouts, logging, and behavior customization
- **Real-time Monitoring**: Process statistics, lifecycle events, and health monitoring
- **Batch Processing**: Start multiple processes concurrently with built-in concurrency control
- **Builder Pattern**: Easy configuration with fluent API
- **Graceful Shutdown**: Configurable timeout and force-kill fallback mechanisms
- **Thread-Safe**: Concurrent operations supported with proper synchronization
- **Memory Management**: Automatic cleanup of disposed processes and memory leak prevention
- **Error Handling**: Comprehensive error tracking and event notifications

## Requirements

- .NET Standard 2.1 or higher
- .NET Framework 4.7.2+ / .NET Core 2.1+ / .NET 5+

## Installation

### Package Manager

```
Install-Package ChildProcessGuard
```

### .NET CLI

```
dotnet add package ChildProcessGuard
```

## Quick Start

### Basic Usage

```csharp
using ChildProcessGuard;

// Simple usage with automatic cleanup
using var guardian = new ProcessGuardian();

var process = guardian.StartProcess("notepad.exe");
Console.WriteLine($"Started process with PID: {process.Id}");

// Process will be automatically terminated when guardian is disposed
```

### Advanced Configuration

```csharp
using ChildProcessGuard;

// Configure with builder pattern
using var guardian = ProcessGuardianBuilder.Debug()
    .WithKillTimeout(TimeSpan.FromSeconds(10))
    .WithMaxProcesses(50)
    .WithDetailedLogging(true)
    .WithAutoCleanup(true, TimeSpan.FromMinutes(1))
    .Build();

// Set up event handlers
guardian.ProcessError += (sender, e) => 
    Console.WriteLine($"Error: {e.Operation} - {e.Exception.Message}");

guardian.ProcessLifecycleEvent += (sender, e) => 
    Console.WriteLine($"Event: {e.EventType} - {e.ProcessInfo}");

var process = guardian.StartProcess("myapp.exe", "--verbose");
```

## Usage Examples

### Environment Variables and Working Directory

```csharp
using var guardian = new ProcessGuardian();

var envVars = new Dictionary<string, string>
{
    { "DEBUG", "true" },
    { "CONFIG_PATH", "/etc/myapp/config.json" },
    { "LOG_LEVEL", "verbose" }
};

var process = guardian.StartProcess(
    "myapp.exe", 
    "--config config.json", 
    workingDirectory: "/path/to/working/dir",
    environmentVariables: envVars
);
```

### Custom ProcessStartInfo

```csharp
using var guardian = new ProcessGuardian();

var startInfo = new ProcessStartInfo
{
    FileName = "powershell.exe",
    Arguments = "-NoProfile -Command \"Get-Process\"",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    CreateNoWindow = true
};

var process = guardian.StartProcessWithStartInfo(startInfo);
string output = process.StandardOutput.ReadToEnd();
```

### Batch Processing

```csharp
using var guardian = ProcessGuardianBuilder.HighPerformance().Build();

// Prepare multiple processes
var processInfos = Enumerable.Range(1, 5)
    .Select(i => new ProcessStartInfo("ping", "127.0.0.1 -n 3"))
    .ToList();

// Start all processes concurrently
var processes = await guardian.StartProcessesBatchAsync(processInfos, maxConcurrency: 3);

// Wait for all to complete
bool allCompleted = await guardian.WaitForAllProcessesAsync(TimeSpan.FromSeconds(30));
Console.WriteLine($"All processes completed: {allCompleted}");
```

### Process Monitoring and Statistics

```csharp
using var guardian = new ProcessGuardian();

// Start some processes
guardian.StartProcess("notepad.exe");
guardian.StartProcess("calc.exe");

// Get real-time statistics
var stats = guardian.GetStatistics();
Console.WriteLine($"Total: {stats.TotalProcesses}, Running: {stats.RunningProcesses}");
Console.WriteLine($"Memory Usage: {stats.TotalMemoryUsage / 1024 / 1024:F1} MB");

// Filter processes by status
var runningProcesses = guardian.GetProcessesByStatus(ProcessStatus.Running);
var longRunning = guardian.GetLongRunningProcesses(TimeSpan.FromSeconds(30));

// Get detailed process information
foreach (var processInfo in runningProcesses)
{
    Console.WriteLine($"Process: {processInfo}");
    Console.WriteLine($"Runtime: {processInfo.GetRuntime():hh\\:mm\\:ss}");
    Console.WriteLine($"Exit Code: {processInfo.GetExitCode()}");
}
```

### Async Operations

```csharp
using var guardian = new ProcessGuardian();

// Start process asynchronously
var process = await guardian.StartProcessAsync("myapp.exe", cancellationToken: cts.Token);

// Gracefully terminate all processes
int terminated = await guardian.KillAllProcessesAsync(TimeSpan.FromSeconds(10));
Console.WriteLine($"Terminated {terminated} processes");

// Selective termination
int killed = await guardian.TerminateProcessesWhere(
    p => p.GetRuntime() > TimeSpan.FromMinutes(5),
    TimeSpan.FromSeconds(5)
);
```

### Error Handling and Events

```csharp
var options = new ProcessGuardianOptions
{
    ThrowOnProcessOperationFailure = false,
    EnableDetailedLogging = true
};

using var guardian = new ProcessGuardian(options);

var errors = new List<ProcessErrorEventArgs>();
guardian.ProcessError += (sender, e) => errors.Add(e);

guardian.CleanupCompleted += (sender, e) => 
    Console.WriteLine($"Cleanup: {e.ProcessesCleanedUp} cleaned, {e.ProcessesFailedToCleanup} failed");

// Operations will not throw exceptions due to configuration
guardian.StartProcess("non_existent_program.exe");
Console.WriteLine($"Captured {errors.Count} errors");
```

### Cross-Platform Example

```csharp
using var guardian = new ProcessGuardian();

string executable, arguments;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    executable = "cmd.exe";
    arguments = "/c echo Hello from Windows";
}
else
{
    executable = "/bin/bash";
    arguments = "-c 'echo Hello from Unix'";
}

var process = guardian.StartProcess(executable, arguments);
await process.WaitForExitAsync();
```

## Configuration Options

### ProcessGuardianOptions

```csharp
var options = new ProcessGuardianOptions
{
    ProcessKillTimeout = TimeSpan.FromSeconds(30),      // Graceful termination timeout
    EnableDetailedLogging = false,                      // Enable verbose logging
    ForceKillOnTimeout = true,                          // Force kill if timeout exceeded
    MaxManagedProcesses = 100,                          // Maximum concurrent processes
    AutoCleanupDisposedProcesses = true,                // Auto cleanup exited processes
    CleanupInterval = TimeSpan.FromMinutes(5),          // Cleanup check interval
    UseProcessGroupsOnUnix = true,                      // Use process groups on Unix
    ThrowOnProcessOperationFailure = false              // Exception handling behavior
};

using var guardian = new ProcessGuardian(options);
```

### Predefined Configurations

```csharp
// High performance configuration
using var guardian = ProcessGuardianBuilder.HighPerformance().Build();

// Debug configuration with detailed logging
using var guardian = ProcessGuardianBuilder.Debug().Build();

// Custom configuration
using var guardian = ProcessGuardianBuilder.Default
    .WithKillTimeout(TimeSpan.FromSeconds(15))
    .WithMaxProcesses(200)
    .WithDetailedLogging(true)
    .Build();
```

## Event Handling

```csharp
using var guardian = new ProcessGuardian();

// Process lifecycle events
guardian.ProcessLifecycleEvent += (sender, e) =>
{
    switch (e.EventType)
    {
        case ProcessLifecycleEventType.ProcessStarted:
            Console.WriteLine($"Started: {e.ProcessInfo}");
            break;
        case ProcessLifecycleEventType.ProcessExited:
            Console.WriteLine($"Exited: {e.ProcessInfo}");
            break;
        case ProcessLifecycleEventType.ProcessTerminated:
            Console.WriteLine($"Terminated: {e.ProcessInfo}");
            break;
    }
};

// Error tracking
guardian.ProcessError += (sender, e) =>
{
    Console.WriteLine($"Error in {e.Operation}: {e.Exception.Message}");
    if (e.ProcessId.HasValue)
        Console.WriteLine($"Process ID: {e.ProcessId.Value}");
};

// Cleanup notifications
guardian.CleanupCompleted += (sender, e) =>
{
    Console.WriteLine($"Cleanup completed in {e.CleanupDuration:hh\\:mm\\:ss}");
};
```

## How It Works

### Windows Implementation
- **Job Objects**: Uses Windows Job Objects with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` flag
- **Process Tree Termination**: Automatically terminates all child processes when the job is closed
- **Reliable Cleanup**: Kernel-level guarantee that processes will be terminated

### Unix Implementation (Linux/macOS)
- **Process Groups**: Creates process groups using `setpgid()` system call
- **Signal Handling**: Uses `SIGTERM` for graceful termination, `SIGKILL` for force termination
- **Process Tree Management**: Terminates entire process groups to ensure all descendants are cleaned up

### Cross-Platform Failsafes
- **AppDomain Events**: Hooks into `ProcessExit` and `CancelKeyPress` events
- **Graceful Degradation**: Falls back to basic process termination if advanced features fail
- **Compatibility Layer**: Provides .NET 5+ features for .NET Standard 2.1

## Performance and Scalability

- **Concurrent Operations**: Thread-safe with configurable concurrency limits
- **Memory Efficient**: Automatic cleanup prevents memory leaks
- **Batch Processing**: Efficient handling of multiple processes
- **Resource Management**: Proper disposal of system handles and objects
- **Monitoring Overhead**: Minimal impact with optional detailed logging

## Error Handling and Resilience

- **Non-blocking Errors**: Configurable exception handling behavior
- **Comprehensive Logging**: Detailed error tracking and event notifications
- **Graceful Degradation**: Continues operation even if some features fail
- **Recovery Mechanisms**: Automatic retry and fallback strategies
- **Resource Cleanup**: Ensures proper cleanup even in error scenarios

## Best Practices

1. **Always use `using` statements** or call `Dispose()` explicitly
2. **Configure appropriate timeouts** based on your process characteristics
3. **Handle events** for production applications to track errors and lifecycle
4. **Use builder pattern** for complex configurations
5. **Monitor statistics** in long-running applications
6. **Test cross-platform behavior** if targeting multiple operating systems
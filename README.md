# ChildProcessGuard

A cross-platform .NET library that ensures child processes automatically terminate when the parent process exits unexpectedly.

## Features

- **Cross-Platform Support**: Works on Windows, Linux, and macOS
- **Automatic Cleanup**: Child processes are automatically terminated when the parent process exits
- **Windows Job Object**: Utilizes Windows Job Objects for reliable process management on Windows
- **Process Tree Termination**: Ensures all descendant processes are also terminated
- **Environment Variable Support**: Pass custom environment variables to child processes
- **Command-Line Tool**: Includes a CLI for easy use in scripts and terminal

## Requirements

- .NET 9.0 or higher

## Installation

### Package Manager

```
Install-Package ChildProcessGuard
```

### .NET CLI

```
dotnet add package ChildProcessGuard
```

## Usage

### Basic Library Usage

```csharp
using System;
using ChildProcessGuard;

// Create a process guardian
using (var guardian = new ProcessGuardian())
{
    // Start a process
    var process = guardian.StartProcess("notepad.exe");
    
    Console.WriteLine($"Process started with PID: {process.Id}");
    Console.WriteLine("This process will automatically terminate when the application exits.");
    
    // Wait for user input
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    
    // When the using block exits, all child processes will be terminated
}
```

### Advanced Usage with Arguments and Environment Variables

```csharp
using System;
using System.Collections.Generic;
using ChildProcessGuard;

using (var guardian = new ProcessGuardian())
{
    var envVars = new Dictionary<string, string>
    {
        { "DEBUG", "true" },
        { "CONFIG_PATH", "/etc/myapp/config.json" }
    };
    
    // Start a process with arguments and environment variables
    var process = guardian.StartProcess(
        "myapp.exe", 
        "--verbose --config config.json", 
        workingDirectory: "/path/to/working/dir",
        environmentVariables: envVars
    );
    
    // You can also manually remove a process from management
    guardian.RemoveProcess(process);
    
    // Or manually terminate all managed processes
    guardian.KillAllProcesses();
}
```

### Command-Line Tool Usage

The package includes a command-line tool for easier usage:

```bash
# Install the tool
dotnet tool install --global ChildProcessGuard.Cli

# Run a program with process guarding
cpguard execute myapp.exe --args="--config config.json" --env="DEBUG=true"

# List all currently managed processes
cpguard list

# Kill all managed processes
cpguard kill
```

## How It Works

- **On Windows**: Uses Job Objects with the `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` flag to ensure child processes are terminated when the job handle is closed
- **On Linux/macOS**: Combines process tracking and explicit cleanup during AppDomain unload
- **All Platforms**: Includes a failsafe mechanism using `AppDomain.CurrentDomain.ProcessExit` event

## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
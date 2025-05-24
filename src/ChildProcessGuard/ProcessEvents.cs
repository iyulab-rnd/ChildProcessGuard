namespace ChildProcessGuard;

/// <summary>
/// Event arguments for process error events
/// </summary>
public class ProcessErrorEventArgs : EventArgs
{
    /// <summary>
    /// The operation that failed
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// The exception that occurred
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// The process ID if available
    /// </summary>
    public int? ProcessId { get; }

    /// <summary>
    /// Additional details about the error
    /// </summary>
    public string? Details { get; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of ProcessErrorEventArgs
    /// </summary>
    /// <param name="operation">The operation that failed</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="processId">The process ID if available</param>
    /// <param name="details">Additional details about the error</param>
    public ProcessErrorEventArgs(string operation, Exception exception, int? processId = null, string? details = null)
    {
        Operation = operation;
        Exception = exception;
        ProcessId = processId;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for process lifecycle events
/// </summary>
public class ProcessLifecycleEventArgs : EventArgs
{
    /// <summary>
    /// The process involved in the event
    /// </summary>
    public ManagedProcessInfo ProcessInfo { get; }

    /// <summary>
    /// The type of lifecycle event
    /// </summary>
    public ProcessLifecycleEventType EventType { get; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of ProcessLifecycleEventArgs
    /// </summary>
    /// <param name="processInfo">The process information</param>
    /// <param name="eventType">The type of lifecycle event</param>
    public ProcessLifecycleEventArgs(ManagedProcessInfo processInfo, ProcessLifecycleEventType eventType)
    {
        ProcessInfo = processInfo;
        EventType = eventType;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Types of process lifecycle events
/// </summary>
public enum ProcessLifecycleEventType
{
    /// <summary>
    /// Process was started and added to management
    /// </summary>
    ProcessStarted,

    /// <summary>
    /// Process exited normally
    /// </summary>
    ProcessExited,

    /// <summary>
    /// Process was terminated by the guardian
    /// </summary>
    ProcessTerminated,

    /// <summary>
    /// Process was removed from management
    /// </summary>
    ProcessRemoved,

    /// <summary>
    /// Process was assigned to a job object (Windows only)
    /// </summary>
    JobObjectAssigned,

    /// <summary>
    /// Process was assigned to a process group (Unix only)
    /// </summary>
    ProcessGroupAssigned
}

/// <summary>
/// Event arguments for cleanup events
/// </summary>
public class CleanupEventArgs : EventArgs
{
    /// <summary>
    /// Number of processes that were cleaned up
    /// </summary>
    public int ProcessesCleanedUp { get; }

    /// <summary>
    /// Number of processes that failed to clean up
    /// </summary>
    public int ProcessesFailedToCleanup { get; }

    /// <summary>
    /// Total time taken for the cleanup operation
    /// </summary>
    public TimeSpan CleanupDuration { get; }

    /// <summary>
    /// Timestamp when the cleanup started
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of CleanupEventArgs
    /// </summary>
    /// <param name="processesCleanedUp">Number of processes cleaned up</param>
    /// <param name="processesFailedToCleanup">Number of processes that failed to clean up</param>
    /// <param name="cleanupDuration">Time taken for cleanup</param>
    public CleanupEventArgs(int processesCleanedUp, int processesFailedToCleanup, TimeSpan cleanupDuration)
    {
        ProcessesCleanedUp = processesCleanedUp;
        ProcessesFailedToCleanup = processesFailedToCleanup;
        CleanupDuration = cleanupDuration;
        Timestamp = DateTime.UtcNow;
    }
}
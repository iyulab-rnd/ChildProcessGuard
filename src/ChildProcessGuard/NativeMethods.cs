using System.Runtime.InteropServices;

namespace ChildProcessGuard;

/// <summary>
/// Native methods for Windows API with enhanced error handling
/// </summary>
internal static class NativeMethods
{
    // P/Invoke declarations for Windows API
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool TerminateJobObject(IntPtr hJob, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool QueryInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength, out uint lpReturnLength);

    // Unix system calls
    [DllImport("libc", EntryPoint = "setpgid", SetLastError = true)]
    internal static extern int SetProcessGroup(int pid, int pgid);

    [DllImport("libc", EntryPoint = "killpg", SetLastError = true)]
    internal static extern int KillProcessGroup(int pgrp, int sig);

    [DllImport("libc", EntryPoint = "getpgid", SetLastError = true)]
    internal static extern int GetProcessGroup(int pid);

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public IntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
    {
        public long TotalUserTime;
        public long TotalKernelTime;
        public long ThisPeriodTotalUserTime;
        public long ThisPeriodTotalKernelTime;
        public uint TotalPageFaultCount;
        public uint TotalProcesses;
        public uint ActiveProcesses;
        public uint PeakActiveProcesses;
    }

    internal enum JobObjectInfoType
    {
        AssociateCompletionPortInformation = 7,
        BasicLimitInformation = 2,
        BasicUIRestrictions = 4,
        EndOfJobTimeInformation = 6,
        ExtendedLimitInformation = 9,
        SecurityLimitInformation = 5,
        GroupInformation = 11,
        BasicAccountingInformation = 1
    }

    // JOB_OBJECT_LIMIT constants
    internal const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
    internal const uint JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x800;
    internal const uint JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK = 0x1000;

    // Unix signals
    internal const int SIGTERM = 15;
    internal const int SIGKILL = 9;

    /// <summary>
    /// Gets the last Win32 error code
    /// </summary>
    /// <returns>The error code</returns>
    internal static int GetLastError()
    {
        return Marshal.GetLastWin32Error();
    }

    /// <summary>
    /// Checks if the current platform supports Unix system calls
    /// </summary>
    /// <returns>True if Unix system calls are supported</returns>
    internal static bool IsUnixPlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
using System.Text;

namespace SpectroTune;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class ProcessUtils
{
    public static string ExecuteCommand(string path, string[] cmdArgs, DataReceivedEventHandler? dataReceived = null, DataReceivedEventHandler? errorReceived = null)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ExecuteCommandWindows(path, cmdArgs, dataReceived, errorReceived);
        }
        else
        {
            return ExecuteCommandUnix(path, cmdArgs, dataReceived, errorReceived);
        }
    }

    private static string ExecuteCommandWindows(string path, string[] cmdArgs, DataReceivedEventHandler? dataReceived = null, DataReceivedEventHandler? errorReceived = null)
    {
        IntPtr job = CreateJobObject(IntPtr.Zero, null);
        if (job == IntPtr.Zero)
        {
            throw new Exception("Could not create job object");
        }

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

        int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
        Marshal.StructureToPtr(info, extendedInfoPtr, false);

        if (!SetInformationJobObject(job, JobObjectExtendedLimitInformation, extendedInfoPtr, (uint)length))
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
            throw new Exception("Could not set job object information");
        }

        Process p = new Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.FileName = path;
        p.StartInfo.Arguments = string.Join(' ', cmdArgs);
        p.StartInfo.CreateNoWindow = true;
        p.EnableRaisingEvents = true;

        var output = new StringBuilder();

        if (dataReceived != null) p.OutputDataReceived += dataReceived;
        if (errorReceived != null) p.ErrorDataReceived += errorReceived;
        p.OutputDataReceived += (sender, eventArgs) =>
        {
            if (eventArgs.Data != null) output.AppendLine(eventArgs.Data);
        };

        p.Start();
    
        if (!AssignProcessToJobObject(job, p.Handle))
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
            throw new Exception("Could not assign process to job object");
        }

        p.BeginOutputReadLine();
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            throw new Exception(p.StandardError.ReadToEnd());
        }

        Marshal.FreeHGlobal(extendedInfoPtr);
        return output.ToString().Trim();
    }


    private static string ExecuteCommandUnix(string path, string[] cmdArgs, DataReceivedEventHandler? dataReceived = null, DataReceivedEventHandler? errorReceived = null)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = path,
            Arguments = string.Join(' ', cmdArgs),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        Process p = new Process { StartInfo = psi };
        p.EnableRaisingEvents = true;
        
        p.Start();

        // Set the process group ID to the process ID
        // This ensures all child processes are in the same group
        Setpgid(p.Id, p.Id);

        var output = new StringBuilder();
        var error = new StringBuilder();
        if (dataReceived != null) p.OutputDataReceived += dataReceived;
        if (errorReceived != null) p.ErrorDataReceived += errorReceived;
        p.OutputDataReceived += (sender, eventArgs) =>
        {
            if (eventArgs.Data != null) output.AppendLine(eventArgs.Data);
        };
        p.ErrorDataReceived += (sender, eventArgs) =>
        {
            if (eventArgs.Data != null) error.AppendLine(eventArgs.Data);
        };
        
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            throw new Exception(error.ToString());
        }

        return output.ToString().Trim();
    }

    // P/Invoke declarations for Windows
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetInformationJobObject(IntPtr hJob, int JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public IntPtr MinimumWorkingSetSize;
        public IntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public IntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public IntPtr ProcessMemoryLimit;
        public IntPtr JobMemoryLimit;
        public IntPtr PeakProcessMemoryUsed;
        public IntPtr PeakJobMemoryUsed;
    }

    const int JobObjectExtendedLimitInformation = 9;
    const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    // P/Invoke declaration for Unix
    [DllImport("libc", SetLastError = true)]
    private static extern int Setpgid(int pid, int pgid);
}

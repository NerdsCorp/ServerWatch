using System.Runtime.InteropServices;
using System.Diagnostics;
namespace ServerWatch.Agent;
internal static class WindowsMetrics
{
    private static readonly object Gate = new(); private static ulong _previousIdle, _previousKernel, _previousUser; private static bool _hasCpuSample;
    public static SystemSample Read()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("ServerWatch Agent is designed for Windows Server.");
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) throw new InvalidOperationException("Could not read Windows CPU counters.");
        var idleValue=ToUInt64(idle); var kernelValue=ToUInt64(kernel); var userValue=ToUInt64(user); double cpu=0;
        lock(Gate){if(_hasCpuSample){var total=(kernelValue-_previousKernel)+(userValue-_previousUser);var idleDelta=idleValue-_previousIdle;if(total>0)cpu=Math.Clamp((1d-(double)idleDelta/total)*100d,0d,100d);}_previousIdle=idleValue;_previousKernel=kernelValue;_previousUser=userValue;_hasCpuSample=true;}
        var memory=new MemoryStatusEx{Length=(uint)Marshal.SizeOf<MemoryStatusEx>()}; if(!GlobalMemoryStatusEx(ref memory))throw new InvalidOperationException("Could not read Windows memory counters.");
        return new SystemSample(Math.Round(cpu,2),memory.TotalPhysical-memory.AvailablePhysical,memory.TotalPhysical);
    }
    public static IReadOnlyList<ProcessStatus> ReadProcesses(IReadOnlyList<string> names)
    {
        var results=new List<ProcessStatus>(names.Count);
        foreach(var name in names)
        {
            var processes=Process.GetProcessesByName(name);
            try{results.Add(new ProcessStatus(name,processes.Length>0));}
            finally{foreach(var process in processes)process.Dispose();}
        }
        return results;
    }
    private static ulong ToUInt64(FileTime time)=>((ulong)time.High<<32)|time.Low;
    [StructLayout(LayoutKind.Sequential)] private readonly struct FileTime{public readonly uint Low;public readonly uint High;}
    [StructLayout(LayoutKind.Sequential)] private struct MemoryStatusEx{public uint Length;public uint MemoryLoad;public ulong TotalPhysical;public ulong AvailablePhysical;public ulong TotalPageFile;public ulong AvailablePageFile;public ulong TotalVirtual;public ulong AvailableVirtual;public ulong AvailableExtendedVirtual;}
    [DllImport("kernel32.dll",SetLastError=true)] private static extern bool GetSystemTimes(out FileTime idleTime,out FileTime kernelTime,out FileTime userTime);
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)] private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
internal sealed record SystemSample(double CpuPercent,ulong MemoryUsedBytes,ulong MemoryTotalBytes);

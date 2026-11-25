using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ServerWatch.Agent;

internal static class ServiceInstaller
{
    private const string ServiceName = "ServerWatchAgent";
    private const uint ScManagerConnect = 0x0001;
    private const uint ScManagerCreateService = 0x0002;
    private const uint ServiceAllAccess = 0xF01FF;
    private const uint ServiceWin32OwnProcess = 0x00000010;
    private const uint ServiceAutoStart = 0x00000002;
    private const uint ServiceErrorNormal = 0x00000001;

    public static string InstallDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ServerWatch", "Agent");

    public static bool IsInstalled()
    {
        using var services = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var service = services.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{ServiceName}", writable: false);
        return service is not null;
    }

    public static void Install(string sourceDirectory)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Service installation is only supported on Windows.");
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new UnauthorizedAccessException("Run the installer with administrator approval.");
        if (IsInstalled()) throw new InvalidOperationException("The ServerWatch Agent service is already registered.");

        sourceDirectory = Path.GetFullPath(sourceDirectory);
        var destination = Path.GetFullPath(InstallDirectory);
        _ = AgentSettings.Load();
        var sourcePrefix = sourceDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (destination.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a source folder outside the installation folder.");

        var sourceConfig = Path.Combine(sourceDirectory, ".env");
        if (!File.Exists(sourceConfig)) throw new FileNotFoundException("Copy the site .env file next to the agent before installing it.", sourceConfig);

        if (!sourceDirectory.Equals(destination, StringComparison.OrdinalIgnoreCase))
            CopyDistribution(sourceDirectory, destination);

        var configPath = Path.Combine(destination, ".env");
        if (!File.Exists(configPath)) throw new FileNotFoundException("The agent .env file was not copied to the installation folder.", configPath);
        RestrictConfigFile(configPath);
        RegisterAndStartService(Path.Combine(destination, "ServerWatch.Agent.exe"));
    }

    private static void CopyDistribution(string source, string destination)
    {
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException($"Agent folder not found: {source}");
        var destinationPrefix = destination.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).StartsWith(destinationPrefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (!files.Any(path => Path.GetFileName(path).Equals("ServerWatch.Agent.exe", StringComparison.OrdinalIgnoreCase)))
            throw new FileNotFoundException("ServerWatch.Agent.exe was not found beside the running application.");

        Directory.CreateDirectory(destination);
        foreach (var sourceFile in files)
        {
            var relativePath = Path.GetRelativePath(source, sourceFile);
            var destinationFile = Path.GetFullPath(Path.Combine(destination, relativePath));
            if (!destinationFile.StartsWith(destinationPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A distribution file resolves outside the installation folder.");
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }
    }

    private static void RestrictConfigFile(string path)
    {
        using var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "icacls.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList = { path, "/inheritance:r", "/grant:r", "*S-1-5-19:R", "*S-1-5-32-544:F", "*S-1-5-18:F" }
        }) ?? throw new InvalidOperationException("Could not start icacls.exe to protect the agent .env file.");
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("Could not restrict access to the agent .env file: " + process.StandardError.ReadToEnd());
    }

    private static void RegisterAndStartService(string executablePath)
    {
        var manager = OpenSCManager(null, null, ScManagerConnect | ScManagerCreateService);
        if (manager == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the Windows Service Control Manager.");
        IntPtr service = IntPtr.Zero;
        try
        {
            var commandLine = "\"" + executablePath + "\"";
            service = CreateService(manager, ServiceName, "ServerWatch Agent", ServiceAllAccess,
                ServiceWin32OwnProcess, ServiceAutoStart, ServiceErrorNormal, commandLine,
                null, IntPtr.Zero, null, "NT AUTHORITY\\LocalService", null);
            if (service == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not register the ServerWatch Agent service.");
            if (!StartService(service, 0, IntPtr.Zero))
            {
                var error = Marshal.GetLastWin32Error();
                DeleteService(service);
                throw new Win32Exception(error, "The service was registered but could not be started.");
            }
        }
        finally
        {
            if (service != IntPtr.Zero) CloseServiceHandle(service);
            CloseServiceHandle(manager);
        }
    }

    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint desiredAccess);

    [DllImport("advapi32.dll", EntryPoint = "CreateServiceW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateService(IntPtr manager, string serviceName, string displayName, uint desiredAccess,
        uint serviceType, uint startType, uint errorControl, string binaryPathName, string? loadOrderGroup,
        IntPtr tagId, string? dependencies, string serviceStartName, string? password);

    [DllImport("advapi32.dll", EntryPoint = "StartServiceW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StartService(IntPtr service, uint argumentCount, IntPtr arguments);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteService(IntPtr service);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseServiceHandle(IntPtr serviceHandle);
}

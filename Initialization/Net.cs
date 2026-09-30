using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace N2N_Saenai.Initialization;

public static class TapManager
{
    public static bool IsInstalled() => NetworkInterface.GetAllNetworkInterfaces().Any(adapter =>
        adapter.Description.Contains("TAP-Windows Adapter V9", StringComparison.OrdinalIgnoreCase));

    public static async Task<DriverInstallResult> EnsureInstalledAsync()
    {
        if (IsInstalled()) return DriverInstallResult.AlreadyInstalled;
        var inf = Path.Combine(AppContext.BaseDirectory, "drivers", "tap0901", "OemVista.inf");
        if (!File.Exists(inf)) return DriverInstallResult.FilesMissing;
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "pnputil.exe", Arguments = $"/add-driver \"{inf}\" /install",
                UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden
            });
            if (process is null) return DriverInstallResult.Failed;
            await process.WaitForExitAsync();
            return IsInstalled() ? DriverInstallResult.Installed : DriverInstallResult.Failed;
        }
        catch (Win32Exception) { return DriverInstallResult.Cancelled; }
    }
}

public enum DriverInstallResult { AlreadyInstalled, Installed, Cancelled, FilesMissing, Failed }

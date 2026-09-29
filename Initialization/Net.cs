using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace N2N_Saenai.Initialization
{
    public static class TapManager
    {

        /// <summary>
        /// 保证 TAP 一定存在
        /// </summary>
        public static async Task<bool> EnsureInstalledAsync()
        {
            if (Exists())
            {
                return true;
            }

            return await InstallAsync();
        }

        /// <summary>
        /// 检查是否存在 TAP 网卡
        /// </summary>
        /// 

        private static bool Exists()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                string description = adapter.Description;

                if (description.Contains(
                        "TAP-Windows Adapter V9",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// 自动安装 TAP 网卡
        /// </summary>
        /// 
        private static async Task<bool> InstallAsync()
        {
            string driverFolder = Path.Combine(
                AppContext.BaseDirectory,
                "driver");

            string tapinstallPath = Path.Combine(
                driverFolder,
                "tapinstall.exe");

            string infPath = Path.Combine(
                driverFolder,
                "OemVista.inf");

            if (!File.Exists(tapinstallPath))
            {
                throw new FileNotFoundException(
                    "找不到 tapinstall.exe");
            }

            if (!File.Exists(infPath))
            {
                throw new FileNotFoundException(
                    "找不到 OemVista.inf");
            }

            Process process = new();

            process.StartInfo.FileName = tapinstallPath;

            process.StartInfo.Arguments =
                $"install \"{infPath}\" tap0901";

            process.StartInfo.UseShellExecute = true;

            // 请求管理员权限
            process.StartInfo.Verb = "runas";

            try
            {
                process.Start();

                await process.WaitForExitAsync();

                return process.ExitCode == 0 && Exists();
            }
            catch (Win32Exception)
            {
                return false;
            }
        }
    }
}
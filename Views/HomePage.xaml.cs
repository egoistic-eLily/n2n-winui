using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using N2N_Saenai.Core;
using N2N_Saenai.Initialization;
using N2N_Saenai.Service;
using System.Collections.ObjectModel;
using System;

namespace N2N_Saenai.Views;

public sealed partial class HomePage : Page
{
    private readonly EdgeClientSession _session = new();
    public ObservableCollection<LogItem> LogItems { get; } = [];

    public HomePage()
    {
        InitializeComponent();
        _session.LogReceived += (_, log) => DispatcherQueue.TryEnqueue(() => AddLog(log.Source, log.Message));
        _session.Connected += (_, ip) => DispatcherQueue.TryEnqueue(() =>
        {
            SetConnectedOk(ip);
            AddLog("系统", string.IsNullOrEmpty(ip)
                ? "连接成功。"
                : $"连接成功，虚拟 IP：{ip}");
        });
        _session.Disconnected += (_, _) => DispatcherQueue.TryEnqueue(() => SetDisconnected());
        var data = Login.UserData!;
        CommunityText.Text = $"设备：{data.DeviceName}    ·    网络：{data.CommunityName}";
        EndpointText.Text = $"超级节点：{data.SupernodeIp}:{data.SupernodePort}";
        AddLog("系统", "登录成功，配置已加载。");
        AddLog("系统", TapManager.IsInstalled() ? "TAP 驱动已就绪。" : "尚未检测到 TAP 驱动；连接前需要安装。");
    }

    private async void DriverButton_Click(object sender, RoutedEventArgs e)
    {
        DriverButton.IsEnabled = false; DriverButton.Content = "正在检查…";
        var result = await TapManager.EnsureInstalledAsync();
        DriverButton.IsEnabled = true; DriverButton.Content = "检查 TAP 驱动";
        var message = result switch
        {
            DriverInstallResult.AlreadyInstalled => "TAP 驱动已就绪。",
            DriverInstallResult.Installed => "TAP 驱动已安装并验证。",
            DriverInstallResult.Cancelled => "已取消管理员授权，未安装驱动。",
            DriverInstallResult.FilesMissing => "驱动文件未随应用发布。",
            _ => "驱动安装未完成，请在管理员终端中检查 pnputil 输出。"
        };
        AddLog("驱动", message);
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectButton.IsEnabled = false;
        try
        {
            if (_session.IsRunning) { await _session.StopAsync(); SetDisconnected(); AddLog("系统", "连接已由用户断开。"); return; }
            if (!TapManager.IsInstalled()) { AddLog("驱动", "请先点击“检查 TAP 驱动”并完成安装。 "); return; }
            ConnectButton.Content = "正在连接…";
            SetConnecting();
            await _session.StartAsync(Login.UserData!);
            ConnectButton.Content = "断开连接";
        }
        catch (Exception ex) { AddLog("错误", ex.Message); SetDisconnected(); }
        finally { ConnectButton.IsEnabled = true; }
    }

    private void SetDisconnected()
    {
        StatusText.Text = "未连接";
        StatusDot.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 148, 163, 184));
        ConnectButton.Content = "连接网络";
        IpText.Text = string.Empty;
        IpText.Visibility = Visibility.Collapsed;
    }

    private void SetConnecting()
    {
        StatusText.Text = "连接中";
        StatusDot.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 245, 158, 11));
        IpText.Visibility = Visibility.Collapsed;
    }

    private void SetConnectedOk(string ip)
    {
        StatusText.Text = "连接成功";
        StatusDot.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 34, 197, 94));
        ConnectButton.Content = "断开连接";
        IpText.Text = string.IsNullOrEmpty(ip) ? "虚拟 IP：未知" : $"虚拟 IP：{ip}";
        IpText.Visibility = Visibility.Visible;
    }
    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogItems.Clear();
    private void AddLog(string source, string message) => LogItems.Add(new LogItem(DateTime.Now.ToString("HH:mm:ss"), source, message));
}

public sealed class LogItem
{
    public LogItem(string time, string source, string message) => (Time, Source, Message) = (time, source, message);
    public string Time { get; set; }
    public string Source { get; set; }
    public string Message { get; set; }
}

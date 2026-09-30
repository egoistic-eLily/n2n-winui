using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace N2N_Saenai.Views;

public sealed partial class LoginPage : Page
{
    // 应用不内置任何默认服务器地址：每个部署指向自己的 n2n-user-server 实例，
    // 地址在登录页填写并保存在本机，供下次启动时回填。
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "n2n-winui", "server-url.txt");

    // 上一次成功登录的账号与密码（本机缓存，明文保存——应用本身以管理员运行，
    // 登录凭据仅存在于本机）。
    private static readonly string LoginCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "n2n-winui", "login.txt");

    public LoginPage()
    {
        InitializeComponent();
        ServerUrl.Text = LoadServerUrl();
        var (lastUserId, lastPassword) = LoadLoginCache();
        Username.Text = lastUserId ?? string.Empty;
        Password.Password = lastPassword ?? string.Empty;
    }

    private static string LoadServerUrl()
    {
        try { return File.Exists(SettingsPath) ? File.ReadAllText(SettingsPath).Trim() : string.Empty; }
        catch (IOException) { return string.Empty; }
        catch (UnauthorizedAccessException) { return string.Empty; }
    }

    private static void SaveServerUrl(string url)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, url);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static (string? UserId, string? Password) LoadLoginCache()
    {
        try
        {
            if (!File.Exists(LoginCachePath)) return (null, null);
            var lines = File.ReadAllLines(LoginCachePath);
            return (lines.ElementAtOrDefault(0), lines.ElementAtOrDefault(1));
        }
        catch (IOException) { return (null, null); }
        catch (UnauthorizedAccessException) { return (null, null); }
    }

    private static void SaveLoginCache(string userId, string password)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LoginCachePath)!);
            File.WriteAllLines(LoginCachePath, new[] { userId, password });
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e) => await SignInAsync();
    private async void Password_KeyDown(object sender, KeyRoutedEventArgs e) { if (e.Key == Windows.System.VirtualKey.Enter) await SignInAsync(); }
    private async void ServerUrl_KeyDown(object sender, KeyRoutedEventArgs e) { if (e.Key == Windows.System.VirtualKey.Enter) await SignInAsync(); }

    private async Task SignInAsync()
    {
        HintText.Visibility = Visibility.Collapsed;

        var serverUrl = ServerUrl.Text.Trim();
        if (serverUrl.Length == 0)
        {
            HintText.Text = "请先填写服务器地址。";
            HintText.Visibility = Visibility.Visible;
            return;
        }
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            HintText.Text = "服务器地址格式不正确，例如 https://your-server/login。";
            HintText.Visibility = Visibility.Visible;
            return;
        }

        LoginButton.IsEnabled = false; LoginButton.Content = "正在验证…";
        try
        {
            var success = await Service.Login.UploadRequestAsync(Username.Text, Password.Password, serverUrl);
            if (!success)
            {
                HintText.Text = Service.Login.LastError ?? "无法登录。请检查帐号、密码或网络连接。";
                HintText.Visibility = Visibility.Visible;
                return;
            }
            SaveServerUrl(serverUrl);
            SaveLoginCache(Username.Text.Trim(), Password.Password);
            Frame.Navigate(typeof(HomePage));
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Content = "继续";
        }
    }
}

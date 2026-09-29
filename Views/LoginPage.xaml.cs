using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Maps;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace N2N_Saenai.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        public async void Init() {
            bool Tapsu = await Initialization.TapManager.EnsureInstalledAsync();
            if (!Tapsu) {
                await MessageBox.Show(this, $"错误", $"网卡安装失败");
                Debug.WriteLine("网卡安装失败");
            }
            await MessageBox.Show(this, $"错误", $"网卡验证/安装成功");
            Debug.WriteLine("网卡安装成功");
            return;
        }
        

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Init();
            LoginButton.IsEnabled = false;
            LoginButton.Content = "登录中...";
            string username = Username.Text;
            string password = Password.Password;
            try
            {
                bool success = await Service.Login.UploadRequest(
               username, password);
                if (!success) {
                    await MessageBox.Show(this, $"提示", $"登录失败");
                    LoginButton.IsEnabled = true;
                    LoginButton.Content = "登录";
                    return;
                }
                await MessageBox.Show(this, $"提示", $"登录成功");
                LoginButton.IsEnabled = true;
                LoginButton.Content = "登录";
                this.Frame.Navigate(typeof(HomePage));
                return;
            }
            catch {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "登录";
                await MessageBox.Show(this, $"提示", $"登录失败");
            }
            
        }
    }
}

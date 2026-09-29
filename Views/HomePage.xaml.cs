using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using N2N_Saenai.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace N2N_Saenai.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {

        private bool _connected = false;
        public HomePage()
        {
            InitializeComponent();
            Init();
        }
        private void Init() {
            UserText.Text =
            $"用户：{Service.Login.userdata.device_name}";
            CommunityText.Text =
            $"网络：{Service.Login.userdata.community_name}";
            if (Service.Login.userdata.supernode_ip == null || Service.Login.userdata.supernode_ip == "")
            {
                IPText.Text = "自动配置";
            }
            else IPText.Text =$"IP：{Service.Login.userdata.supernode_ip}";
            AddLog("登录成功");
            AddLog("配置加载完成");
        }
        private async void  ConnectButton_Click(object sender, RoutedEventArgs e) {
            string path = Path.Combine(AppContext.BaseDirectory, "driver");
            _connected = !_connected;
            if (_connected) {
                StatusText.Text = "● 已连接";
                ConnectButton.Content = "断开";
                AddLog("已连接");
                await Core.Pipe.LaunchEdgeClient(Path.Combine(path, "edge.exe"), Service.Login.userdata);
            }
            else
            {
                StatusText.Text = "● 未连接";
                ConnectButton.Content = "连接";
                AddLog("已断开");
                await Core.Pipe.StopEdgeClient();
            }

        }
        private void AddLog(string text)
        {
            LogBox.Text +=
                $"[{DateTime.Now:HH:mm:ss}] {text}\n";
        }
    }
}

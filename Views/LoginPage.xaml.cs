using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace N2N_Saenai.Views;

public sealed partial class LoginPage : Page
{
    public LoginPage() => InitializeComponent();

    private async void LoginButton_Click(object sender, RoutedEventArgs e) => await SignInAsync();
    private async void Password_KeyDown(object sender, KeyRoutedEventArgs e) { if (e.Key == Windows.System.VirtualKey.Enter) await SignInAsync(); }

    private async Task SignInAsync()
    {
        HintText.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = false; LoginButton.Content = "正在验证…";
        try
        {
            var success = await Service.Login.UploadRequestAsync(Username.Text, Password.Password);
            if (!success)
            {
                HintText.Text = Service.Login.LastError ?? "无法登录。请检查帐号、密码或网络连接。";
                HintText.Visibility = Visibility.Visible;
                return;
            }
            Frame.Navigate(typeof(HomePage));
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Content = "继续";
        }
    }
}

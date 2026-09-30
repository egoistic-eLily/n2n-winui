using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using N2N_Saenai.Views;
using Windows.Graphics;

namespace N2N_Saenai;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "n2n-winui";
        AppWindow.Resize(new SizeInt32(1060, 720));
        if (AppWindow.Presenter is OverlappedPresenter presenter) presenter.IsMaximizable = false;
        RootFrame.Navigate(typeof(LoginPage));
    }
}

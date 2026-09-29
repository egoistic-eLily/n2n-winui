using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using N2N_Saenai.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace N2N_Saenai
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppWindow.Resize(new SizeInt32(1000, 650));
            this.Title = "N2N Launcher";

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;      // 禁止拖动边框调整大小
                presenter.IsMaximizable = false;   // 禁用最大化按钮
            }

            RootFrame.Navigate(typeof(LoginPage));
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Chat
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AuthWindow : Window
    {
        private static Window authWindow;

        public AuthWindow()
        {
            authWindow = this;
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            Microsoft.UI.Windowing.AppWindow appWindow =
                Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                    Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd)
                );

            appWindow.Resize(new Windows.Graphics.SizeInt32(450, 600));
            var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;    // Disables dragging edges
                presenter.IsMaximizable = false;  // Disables the maximize button
            }

            InitializeComponent();
            AuthFrame.Navigate(typeof(LoginPage));
        }

        public static void CloseRootWindow()
        {
            authWindow.Close();
        }
    }
}

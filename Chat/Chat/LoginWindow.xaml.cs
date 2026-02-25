using Microsoft.UI.Xaml;
using System;

namespace Chat
{
    public sealed partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();             
             
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            Microsoft.UI.Windowing.AppWindow appWindow =
                Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                    Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd)
                );

            appWindow.Resize(new Windows.Graphics.SizeInt32(450, 600));
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var chatWindow = new MainWindow();
            chatWindow.Activate();
            this.Close();
        }
    }
}
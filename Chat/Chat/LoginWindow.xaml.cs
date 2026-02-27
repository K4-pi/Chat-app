using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Chat
{
    public sealed partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

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
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;

            foreach (char c in ":@'/,.><-_=+()$#!?")
            {
                if (UsernameField.Text.Contains(c) || PasswordField.Password.Contains(c))
                {
                    ErrorLoginText.Text = "No special symbols!";
                    LoginButton.IsEnabled = true;
                    UsernameField.Text = "";
                    PasswordField.Password = "";
                    return;
                }
            }

            if (UsernameField.Text == "")
            {
                ErrorLoginText.Text = "No username provided!";         
                
            }
            else if (PasswordField.Password == "")
            {
                ErrorLoginText.Text = "No password provided!";
            }
            else
            {
                var client = new TcpChatClient();
                bool isConnected = await client.ConnectAsync("127.0.0.1", 9000);

                if (isConnected)
                {
                    string request = $"LOGIN:{UsernameField.Text}@{PasswordField.Password}";
                    string response = await client.SendAndReceiveAsync(request);

                    if (response.Split(':')[0] == "AUTH_SUCCESS")
                    {
                        WelcomeText.Text = response;

                        string userId = response.Split(':')[1];

                        var chatWin = new MainWindow(userId, client);
                        chatWin.Activate();
                        this.Close();
                    }
                    else
                    {
                        ErrorLoginText.Text = "Wrong username and/or password";
                    }
                }
                else
                {
                    ErrorLoginText.Text = "Server conncetion error";
                }
            }
            LoginButton.IsEnabled = true;
        }
    }
}
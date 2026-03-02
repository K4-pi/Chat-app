using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChatApp
{
    public partial class LoginPage : UserControl
    {
        private string address = "127.0.0.1";
        private int port = 9000;

        public LoginPage()
        {
            InitializeComponent();
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            // Swapping content via our static Instance in AuthWindow
            AuthWindow.Instance.AuthContentArea.Content = new RegisterPage();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validation Logic
            string user = UsernameField.Text ?? "";
            string pass = PasswordField.Text ?? ""; // Avalonia uses .Text even for password mode

            if (string.IsNullOrWhiteSpace(user))
            {
                ErrorLoginText.Text = "No username provided!";
                return;
            }
            if (string.IsNullOrWhiteSpace(pass))
            {
                ErrorLoginText.Text = "No password provided!";
                return;
            }

            // Forbidden character check
            char[] forbidden = ":@'/,.-_=+$#!?%^&*|(){}[]><".ToCharArray();
            if (user.Any(c => forbidden.Contains(c)) || pass.Any(c => forbidden.Contains(c)))
            {
                ErrorLoginText.Text = "No special symbols!";
                UsernameField.Text = "";
                PasswordField.Text = "";
                return;
            }

            // 2. Connection Logic
            LoginButton.IsEnabled = false;
            var client = new TcpChatClient();

            try
            {
                if (await client.ConnectAsync(address, port))
                {
                    string request = $"LOGIN:{user}@{pass}";
                    string response = await client.SendAndReceiveAsync(request);

                    if (response.StartsWith("AUTH_SUCCESS:"))
                    {
                        string userId = response.Substring(13);

                        // Open Main Chat Window
                        var chatWin = new MainWindow(userId, client);
                        chatWin.Show(); // Use .Show() to display the window

                        // Close the Login Window
                        AuthWindow.CloseRootWindow();
                    }
                    else
                    {
                        ErrorLoginText.Text = "Wrong username and/or password";
                        await client.CloseConnectionAsync();
                    }
                }
                else
                {
                    ErrorLoginText.Text = "Server connection error";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login exception: {ex.Message}");
                ErrorLoginText.Text = "An unexpected error occurred.";
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }
    }
}

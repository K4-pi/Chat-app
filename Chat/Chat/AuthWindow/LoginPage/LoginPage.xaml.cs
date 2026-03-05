using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

using Chat.Utilities;

namespace Chat
{
    public sealed partial class LoginPage : Page
    {
        private string address = "127.0.0.1";
        private int port = 9000;

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void SignUp_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(RegisterPage));
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameField.Text;
            string password = PasswordField.Password;

            if (username == "")
            {
                ErrorLoginText.Text = "No username provided!";         
                
            }
            else if (password == "")
            {
                ErrorLoginText.Text = "No password provided!";
            }
            else
            {
                foreach (char c in ":@'/,.-_=+$#!?%^&*|(){}[]><")
                {
                    if (username.Contains(c) || password.Contains(c))
                    {
                        ErrorLoginText.Text = "No special symbols!";
                        UsernameField.Text = "";
                        PasswordField.Password = "";
                        return;
                    }
                }

                LoginButton.IsEnabled = false;
                var client = new TcpChatClient();
                try
                {
                    if (await client.ConnectAsync(address, port))
                    {
                        string request = $"LOGIN:{username}@{password}";
                        string response = await client.SendAndReceiveAsync(request);

                        if (response.StartsWith("AUTH_SUCCESS:"))
                        {
                            string userId = response.Substring(13);

                            var chatWin = new MainWindow(userId, client);
                            AuthWindow.CloseRootWindow();
                            chatWin.Activate();
                            return;
                        }
                        else
                        {
                            ErrorLoginText.Text = "Wrong username and/or password";
                            await client.CloseConnectionAsync();
                        }
                    }
                    else
                    {
                        ErrorLoginText.Text = "Server conncetion error";
                        await client.CloseConnectionAsync();
                    }
                }
                catch(Exception ex)
                {
                    await client.CloseConnectionAsync();
                    Debug.WriteLine($"Login exception: {ex}");
                }
                finally
                {
                    LoginButton.IsEnabled = true;
                }
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChatApp
{
    public partial class RegisterPage : UserControl
    {
        private string address = "127.0.0.1";
        private int port = 9000;

        public RegisterPage()
        {
            InitializeComponent();
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to Login
            AuthWindow.Instance.AuthContentArea.Content = new LoginPage();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string user = UsernameField.Text ?? "";
            string pass = PasswordField.Text ?? "";

            // Validation (Reuse your logic)
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                ErrorLoginText.Text = "Fields cannot be empty!";
                return;
            }

            char[] forbidden = ":@'/,.-_=+$#!?%^&*|(){}[]><".ToCharArray();
            if (user.Any(c => forbidden.Contains(c)) || pass.Any(c => forbidden.Contains(c)))
            {
                ErrorLoginText.Text = "No special symbols allowed!";
                return;
            }

            RegisterButton.IsEnabled = false;
            var client = new TcpChatClient();

            try
            {
                if (await client.ConnectAsync(address, port))
                {
                    // Assuming your server expects REGISTER:user@pass
                    string request = $"REGISTER:{user}@{pass}";
                    string response = await client.SendAndReceiveAsync(request);

                    if (response.StartsWith("REG_SUCCESS"))
                    {
                        // Successfully registered! Go back to login or auto-login.
                        AuthWindow.Instance.AuthContentArea.Content = new LoginPage();
                    }
                    else
                    {
                        ErrorLoginText.Text = "Username already exists or server error.";
                        await client.CloseConnectionAsync();
                    }
                }
                else
                {
                    ErrorLoginText.Text = "Cannot connect to server.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Register error: {ex.Message}");
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }
    }
}

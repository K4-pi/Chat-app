using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Chat
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RegisterPage : Page
    {
        private string address = "127.0.0.1";
        private int port = 9000;

        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void SignIn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorLoginText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);

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
                        RegisterButton.IsEnabled = true;
                        return;
                    }
                }

                RegisterButton.IsEnabled = false;
                TcpChatClient client = new TcpChatClient();
                try
                {
                    if (await client.ConnectAsync(address, port))
                    {
                        string request = $"REGISTER:{username}@{password}";
                        string response = await client.SendAndReceiveAsync(request);

                        if (response.StartsWith("REGISTER_SUCCESS"))
                        {
                            UsernameField.Text = "";
                            PasswordField.Password = "";

                            ErrorLoginText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
                            ErrorLoginText.Text = "Account created successfully!";
                        }
                        else
                        {
                            ErrorLoginText.Text = "Username already taken!";
                        }
                    }
                    else
                    {
                        ErrorLoginText.Text = "Server conncetion error";
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Register exception: {ex}");
                }
                finally
                {
                    await client.CloseConnectionAsync();
                    RegisterButton.IsEnabled = true;
                }
            }
        }
    }
}

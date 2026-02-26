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
using Windows.ApplicationModel.Chat;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Chat
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        TcpChatClient client;

        public MainWindow(string userId, TcpChatClient _client)
        {
            InitializeComponent();
            Title = $"Session: {userId}";

            client = _client;
            _ = client.ListenAsync(OnMessageReceived);
        }

        public void OnMessageReceived(string msg)
        {
            // "MSG:general:User@Hello World!"
            if (!msg.StartsWith("MSG:")) return;

            // "general:User@Hello World!"
            string formated = msg.Substring(4); // Remove "MSG:" prefix

            // "[general] [User@Hello World!]"
            string[] tokens = formated.Split(':', 2);
            if (tokens.Length < 2) return;

            // "[User] [Hello World!]"
            string[] content = tokens[1].Split('@', 2);

            string user = content[0];
            string text = content[1];

            this.DispatcherQueue.TryEnqueue(() =>
            {
                MessageList.Items.Add(new Message
                {
                    Username = user,
                    Text = text,
                    SentAt = DateTime.UtcNow.ToString("HH:mm")
                });

                MessageList.ScrollIntoView(MessageList.Items.LastOrDefault());
            });
        }

        public async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            /*
                We will not add message to our own list,
                Server will broadcast it back...
                It should do that
            */

            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            client.SendAsync($"MSG:{text}");

            MessageInput.Text = "";
        }
    }
}

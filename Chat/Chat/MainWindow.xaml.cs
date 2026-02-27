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
            Debug.WriteLine("DEBUG chat window");
            InitializeComponent();
            Title = $"UserID: {userId}";

            client = _client;
            _ = client.ListenAsync(OnMessageReceived);
        }

        public void OnMessageReceived(string msg)
        {
            Debug.WriteLine($"OnMessageReceived: {msg}");

            string myRoomID = "69a0a4f759fb6ad9c0945263"; //general

            // "MSG:User@Hello World!"
            if (!msg.StartsWith("MSG:")) return;

            // "User@Hello World!"
            string formated = msg.Substring(4); // Remove "MSG:" prefix

            // "[69a0a4f759fb6ad9c0945263] [Hello World!]"
            string[] content = formated.Split('@', 2);

            string user = content[0];
            string text = content[1];

            this.DispatcherQueue.TryEnqueue(() =>
            {
                MessageList.Items.Add(new Message
                {
                    Username = user,
                    Text = text,
                    SentAt = DateTime.UtcNow.ToString()
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

            string roomID = "69a0a4f759fb6ad9c0945263";

            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            client.SendAsync($"MSG:{roomID}@{text}@{DateTime.UtcNow.ToString("HH:mm")}");

            MessageInput.Text = "";
        }
    }
}

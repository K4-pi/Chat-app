using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Chat;
using Windows.ApplicationModel.Store.Preview.InstallControl;
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
        private TcpChatClient client;
        public ObservableCollection<ChatRoom> userRooms { get; set; } = new ObservableCollection<ChatRoom>();

        private string currentRoomId; //general

        public MainWindow(string userId, TcpChatClient _client)
        {
            Debug.WriteLine("DEBUG chat window");
            InitializeComponent();
            Title = $"UserID: {userId}";

            client = _client;
            _ = client.ListenAsync(OnMessageReceived);

            client.SendAsync("GET_ROOMS");
            RoomList.ItemsSource = userRooms;
        }

        public void OnMessageReceived(string msg)
        {
            Debug.WriteLine($"OnMessageReceived: {msg}");

            string[] splitedMessages = msg.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in splitedMessages)
            {
                // "MSG:User@Hello World!"
                if (s.StartsWith("MSG:"))
                {
                    // "User@Hello World!"
                    string formated = s.Substring(4); // Remove "MSG:" prefix

                    // "[User] [Hello World!]"
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
                else if (s.StartsWith("ROOM_LIST:"))
                {
                    string content = s.Substring(10);
                    if (content == "NONE") return;

                    var rooms = content.Split('|');

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        RoomList.ItemsSource = null; // Prevents refreshing every time new message is added

                        userRooms.Clear();
                        Debug.WriteLine("Your rooms:");

                        foreach (var r in rooms)
                        {
                            Debug.WriteLine(r);

                            var tokens = r.Split(',');
                            var newRoom = new ChatRoom { Id = tokens[0], Name = tokens[1] };
                            userRooms.Add(newRoom);
                        }

                        RoomList.ItemsSource = userRooms;
                    });
                }
            }            
        }

        private void RoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoomList.SelectedItem is ChatRoom selectedRoom)
            {
                currentRoomId = selectedRoom.Id;
                MessageList.Items.Clear();

                client.SendAsync($"GET_HISTORY:{currentRoomId}"); // Update messages
                Debug.WriteLine($"Switched to room: {selectedRoom.Name} ({currentRoomId})");
            }
        }

        public async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            /*
                We will not add message to our own list,
                Server will broadcast it back...
                It should do that
            */
            if (currentRoomId == null)
            {
                Debug.WriteLine("You need to choose room before you send message");
                return;
            }

            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            client.SendAsync($"MSG:{currentRoomId}@{text}");

            MessageInput.Text = "";
        }
    }
}

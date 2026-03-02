using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading; // Crucial for Dispatcher

namespace ChatApp
{
    public partial class MainWindow : Window
    {
        private TcpChatClient client;
        // Avalonia automatically detects changes in ObservableCollections
        public ObservableCollection<ChatRoom> UserRooms { get; set; } = new ObservableCollection<ChatRoom>();
        // Using a collection for messages is better than manual .Items.Add
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        private string currentRoomId;

        // Avalonia doesn't need 'sealed', just 'partial'
        public MainWindow(string userId, TcpChatClient _client)
        {
            InitializeComponent();
            this.Title = $"UserID: {userId}";

            client = _client;
            
            // Set up bindings
            RoomList.ItemsSource = UserRooms;
            MessageList.ItemsSource = Messages;

            // Start listening for server data
            _ = client.ListenAsync(OnMessageReceived);

            // Ask server for rooms
            client.SendAsync("GET_ROOMS");
        }

        public void OnMessageReceived(string msg)
        {
            Debug.WriteLine($"OnMessageReceived: {msg}");
            string[] splitedMessages = msg.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in splitedMessages)
            {
                if (s.StartsWith("MSG:"))
                {
                    string formated = s.Substring(4);
                    string[] content = formated.Split('@', 3);

                    // Switching to UI Thread for Avalonia
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Messages.Add(new Message
                        {
                            Username = content[0],
                            Text = content[1],
                            SentAt = content[2]
                        });

                        // Scroll to bottom
                        if (Messages.Count > 0)
                            MessageList.ScrollIntoView(Messages.Last());
                    });
                }
                else if (s.StartsWith("ROOM_LIST:"))
                {
                    string content = s.Substring(10);
                    if (content == "NONE") return;

                    var rooms = content.Split('|');

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        UserRooms.Clear();
                        foreach (var r in rooms)
                        {
                            var tokens = r.Split(',');
                            UserRooms.Add(new ChatRoom {
                                Id = tokens[0],
                                Name = tokens[1]
                            });
                        }
                    });
                }
            }
        }

        private void RoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoomList.SelectedItem is ChatRoom selectedRoom)
            {
                currentRoomId = selectedRoom.Id;
                Messages.Clear(); // Clear the collection, UI updates automatically

                client.SendAsync($"GET_HISTORY:{currentRoomId}");
                Debug.WriteLine($"Switched to room: {selectedRoom.Name} ({currentRoomId})");
            }
        }

        public void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = new AuthWindow();
            authWindow.Show(); // Use .Show() in Avalonia, not .Activate()
            this.Close();
        }

        public void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentRoomId == null)
            {
                Debug.WriteLine("Choose a room first!");
                return;
            }

            string text = MessageInput.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Send to server
            client.SendAsync($"MSG:{currentRoomId}@{text}@{DateTime.UtcNow}");

            MessageInput.Text = "";
        }
    }
}

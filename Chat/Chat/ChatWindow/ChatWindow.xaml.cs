using Chat.DataModels;
using Chat.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
        public ObservableCollection<User> UsersInRoom { get; } = new ObservableCollection<User>();

        private string? currentRoomId; //general

        public MainWindow(string userId, TcpChatClient _client)
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            Microsoft.UI.Windowing.AppWindow appWindow =
                Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                    Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd)
                );

            appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 600));

            InitializeComponent();
            Title = $"UserID: {userId}";

            client = _client;
            _ = client.ListenAsync(OnMessageReceived);
            _ = client.SendAsync("GET_ROOMS");

            RoomList.ItemsSource = userRooms;
        }

        public void OnMessageReceived(string msg)
        {
            Debug.WriteLine($"OnMessageReceived: {msg}");

            string[] splitedMessages = msg.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in splitedMessages)
            {
                // "MSG:User@Hello World!@time"
                if (s.StartsWith("MSG:"))
                {
                    // "User@Hello World!@time"
                    string formated = s.Substring(4); // Remove "MSG:" prefix

                    // "[User] [Hello World!] [time]"
                    string[] content = formated.Split('@', 3);

                    string user = content[0];
                    string text = content[1];
                    string time = content[2];

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        Messages.Add(new Message
                        {
                            Username = user,
                            Text = text,
                            SentAt = time
                        });

                        MessageList.UpdateLayout();
                        MessageScrollViewer.ChangeView(null, MessageScrollViewer.ScrollableHeight, null);
                    });
                }
                else if (s.StartsWith("ROOM_LIST:"))
                {
                    string content = s.Substring(10);
                    if (content == "NONE") return;

                    string[] rooms = content.Split('|');

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        RoomList.ItemsSource = null; // Prevents refreshing every time new room is added

                        userRooms.Clear();
                        Debug.WriteLine("Your rooms:");

                        foreach (var r in rooms)
                        {
                            Debug.WriteLine(r);

                            var tokens = r.Split(',', 2);
                            var newRoom = new ChatRoom { 
                                Id = tokens[0], 
                                Name = tokens[1] 
                            };
                            userRooms.Add(newRoom);
                        }

                        RoomList.ItemsSource = userRooms;
                    });
                }
                else if (s.StartsWith("USERS_LIST:"))
                {
                    string content = s.Substring(11);
                    if (content == "NONE") return;

                    string[] users = content.Split('|');

                    Debug.WriteLine("users list:");
                    foreach (var u in users)
                    {
                        Debug.WriteLine($"USER {u}");
                    }

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        UsersInRoom.Clear();

                        foreach (var u in users)
                        {
                            UsersInRoom.Add(new User
                            {
                                Username = u
                            });
                        }

                        UsersList.UpdateLayout();
                        UserScrollViewer.ChangeView(null, UserScrollViewer.ScrollableHeight, null);
                    });
                }
            }            
        }

        private async void RoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoomList.SelectedItem is ChatRoom selectedRoom)
            {
                currentRoomId = selectedRoom.Id;
                Messages.Clear();
                UsersInRoom.Clear();

                await client.SendAsync($"GET_HISTORY:{currentRoomId}"); // Update messages
                Debug.WriteLine($"Switched to room: {selectedRoom.Name} ({currentRoomId})");

                await Task.Delay(100);
                await client.SendAsync($"GET_USERS_LIST:{currentRoomId}"); // Update users
            }
        }

        public async void LogoutButton_Click(object sender, RoutedEventArgs e) // Not sure about that
        {
            await client.SendAsync("DISCONNECT");
            var authWindow = new AuthWindow();
            this.Close();
            authWindow.Activate();
        }

        private async Task SendMessage()
        {
            /*
                Not adding message to our own list because
                server will broadcast it back
            */
            if (currentRoomId == null)
            {
                Debug.WriteLine("You need to choose room before you send message");
                return;
            }

            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;


            try
            {
                await client.SendAsync($"MSG:{currentRoomId}@{text}@{DateTime.UtcNow.ToString()}");
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"SendMessageException:{ex.Message}");
            }

            MessageInput.Text = "";
        }

        public async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private void MessageInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = SendMessage();
                e.Handled = true;
            }
        }
    }
}

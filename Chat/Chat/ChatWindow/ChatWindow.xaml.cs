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
        private bool isShowed = false;

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

        public async void OnMessageReceived(string msg)
        {
            Debug.WriteLine($"OnMessageReceived: {msg}");

            string[] splitedMessages = msg.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in splitedMessages)
            {
                // "MSG:roomId@User@Hello World!@time"
                if (s.StartsWith("MSG:"))
                {
                    // "roomId@User@Hello World!@time"
                    string formated = s.Substring(4); // Remove "MSG:" prefix

                    // "[roomId] [User] [Hello World!] [time]"
                    string[] content = formated.Split('@', 4);

                    string roomId = content[0];
                    string user   = content[1];
                    string text   = content[2];
                    string time   = content[3];

                    Debug.WriteLine($"roomid: {roomId}");
                    Debug.WriteLine($"current room id: {currentRoomId}");

                    if (roomId != currentRoomId) return;

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

                    string[] rooms = content.Split('@');

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        RoomList.ItemsSource = null; // Prevents refreshing every time new room is added

                        userRooms.Clear();
                        Debug.WriteLine("Your rooms:");

                        foreach (var r in rooms)
                        {
                            Debug.WriteLine(r);

                            var tokens = r.Split(',', 3);
                            var newRoom = new ChatRoom { 
                                Id = tokens[0], 
                                Name = tokens[1], 
                                Code = tokens[2]
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

                    string[] users = content.Split('@');

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
                else if (s.StartsWith("CREATE_ROOM:"))
                {
                    string content = s.Substring(12);
                    if (content == "SUCCESS")
                    {
                        await client.SendAsync("GET_ROOMS");
                        await ShowAlert("SUCCESS", "Created room");
                    }
                    else if (content == "EXISTS")
                    {
                        await ShowAlert("ERROR", "Room with that name already exists");
                    }
                    else
                    {
                        await ShowAlert("ERROR", "Failed to create room");
                    }
                }
                else if (s.StartsWith("JOIN_ROOM:"))
                {
                    string content = s.Substring(10);
                    if (content == "SUCCESS")
                    {
                        await client.SendAsync("GET_ROOMS");
                        await ShowAlert("SUCCESS", "Joined room");
                    }
                    else if (content == "NO_ROOM")
                    {
                        await ShowAlert("ERROR", "Room doesn't exists");
                    }
                    else
                    {
                        await ShowAlert("ERROR", "Failed to join a room");
                    }
                }
                else if (s.StartsWith("DELETE_ROOM:"))
                {
                    string content = s.Substring(12);
                    if (content == "DELETED")
                    {
                        await client.SendAsync("GET_ROOMS");
                        await ShowAlert("SUCCESS", "Deleted room");
                    }
                    else if (content == "LEFT")
                    {
                        await client.SendAsync("GET_ROOMS");
                        await ShowAlert("SUCCESS", "Left room");
                    }
                    else
                    {
                        await ShowAlert("ERROR", "Room delete/leave problem");
                    }
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
                await ShowAlert("ERROR", "You need to choose room before you send message");
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

        private async void DeleteRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button?.DataContext is ChatRoom selectedRoom)
            {
                await client.SendAsync($"DELETE_ROOM:{selectedRoom.Name}");
            }
        }

        private async void JoinRoomButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox roomNameInput = new TextBox
            {
                Header = "Name",
                //PlaceholderText = "insert room name",
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBox roomCodeInput = new TextBox
            {
                Header = "Code"
                //PlaceholderText = "insert room code"
            };

            StackPanel panel = new StackPanel();
            panel.Children.Add(roomNameInput);
            panel.Children.Add(roomCodeInput);

            ContentDialog dialog = new ContentDialog
            {
                Title = "Join room",
                Content = panel,
                PrimaryButtonText = "Join",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string roomCode = roomCodeInput.Text;
                string roomName = roomNameInput.Text;

                if (!string.IsNullOrEmpty(roomCode) && !string.IsNullOrEmpty(roomName))
                {
                    await client.SendAsync($"JOIN_ROOM:{roomName}@{roomCode}");
                }
                else
                {
                    await ShowAlert("ERROR", "You need to provide name and code");
                }
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // NEW USERNAME
            TextBox newUsername = new TextBox
            {
                Header = "new username",
                Margin = new Thickness(0, 0, 10, 0),
                Description = "..."
            };

            Button usernameButton = new Button
            {
                Content = "Change", 
                VerticalAlignment = VerticalAlignment.Bottom 
            };

            Grid usernameGrid = new Grid();
            usernameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Fill space
            usernameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            usernameGrid.Margin = new Thickness(0, 0, 0, 10);

            Grid.SetColumn(newUsername, 0);
            Grid.SetColumn(usernameButton, 1);
            usernameGrid.Children.Add(newUsername);
            usernameGrid.Children.Add(usernameButton);

            TextBox newPassword = new TextBox
            {
                Header = "new password",
                Margin = new Thickness(0, 0, 10, 0),
                Description = "..."
            };

            Button passwordButton = new Button
            {
                Content = "Change", 
                VerticalAlignment = VerticalAlignment.Bottom
            };

            // NEW PASSWORD
            Grid passwordGrid = new Grid();
            passwordGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            passwordGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            passwordGrid.Margin = new Thickness(0, 0, 0, 10);

            Grid.SetColumn(newPassword, 0);
            Grid.SetColumn(passwordButton, 1);
            passwordGrid.Children.Add(newPassword);
            passwordGrid.Children.Add(passwordButton);

            StackPanel panel = new StackPanel();
            panel.Children.Add(usernameGrid);
            panel.Children.Add(passwordGrid);

            ContentDialog dialog = new ContentDialog
            {
                Title = "Update Account",
                Content = panel,
                PrimaryButtonText = null,
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            string regex = "!@#$%^&*()_-+=/?.,<>;:][}{\\|\'\"";

            usernameButton.Click += async (s, args) => {
                string usernameString = newUsername.Text;

                if (usernameString.Length < 4)
                {
                    newUsername.Description = "Username must be at least 4 characters";
                }
                else if (usernameString.Any(c => regex.Contains(c)))
                {
                    newUsername.Description = "Special characters are not allowed";
                }
                else
                {
                    _ = client.SendAsync($"CHANGE_USERNAME:{usernameString}");

                    newUsername.Description = "Username changed";
                    newUsername.Text = "";
                }
            };

            passwordButton.Click += (s, args) => {
                string newPasswordString = newPassword.Text;

                if (newPasswordString.Length < 4)
                {
                    newPassword.Description = "Passowrd must be at least 4 characters";
                }
                else if (newPasswordString.Any(c => regex.Contains(c)))
                {
                    newPassword.Description = "Special characters are not allowed";
                }
                else
                {
                    _ = client.SendAsync($"CHANGE_PASSWORD:{newPasswordString}");
                    
                    newPassword.Description = "Password changed";
                    newPassword.Text = "";
                }
            };

            ContentDialogResult result = await dialog.ShowAsync();
        }

        private async void CreateRoomButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox roomNameInput = new TextBox
            {
                Header = "Name",
                //PlaceholderText = "insert room name",
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBox roomCodeInput = new TextBox
            {
                Header = "Code"
                //PlaceholderText = "insert room code"
            };

            StackPanel panel = new StackPanel();
            panel.Children.Add(roomNameInput);
            panel.Children.Add(roomCodeInput);

            ContentDialog dialog = new ContentDialog
            {
                Title = "Crete room",
                Content = panel,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string roomName = roomNameInput.Text;
                string roomCode = roomCodeInput.Text;

                if (!string.IsNullOrEmpty(roomCode) && !string.IsNullOrEmpty(roomName))
                {
                    await client.SendAsync($"CREATE_ROOM:{roomName}@{roomCode}");
                }
                else
                {
                    await ShowAlert("ERROR", "You need to provide name and code");
                }
            }
        }

        private void MessageInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = SendMessage();
                e.Handled = true;
            }
        }

        private async Task ShowAlert(string title, string message)
        {
            if (isShowed) return;

            isShowed = true;
            ContentDialog errorDialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            await errorDialog.ShowAsync();
            isShowed = false;
        }
    }
}

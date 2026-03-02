using Avalonia;
using Avalonia.Controls;

namespace ChatApp
{
    public partial class AuthWindow : Window
    {
        // Static reference so we can close this window from the LoginPage
        public static AuthWindow Instance { get; private set; }

        public AuthWindow()
        {
            InitializeComponent();
            Instance = this;

            // Manual Navigation
            AuthContentArea.Content = new LoginPage();
        }

        public static void CloseRootWindow()
        {
            Instance?.Close();
        }
    }
}

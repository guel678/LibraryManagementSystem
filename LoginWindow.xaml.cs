using System.Windows;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagementSystem;

public partial class LoginWindow : Window
{
    private readonly DataStore store = App.Services.GetRequiredService<DataStore>();
    private readonly AuthService auth;

    public LoginWindow()
    {
        InitializeComponent();
        auth = new AuthService(store);
        ConfigureMode();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text.Trim();
        var password = PasswordBox.Password;

        if (!store.Data.Users.Any())
        {
            CreateInitialAdmin(username, password);
            return;
        }

        var user = auth.Login(username, password);

        if (user is null)
        {
            ErrorText.Text = "Invalid username or password.";
            return;
        }

        OpenMainWindow(user);
    }

    private void ConfigureMode()
    {
        if (store.Data.Users.Any())
        {
            ModeText.Text = "Sign in with your assigned account.";
            LoginButton.Content = "Login";
            return;
        }

        ModeText.Text = "Create the first administrator account to start using the system.";
        LoginButton.Content = "Create Admin";
    }

    private void UsernameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UsernamePlaceholder.Visibility = string.IsNullOrWhiteSpace(UsernameBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UsernameBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        UsernamePlaceholder.Visibility = Visibility.Collapsed;
    }

    private void UsernameBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        UsernamePlaceholder.Visibility = string.IsNullOrWhiteSpace(UsernameBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void PasswordBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        PasswordPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void PasswordBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void CreateInitialAdmin(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorText.Text = "Enter a username and password.";
            return;
        }

        var user = new Models.User
        {
            Id = store.NextUserId(),
            Username = username,
            PasswordHash = PasswordService.Hash(password),
            Role = Models.UserRole.Admin
        };

        store.Data.Users.Add(user);
        store.Save();

        OpenMainWindow(user);
    }

    private void OpenMainWindow(Models.User user)
    {
        try
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(user, store)
            };
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Could not open dashboard.";
            MessageBox.Show($"Could not open dashboard: {ex.Message}", "Login error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Diplom.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Diplom;

public partial class MainWindow : Window
{
    private User? _currentUser;

    public MainWindow()
    {
        InitializeComponent();
        UpdateUIForGuest();
    }

    public void UpdateUIForLoggedInUser(User user)
    {
        _currentUser = user;

        GuestContent.IsVisible = false;
        UserContent.IsVisible = true;

        BtnLogin.IsVisible = false;
        BtnRegister.IsVisible = false;

        BtnLogout.IsVisible = true;

        WelcomeText.Text = $"Добро пожаловать, {user.UserName}!";

        SetAvatar(user.ImagePath);
    }

    public void UpdateUIForGuest()
    {
        _currentUser = null;

        GuestContent.IsVisible = true;
        UserContent.IsVisible = false;

        BtnLogin.IsVisible = true;
        BtnRegister.IsVisible = true;

        BtnLogout.IsVisible = false;

        WelcomeText.Text = string.Empty;
        AvatarImage.Source = null;
    }

    private async void OpenLogin(object? sender, RoutedEventArgs e)
    {
        var loginWindow = new LoginPage(this);
        await loginWindow.ShowDialog(this);
    }

    private async void OpenRegistration(object? sender, RoutedEventArgs e)
    {
        var regWindow = new RegistrationPage(this);
        await regWindow.ShowDialog(this);
    }

    private void Logout(object? sender, RoutedEventArgs e)
    {
        UpdateUIForGuest();
    }

    private async void OpenLocalUpload(object? sender, RoutedEventArgs e)
    {
        if (_currentUser == null)
            return;

        var window = new LocalUploadWindow(_currentUser);
        await window.ShowDialog(this);
    }

    private async void OpenNetworkUpload(object? sender, RoutedEventArgs e)
    {
        if (_currentUser == null)
            return;

        var window = new NetworkUploadWindow(_currentUser);
        await window.ShowDialog(this);
    }
    private async void OpenActiveOrders(object? sender, RoutedEventArgs e)
    {
        if (_currentUser == null)
            return;

        var window = new ActiveOrdersWindow(_currentUser);
        await window.ShowDialog(this);
    }

    private async void OpenHistory(object? sender, RoutedEventArgs e)
    {
        if (_currentUser == null)
            return;

        var window = new HistoryWindow(_currentUser);
        await window.ShowDialog(this);
    }

    public User? GetCurrentUser() => _currentUser;

    private void SetAvatar(string? imagePath)
    {
        AvatarImage.Source = null;

        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        try
        {
            var fullPath = Path.IsPathRooted(imagePath)
                ? imagePath
                : Path.Combine(AppContext.BaseDirectory, imagePath);

            if (File.Exists(fullPath))
                AvatarImage.Source = new Bitmap(fullPath);
        }
        catch
        {
            AvatarImage.Source = null;
        }
    }
}
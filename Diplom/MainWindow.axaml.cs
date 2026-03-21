using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Diplom.Models;
using System;
using System.IO;

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

        BtnLocalUpload.IsEnabled = true;
        BtnNetworkUpload.IsEnabled = true;
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

        BtnLocalUpload.IsEnabled = false;
        BtnNetworkUpload.IsEnabled = false;
        BtnLogout.IsVisible = false;

        WelcomeText.Text = string.Empty;
        AvatarImage.Source = null;
    }

    private void OpenLogin(object? sender, RoutedEventArgs e)
    {
        new LoginPage(this).Show();
    }

    private void OpenRegistration(object? sender, RoutedEventArgs e)
    {
        new RegistrationPage(this).Show();
    }

    private void Logout(object? sender, RoutedEventArgs e)
    {
        UpdateUIForGuest();
    }

    private void OpenLocalUpload(object? sender, RoutedEventArgs e)
    {
        new LocalUploadWindow().Show();
    }

    private void OpenNetworkUpload(object? sender, RoutedEventArgs e)
    {
        new NetworkUploadWindow().Show();
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
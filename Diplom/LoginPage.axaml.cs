using Avalonia.Controls;
using Avalonia.Interactivity;
using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Diplom;

public partial class LoginPage : Window
{
    private readonly MainWindow _mainWindow;

    public LoginPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private async void Login(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = string.Empty;

        string login = LoginBox.Text?.Trim() ?? string.Empty;
        string password = PasswordBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            StatusText.Text = "Введите логин и пароль.";
            return;
        }

        try
        {
            await using var context = new DiplomContext();

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Login == login && u.Password == password);

            if (user is null)
            {
                StatusText.Text = "Неверный логин или пароль.";
                return;
            }

            _mainWindow.UpdateUIForLoggedInUser(user);
            Close();
        }
        catch
        {
            StatusText.Text = "Ошибка подключения к базе данных.";
        }
    }

    private void GoBack(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
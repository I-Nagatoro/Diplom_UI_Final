using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace Diplom;

public partial class RegistrationPage : Window
{
    private readonly MainWindow _mainWindow;
    private string? _selectedImagePath;

    public RegistrationPage(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
    }

    private async void SelectImage(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = string.Empty;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return;

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Выберите изображение профиля",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp" }
                }
            }
        });

        if (result.Count == 0)
            return;

        var file = result[0];
        var localPath = file.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(localPath))
        {
            StatusText.Text = "Не удалось получить путь к изображению.";
            return;
        }

        _selectedImagePath = localPath;

        try
        {
            await using var stream = await file.OpenReadAsync();
            PreviewImage.Source = new Bitmap(stream);
        }
        catch
        {
            StatusText.Text = "Ошибка при загрузке изображения.";
            PreviewImage.Source = null;
            _selectedImagePath = null;
        }
    }

    private async void Register(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = string.Empty;

        string username = UsernameBox.Text?.Trim() ?? string.Empty;
        string login = LoginBox.Text?.Trim() ?? string.Empty;
        string password = PasswordBox.Text ?? string.Empty;
        string confirmPassword = ConfirmPasswordBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(password))
        {
            StatusText.Text = "Заполните все обязательные поля.";
            return;
        }

        if (password != confirmPassword)
        {
            StatusText.Text = "Пароли не совпадают.";
            return;
        }

        if (password.Length < 6)
        {
            StatusText.Text = "Пароль должен содержать не менее 6 символов.";
            return;
        }

        string imagePathToSave = "images/picture.png";

        if (!string.IsNullOrWhiteSpace(_selectedImagePath))
        {
            try
            {
                var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
                Directory.CreateDirectory(imagesDir);

                var fileName = Path.GetFileName(_selectedImagePath);
                var destinationPath = Path.Combine(imagesDir, fileName);

                File.Copy(_selectedImagePath, destinationPath, true);
                imagePathToSave = Path.Combine("images", fileName).Replace('\\', '/');
            }
            catch
            {
                StatusText.Text = "Ошибка при сохранении изображения.";
                return;
            }
        }

        try
        {
            await using var context = new DiplomContext();
            var notasynccont = new DiplomContext();

            if (await context.Users.AnyAsync(u => u.Login == login))
            {
                StatusText.Text = "Пользователь с таким логином уже существует.";
                return;
            }

            if (await context.Users.AnyAsync(u => u.UserName == username))
            {
                StatusText.Text = "Пользователь с таким именем уже существует.";
                return;
            }
            var userId = notasynccont.Users.Max(x=>x.UserId);
            var newUser = new User
            {
                UserId = userId + 1,
                UserName = username,
                Login = login,
                Password = password,
                RoleId = 2,
                ImagePath = imagePathToSave
            };

            context.Users.Add(newUser);
            await context.SaveChangesAsync();

            _mainWindow.UpdateUIForLoggedInUser(newUser);
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
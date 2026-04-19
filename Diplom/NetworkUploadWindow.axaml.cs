using Avalonia.Controls;
using Avalonia.Interactivity;
using Diplom.Models;
using System.Threading.Tasks;

namespace Diplom;

public partial class NetworkUploadWindow : Window
{
    public NetworkUploadWindow()
    {
        InitializeComponent();
    }
    public NetworkUploadWindow(User user)
    {
        InitializeComponent();
    }

    private async void DownloadVideo(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UrlBox.Text))
        {
            StatusText.Text = "Введите ссылку.";
            return;
        }

        StatusText.Text = "Загрузка...";
        await Task.Delay(2000);
        StatusText.Text = "Видео загружено и отправлено на обработку.";
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;

namespace Diplom;

public partial class HistoryWindow : Window
{
    private readonly User _currentUser;

    // Храним адрес сервера (заполняется при открытии из MainWindow или LocalUploadWindow)
    public static string? ServerBaseUrl { get; set; }

    public HistoryWindow() => InitializeComponent();

    public HistoryWindow(User user)
    {
        InitializeComponent();
        _currentUser = user;
        LoadHistory();
    }

    private async void LoadHistory()
    {
        await using var db = new DiplomContext();
        var history = await db.Histories
            .Where(h => h.UserId == _currentUser.UserId)
            .OrderByDescending(h => h.DatetimeFinish)
            .ToListAsync();

        HistoryList.ItemsSource = history;
        EmptyText.IsVisible = !history.Any();
    }

    private void OpenVideo(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not History record) return;

        string? url = null;

        if (!string.IsNullOrEmpty(record.FileId) && !string.IsNullOrEmpty(ServerConfig.ServerBaseUrl))
        {
            url = $"{ServerConfig.ServerBaseUrl}/download/{record.FileId}";
        }
        else
        {
            url = record.VideoPath ?? record.VideoUri;
        }

        if (string.IsNullOrEmpty(url)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private void BackClick(object? sender, RoutedEventArgs e) => Close();
}
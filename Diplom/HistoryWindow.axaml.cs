using Avalonia.Controls;
using Avalonia.Interactivity;
using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;

namespace Diplom;

public partial class HistoryWindow : Window
{
    private readonly User _currentUser;

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

        string? path = record.VideoPath ?? record.VideoUri;
        if (string.IsNullOrEmpty(path)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void BackClick(object? sender, RoutedEventArgs e) => Close();
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Diplom;

public partial class HistoryWindow : Window
{
    private User _user;
    private List<History> _history;

    public HistoryWindow(User user)
    {
        InitializeComponent();
        _user = user;
        LoadHistory();
    }
    public HistoryWindow()
    {
        InitializeComponent();
    }

    private async void LoadHistory()
    {
        using var db = new DiplomContext();

        _history = await db.Histories
            .Where(h => h.UserId == _user.UserId)
            .ToListAsync();

        if (_history.Count == 0)
        {
            EmptyText.IsVisible = true;
            HistoryList.IsVisible = false;
        }
        else
        {
            HistoryList.ItemsSource = _history;
        }
    }

    private void OpenVideo(object? sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var record = btn?.Tag as History;

        string path = record?.VideoPath ?? record?.VideoUri;

        if (string.IsNullOrEmpty(path)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void BackClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
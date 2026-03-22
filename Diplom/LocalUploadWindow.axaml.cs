using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Diplom.Services;
using System;
using System.Diagnostics;

namespace Diplom;

public partial class LocalUploadWindow : Window
{
    private readonly DubbingService _dubbingService;
    private string? _resultPath;

    public LocalUploadWindow()
    {
        InitializeComponent();

        _dubbingService = new DubbingService();
        _dubbingService.ProgressChanged += UpdateProgress;
    }

    protected override void OnClosed(EventArgs e)
    {
        _dubbingService.ProgressChanged -= UpdateProgress;
        base.OnClosed(e);
    }

    private void UpdateProgress(int percent, string stage)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ProcessingBar.IsVisible = true;
            ProcessingBar.Value = percent;
            StatusText.Text = $"{stage} ({percent}%)";
        });
    }

    private async void SelectFile(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = string.Empty;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return;

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите видео",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video")
                {
                    Patterns = new[] { "*.mp4", "*.mkv", "*.avi", "*.mov" }
                }
            }
        });

        if (result.Count == 0)
            return;

        var localPath = result[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            StatusText.Text = "Не удалось получить путь к файлу.";
            return;
        }

        FilePathBox.Text = localPath;
    }

    private async void StartProcessing(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FilePathBox.Text))
        {
            StatusText.Text = "Выберите видео.";
            return;
        }

        StartButton.IsEnabled = false;
        OpenResultButton.IsVisible = false;
        SelectBtn.IsEnabled = false;
        BackBtn.IsEnabled = false;
        ProcessingBar.IsVisible = true;
        ProcessingBar.Value = 0;
        StatusText.Text = "Начата обработка...";

        try
        {
            var result = await _dubbingService.ProcessVideoAsync(FilePathBox.Text);

            if (!result.success)
            {
                StatusText.Text = $"Ошибка: {result.message}";
                return;
            }

            _resultPath = result.resultVideoPath;
            StatusText.Text = "Обработка завершена.";
            OpenResultButton.IsVisible = true;
            ProcessingBar.Value = 100;
        }
        catch
        {
            StatusText.Text = "Ошибка при обработке видео.";
        }
        finally
        {
            StartButton.IsEnabled = true;
            BackBtn.IsEnabled = true;
            SelectBtn.IsEnabled = true;
        }
    }

    private void OpenResult(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_resultPath))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = _resultPath,
            UseShellExecute = true
        });
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
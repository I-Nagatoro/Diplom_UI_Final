using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Diplom.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Diplom;

public partial class LocalUploadWindow : Window
{
    private readonly User _currentUser;
    private readonly HttpClient _httpClient;
    private string _serverBaseUrl = string.Empty;
    private string? _resultUrl;
    private int _orderId;
    private DispatcherTimer? _timer;

    // Публичное статическое свойство для доступа из других окон (например, HistoryWindow)

    public LocalUploadWindow() => InitializeComponent();

    public LocalUploadWindow(User user)
    {
        InitializeComponent();
        _currentUser = user;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };
        _ = TryAutoConnect();
    }

    // Конструктор для открытия активного заказа
    public LocalUploadWindow(Order order)
    {
        InitializeComponent();
        _currentUser = order.User ?? new User(); // на случай, если User не загружен
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };

        // Подхватываем данные заказа
        _orderId = order.OrderId;
        if (!string.IsNullOrEmpty(order.VideoPath))
            FilePathBox.Text = order.VideoPath;

        // Используем последний известный URL сервера
        _serverBaseUrl = ServerConfig.ServerBaseUrl ?? string.Empty;
        if (!string.IsNullOrEmpty(_serverBaseUrl))
        {
            ServerUrlBox.Text = _serverBaseUrl;
            ConnectionStatusText.Text = $"✅ Подключено к {_serverBaseUrl}";
            ConnectionStatusText.Foreground = Avalonia.Media.Brushes.Green;
        }
        else
        {
            ConnectionStatusText.Text = "Неизвестный сервер. Введите URL и нажмите Подключиться.";
            ConnectionStatusText.Foreground = Avalonia.Media.Brushes.Red;
            StartButton.IsEnabled = false;
            return;
        }

        // Если заказ ещё не завершён, сразу начинаем отслеживание прогресса
        if (order.Progress < 100 && !order.Completed)
        {
            StartTracking();
            // Блокируем кнопку отправки, т.к. дубляж уже идёт
            StartButton.IsEnabled = false;
            SelectBtn.IsEnabled = false;
            ProgressPanel.IsVisible = true;
        }
        else if (order.Completed && order.Status == "completed")
        {
            // Если дубляж завершён, показываем кнопку для скачивания
            StartButton.IsEnabled = false;
            SelectBtn.IsEnabled = false;
            OpenResultButton.IsVisible = true;
            _resultUrl = $"{_serverBaseUrl}/download/{order.FileId}";
            StatusText.Text = "✅ Дубляж завершён. Нажмите «Открыть итоговое видео».";
        }
    }

    private async Task TryAutoConnect()
    {
        var url = ServerUrlBox.Text?.Trim();
        if (!string.IsNullOrEmpty(url))
            await TestServerConnection(url);
    }

    private async void ConnectToServer(object? sender, RoutedEventArgs e)
    {
        var url = ServerUrlBox.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            ConnectionStatusText.Text = "Введите URL сервера";
            return;
        }
        await TestServerConnection(url.TrimEnd('/'));
    }

    private async Task TestServerConnection(string url)
    {
        try
        {
            ConnectionStatusText.Text = "Проверка соединения...";
            ConnectionStatusText.Foreground = Avalonia.Media.Brushes.Orange;

            var response = await _httpClient.GetAsync($"{url}/ping");
            if (response.IsSuccessStatusCode)
            {
                _serverBaseUrl = url;
                ServerConfig.ServerBaseUrl = url;   
                ServerUrlBox.Text = url;
                ConnectionStatusText.Text = $"✅ Подключено к {url}";
                ConnectionStatusText.Foreground = Avalonia.Media.Brushes.Green;
                StartButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusText.Text = "❌ Сервер не отвечает.";
                ConnectionStatusText.Foreground = Avalonia.Media.Brushes.Red;
                StartButton.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            ConnectionStatusText.Text = $"❌ Ошибка соединения: {ex.Message}";
            ConnectionStatusText.Foreground = Avalonia.Media.Brushes.Red;
            StartButton.IsEnabled = false;
        }
    }

    private async void SelectFile(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null) return;

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите видео",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video")
                {
                    Patterns = new[] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.webm" }
                }
            }
        });

        if (result.Count == 0) return;

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

        if (string.IsNullOrEmpty(_serverBaseUrl))
        {
            StatusText.Text = "Сначала подключитесь к серверу.";
            return;
        }

        StartButton.IsEnabled = false;
        SelectBtn.IsEnabled = false;
        StatusText.Text = "Отправка видео на сервер...";
        ProgressPanel.IsVisible = true;

        try
        {
            using var formData = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(FilePathBox.Text);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
            formData.Add(fileContent, "file", Path.GetFileName(FilePathBox.Text));
            formData.Add(new StringContent(FilePathBox.Text), "video_path");   // передаём путь для сохранения в БД

            var response = await _httpClient.PostAsync($"{_serverBaseUrl}/dub/", formData);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StartDubbingResponse>(json);
            if (result == null)
            {
                StatusText.Text = "❌ Неверный ответ сервера.";
                return;
            }

            _orderId = result.order_id;
            StatusText.Text = "Дубляж запущен, отслеживание...";
            StartTracking();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"❌ Ошибка отправки: {ex.Message}";
            StartButton.IsEnabled = true;
            SelectBtn.IsEnabled = true;
        }
    }

    private void StartTracking()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await CheckProgress();
        _timer.Start();
    }

    private async Task CheckProgress()
    {
        if (_orderId == 0) return;

        try
        {
            var response = await _httpClient.GetStringAsync($"{_serverBaseUrl}/order/{_orderId}");
            var status = JsonSerializer.Deserialize<OrderStatusResponse>(response);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProcessingBar.Value = status.progress;
                StatusText.Text = $"{status.stage} ({status.progress}%)";

                if (status.completed && status.status == "completed")
                {
                    _timer?.Stop();
                    StatusText.Text = "✅ Готово!";
                    OpenResultButton.IsVisible = true;
                    _resultUrl = $"{_serverBaseUrl}{status.result_url}";
                    StartButton.IsEnabled = true;
                    SelectBtn.IsEnabled = true;
                }
                else if (status.status == "error" || status.status == "failed")
                {
                    _timer?.Stop();
                    StatusText.Text = $"❌ Ошибка: {status.stage}";
                    StartButton.IsEnabled = true;
                    SelectBtn.IsEnabled = true;
                }
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText.Text = $"Ошибка связи: {ex.Message}";
            });
        }
    }

    private void OpenResult(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_resultUrl))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _resultUrl,
                UseShellExecute = true
            });
        }
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        _timer?.Stop();
        Close();
    }

    private class StartDubbingResponse
    {
        public int order_id { get; set; }
        public string status { get; set; } = string.Empty;
    }

    private class OrderStatusResponse
    {
        public int order_id { get; set; }
        public int progress { get; set; }
        public string stage { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public bool completed { get; set; }
        public string result_url { get; set; } = string.Empty;
        public string file_id { get; set; } = string.Empty;
    }
}
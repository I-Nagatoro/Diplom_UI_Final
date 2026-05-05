using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Diplom.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Diplom;

public partial class NetworkUploadWindow : Window
{
    private readonly User _currentUser;
    private readonly HttpClient _httpClient;
    private string _serverBaseUrl = string.Empty;
    private string? _resultUrl;
    private int _orderId;
    private DispatcherTimer? _timer;

    public NetworkUploadWindow() => InitializeComponent();

    public NetworkUploadWindow(User user)
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

    private async void DownloadVideo(object? sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            StatusText.Text = "Введите ссылку на видео.";
            return;
        }

        if (string.IsNullOrEmpty(_serverBaseUrl))
        {
            StatusText.Text = "Сначала подключитесь к серверу.";
            return;
        }

        StartButton.IsEnabled = false;
        ProcessingBar.IsVisible = true;
        StatusText.Text = "Отправка ссылки на сервер...";

        try
        {
            // Отправляем только url, сервер создаст заказ и вернёт order_id
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("url", url)
            });

            var response = await _httpClient.PostAsync($"{_serverBaseUrl}/dub/url", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StartDubbingResponse>(json);
            if (result == null)
            {
                StatusText.Text = "❌ Неверный ответ сервера.";
                StartButton.IsEnabled = true;
                ProcessingBar.IsVisible = false;
                return;
            }

            _orderId = result.order_id;
            StatusText.Text = "Дубляж запущен, отслеживание...";
            StartTracking();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"❌ Ошибка: {ex.Message}";
            StartButton.IsEnabled = true;
            ProcessingBar.IsVisible = false;
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
                    _resultUrl = $"{_serverBaseUrl}{status.result_url}";
                    StartButton.IsEnabled = true;
                }
                else if (status.status == "failed" || status.status == "error")
                {
                    _timer?.Stop();
                    StatusText.Text = $"❌ Ошибка: {status.stage}";
                    StartButton.IsEnabled = true;
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
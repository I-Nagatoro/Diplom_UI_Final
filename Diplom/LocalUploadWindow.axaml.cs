using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Diplom;

public partial class LocalUploadWindow : Window
{
    User _currentUser;
    private string? _resultPath;
    private DispatcherTimer _timer;
    private int _orderId;
    private string _serverBaseUrl = string.Empty;
    private string _currentTaskId = string.Empty;
    private string _currentFileId = string.Empty;
    private static HttpClient _httpClient;

    public LocalUploadWindow()
    {
        InitializeComponent();
        InitHttpClient();
    }

    public LocalUploadWindow(User user)
    {
        InitializeComponent();
        InitHttpClient();
        _currentUser = user;
        _ = TryAutoConnect();
    }

    public LocalUploadWindow(Order existingOrder)
    {
        InitializeComponent();
        InitHttpClient();
        _orderId = existingOrder.OrderId;
        FilePathBox.Text = existingOrder.VideoPath;
    }

    private void InitHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            AllowAutoRedirect = true
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(10) // 10 минут на весь запрос
        };
    }

    private async Task TryAutoConnect()
    {
        var url = ServerUrlBox.Text?.Trim();
        if (!string.IsNullOrEmpty(url))
        {
            await TestServerConnection(url);
        }
    }

    private async void ConnectToServer(object? sender, RoutedEventArgs e)
    {
        var url = ServerUrlBox.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            ConnectionStatusText.Text = "Введите URL сервера";
            return;
        }

        url = url.TrimEnd('/');
        await TestServerConnection(url);
    }

    private async Task TestServerConnection(string url)
    {
        try
        {
            ConnectionStatusText.Text = "Проверка соединения...";
            ConnectionStatusText.Foreground = Avalonia.Media.Brushes.Orange;

            string[] urlsToTry = { url };
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                urlsToTry = new[] { url, url.Replace("http://", "https://") };
            }

            HttpResponseMessage? response = null;
            string? successUrl = null;

            foreach (var tryUrl in urlsToTry)
            {
                try
                {
                    response = await _httpClient.GetAsync($"{tryUrl}/docs");
                    if (response.IsSuccessStatusCode)
                    {
                        successUrl = tryUrl;
                        break;
                    }
                }
                catch
                {
                    // Продолжаем
                }
            }

            if (successUrl != null)
            {
                _serverBaseUrl = successUrl;
                ServerUrlBox.Text = successUrl;
                ConnectionStatusText.Text = $"✅ Подключено к {successUrl}";
                ConnectionStatusText.Foreground = Avalonia.Media.Brushes.Green;
                StartButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusText.Text = "❌ Сервер не отвечает. Проверьте URL и запущен ли сервер.";
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

        try
        {
            _currentFileId = Guid.NewGuid().ToString();

            using var db = new DiplomContext();
            var order = new Order
            {
                VideoPath = FilePathBox.Text,
                DatetimeOrder = DateTime.Now,
                UserId = _currentUser.UserId,
                Progress = 0,
                Stage = "Отправка"
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            _orderId = order.OrderId;

            // Проверка размера файла (для отладки)
            var fileInfo = new FileInfo(FilePathBox.Text);
            Debug.WriteLine($"[CLIENT] Sending file: {fileInfo.Name}, Size: {fileInfo.Length} bytes");

            using var formData = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(FilePathBox.Text);
            fileStream.Position = 0;
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");

            // Временно отключаем ProgressableStreamContent
            formData.Add(fileContent, "file", Path.GetFileName(FilePathBox.Text));

            StatusText.Text = "Отправка видео на сервер... (без прогресса)";

            var response = await _httpClient.PostAsync($"{_serverBaseUrl}/dub/", formData);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StartDubbingResponse>(responseJson);
            _currentTaskId = result!.task_id;

            order.TaskId = _currentTaskId;
            order.FileId = _currentFileId;
            order.Stage = "В очереди";
            await db.SaveChangesAsync();

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
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += async (_, _) => await CheckProgress();
        _timer.Start();
    }

    private async Task CheckProgress()
    {
        if (string.IsNullOrEmpty(_currentTaskId))
            return;

        try
        {
            var response = await _httpClient.GetStringAsync($"{_serverBaseUrl}/task/{_currentTaskId}");
            var status = JsonSerializer.Deserialize<TaskStatusResponse>(response);

            Dispatcher.UIThread.Post(() =>
            {
                ProcessingBar.IsVisible = true;
                ProcessingBar.Value = status.progress;
                StatusText.Text = $"{status.status} ({status.progress}%)";

                UpdateOrderProgress(status.progress, status.status);

                if (status.state == "SUCCESS")
                {
                    _timer.Stop();
                    StatusText.Text = "✅ Готово!";
                    OpenResultButton.IsVisible = true;
                    _resultPath = $"{_serverBaseUrl}/download/{_currentFileId}";
                    StartButton.IsEnabled = true;
                    SelectBtn.IsEnabled = true;
                }
                else if (status.state == "FAILURE")
                {
                    _timer.Stop();
                    StatusText.Text = $"❌ Ошибка: {status.error}";
                    StartButton.IsEnabled = true;
                    SelectBtn.IsEnabled = true;
                }
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusText.Text = $"Ошибка связи: {ex.Message}";
            });
        }
    }

    private async void UpdateOrderProgress(int progress, string stage)
    {
        using var db = new DiplomContext();
        var order = await db.Orders.FindAsync(_orderId);
        if (order != null)
        {
            order.Progress = progress;
            order.Stage = stage;
            await db.SaveChangesAsync();
        }
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
        _timer?.Stop();
        Close();
    }

    private class StartDubbingResponse
    {
        public string task_id { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
    }

    private class TaskStatusResponse
    {
        public string state { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public int progress { get; set; }
        public string error { get; set; } = string.Empty;
    }

    public class ProgressableStreamContent : HttpContent
    {
        private readonly HttpContent _content;
        private readonly Action<long, long> _progress;
        private const int BufferSize = 8192;

        public ProgressableStreamContent(HttpContent content, Action<long, long> progress)
        {
            _content = content;
            _progress = progress;
            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var buffer = new byte[BufferSize];
            var contentStream = await _content.ReadAsStreamAsync();
            var totalBytes = contentStream.Length;
            var uploadedBytes = 0L;

            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
                uploadedBytes += bytesRead;
                _progress?.Invoke(uploadedBytes, totalBytes);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Headers.ContentLength ?? -1;
            return length != -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _content.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
using System.Net.Http;
using System.Threading.Tasks;

namespace Diplom.Services;

/// <summary>
/// Заготовка для будущего перехода на серверную архитектуру.
/// Вместо локального Python будет HTTP API.
/// </summary>
public class ServerDubbingService
{
    private readonly HttpClient _httpClient;

    public ServerDubbingService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> UploadVideoAsync(string filePath)
    {
        // TODO:
        // 1. Отправить видео через multipart/form-data
        // 2. Получить jobId
        // 3. Дождаться готовности
        // 4. Скачать итоговое видео

        await Task.Delay(1000);

        return "SERVER_STUB";
    }
}
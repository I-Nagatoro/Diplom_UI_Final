using System;
using System.IO;
using System.Threading.Tasks;

namespace Diplom.Services;

public class DubbingService
{
    private readonly PythonProcessService _pythonService;

    public event Action<int, string>? ProgressChanged;

    public DubbingService()
    {
        _pythonService = new PythonProcessService();

        _pythonService.ProgressChanged += (p, s) =>
        {
            ProgressChanged?.Invoke(p, s);
        };
    }

    public async Task<(bool success, string message, string? resultVideoPath)>
        ProcessVideoAsync(string sourceVideoPath)
    {
        try
        {
            string projectPath = _pythonService.GetProjectPath();

            string inputDir = Path.Combine(projectPath, "input");
            string outputDir = Path.Combine(projectPath, "output");

            Directory.CreateDirectory(inputDir);
            Directory.CreateDirectory(outputDir);

            string targetVideoPath = Path.Combine(inputDir, "video.mp4");

            // 1️⃣ Копируем видео
            File.Copy(sourceVideoPath, targetVideoPath, true);

            // 2️⃣ Запускаем Python
            var result = await _pythonService.RunMainScriptAsync();

            if (!result.success)
                return (false, result.output, null);

            // 3️⃣ Путь к итоговому видео
            string finalVideoPath =
                Path.Combine(outputDir, "final_dubbed_video.mp4");

            // 4️⃣ Ждём появления файла (до 5 минут)
            bool fileReady = await WaitForFileAsync(finalVideoPath, TimeSpan.FromMinutes(5));

            if (!fileReady)
                return (false, "Итоговое видео не появилось в течение допустимого времени.", null);

            return (true, "Готово", finalVideoPath);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    /// <summary>
    /// Ожидает появления файла и его разблокировки.
    /// </summary>
    private async Task<bool> WaitForFileAsync(string path, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            if (File.Exists(path))
            {
                try
                {
                    // Проверяем, что файл не используется другим процессом
                    using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true;
                    }
                }
                catch
                {
                    // файл ещё занят ffmpeg — ждём
                }
            }

            await Task.Delay(1000);
        }

        return false;
    }
}
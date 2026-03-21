using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Diplom.Services;

/// <summary>
/// Запуск Python процесса с потоковым чтением stdout.
/// Передаёт прогресс через событие.
/// </summary>
public class PythonProcessService
{
    private readonly string _projectPath =
        @"C:\Users\sokol\PycharmProjects\QwenTTS_Dubbing";

    private readonly string _pythonExe = @"C:\Users\sokol\miniconda3\envs\qwen_tts_env\python.exe";

    public event Action<int, string>? ProgressChanged;

    public async Task<(bool success, string output)> RunMainScriptAsync()
    {
        var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = _pythonExe,
            Arguments = "main.py",
            WorkingDirectory = _projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.OutputDataReceived += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            ParseProgress(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();

        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode == 0, error);
    }

    private void ParseProgress(string line)
    {
        if (!line.StartsWith("PROGRESS::"))
            return;

        try
        {
            var parts = line.Split("::");
            int percent = int.Parse(parts[1]);
            string stage = parts[2];

            ProgressChanged?.Invoke(percent, stage);
        }
        catch
        {
            // игнорируем некорректные строки
        }
    }

    public string GetProjectPath() => _projectPath;
}
using System;
using System.IO;
using System.Text;

namespace ConferencePlayer.Core;

/// <summary>
/// Extremely small, dependency-free file logger.
/// </summary>
public sealed class AppLogger
{
    private readonly object _gate = new();
    private readonly string _logFilePath;

    public AppLogger(string logsFolderPath)
    {
        Directory.CreateDirectory(logsFolderPath);

        var fileName = $"conferenceplayer-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log";
        _logFilePath = Path.Combine(logsFolderPath, fileName);

        Info($"Log started. UTC={DateTime.UtcNow:o}");
    }

    public string LogFilePath => _logFilePath;

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception ex) => Write("ERROR", $"{message}{Environment.NewLine}{ex}");

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            var line = $"[{DateTime.UtcNow:o}] {level}: {message}{Environment.NewLine}";
            File.AppendAllText(_logFilePath, line, Encoding.UTF8);
        }
    }
}

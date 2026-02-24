using System;
using System.IO;
using System.Text;
using System.Linq;

namespace ConferencePlayer.Core;

/// <summary>
/// Extremely small, dependency-free file logger with basic security sanitization.
/// </summary>
public sealed class AppLogger
{
    private static readonly string? _cachedUser = GetSafeEnvironmentVariable(() => Environment.UserName);
    private static readonly string? _cachedMachine = GetSafeEnvironmentVariable(() => Environment.MachineName);

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

    public void DeleteOldLogs(int daysToKeep)
    {
        try
        {
            var folder = Path.GetDirectoryName(_logFilePath);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

            var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
            var files = Directory.EnumerateFiles(folder, "conferenceplayer-*.log");

            foreach (var file in files)
            {
                try
                {
                    var fi = new FileInfo(file);
                    // Check creation time or last write time
                    if (fi.CreationTimeUtc < cutoff)
                    {
                        fi.Delete();
                    }
                }
                catch
                {
                    // Swallow individual file deletion errors (e.g. locked file)
                }
            }
        }
        catch (Exception ex)
        {
            Error("Failed to clean up old logs", ex);
        }
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? ex)
    {
        var sb = new StringBuilder();
        sb.Append(message);

        if (ex != null)
        {
            sb.Append(Environment.NewLine);
#if DEBUG
            sb.Append(ex.ToString());
#else
            // Production: log only Type and Message to avoid excessive detail/stacktrace exposure (CWE-532)
            sb.Append($"{ex.GetType().Name}: {ex.Message}");
            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.Append($"{Environment.NewLine}Inner: {inner.GetType().Name}: {inner.Message}");
                inner = inner.InnerException;
            }
#endif
        }

        Write("ERROR", sb.ToString());
    }

    private void Write(string level, string message)
    {
        var sanitized = Sanitize(message);
        lock (_gate)
        {
            var line = $"[{DateTime.UtcNow:o}] {level}: {sanitized}{Environment.NewLine}";
            File.AppendAllText(_logFilePath, line, Encoding.UTF8);
        }
    }

    private static string? GetSafeEnvironmentVariable(Func<string> getter)
    {
        try
        {
            var value = getter();
            return (!string.IsNullOrWhiteSpace(value) && value.Length > 1) ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var result = input;
        try
        {
            // Mask the current user's name in paths to avoid PII exposure
            if (_cachedUser != null)
            {
                result = result.Replace(_cachedUser, "[USER]", StringComparison.OrdinalIgnoreCase);
            }

            // Mask machine name as it's often considered environment-sensitive info
            if (_cachedMachine != null)
            {
                result = result.Replace(_cachedMachine, "[MACHINE]", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Defensive: if anything fails, return original input to ensure we still log something.
        }

        return result;
    }
}

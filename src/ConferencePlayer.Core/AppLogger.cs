using System;
using System.IO;
using System.Text;

namespace ConferencePlayer.Core;

/// <summary>
/// Extremely small, dependency-free file logger with basic security sanitization.
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

    private string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var result = input;
        try
        {
            // Mask the current user's name in paths to avoid PII exposure
            var user = Environment.UserName;
            if (!string.IsNullOrWhiteSpace(user) && user.Length > 1)
            {
                result = result.Replace(user, "[USER]", StringComparison.OrdinalIgnoreCase);
            }

            // Mask machine name as it's often considered environment-sensitive info
            var machine = Environment.MachineName;
            if (!string.IsNullOrWhiteSpace(machine) && machine.Length > 1)
            {
                result = result.Replace(machine, "[MACHINE]", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Defensive: if environment access fails, return original input to ensure we still log something.
        }

        return result;
    }
}

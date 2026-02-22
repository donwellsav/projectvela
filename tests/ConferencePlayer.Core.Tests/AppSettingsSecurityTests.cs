using System.IO;
using Xunit;

namespace ConferencePlayer.Core.Tests;

public class AppSettingsSecurityTests
{
    [Fact]
    public void Sanitize_RemovesDangerousExtensions()
    {
        // Arrange
        var settings = new AppSettings();
        settings.AllowedExtensions.Add(".exe");
        settings.AllowedExtensions.Add(".bat");
        settings.AllowedExtensions.Add(".dll");
        settings.AllowedExtensions.Add(".mp4"); // Should remain

        // Act
        settings.Sanitize();

        // Assert
        Assert.DoesNotContain(".exe", settings.AllowedExtensions);
        Assert.DoesNotContain(".bat", settings.AllowedExtensions);
        Assert.DoesNotContain(".dll", settings.AllowedExtensions);
        Assert.Contains(".mp4", settings.AllowedExtensions);
    }

    [Fact]
    public void Sanitize_FixesInvalidLogsPath()
    {
        // Skip if no invalid path chars defined on this platform (e.g. Linux often allows almost anything)
        var invalidChars = Path.GetInvalidPathChars();
        if (invalidChars.Length == 0)
        {
            return;
        }

        // Arrange
        var settings = new AppSettings();
        // Use a character that is invalid on the current platform
        settings.LogsFolderPath = "Invalid" + invalidChars[0] + "Path";

        // Act
        settings.Sanitize();

        // Assert
        // Expecting it to fall back to default logs folder
        var defaultLogs = PathHelpers.GetDefaultLogsFolder();
        Assert.Equal(defaultLogs, settings.LogsFolderPath);
    }
}

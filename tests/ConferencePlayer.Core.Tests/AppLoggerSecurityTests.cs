using System;
using System.IO;
using System.Linq;
using ConferencePlayer.Core;
using Xunit;

namespace ConferencePlayer.Core.Tests;

public class AppLoggerSecurityTests
{
    [Fact]
    public void Logger_MasksUserNameAndMachineName()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "VelaTests_" + Guid.NewGuid().ToString());
        var logger = new AppLogger(tempPath);
        var userName = Environment.UserName;
        var machineName = Environment.MachineName;

        // Act
        logger.Info($"User {userName} on {machineName} started the app.");

        // Assert
        var logFile = Directory.GetFiles(tempPath, "*.log").First();
        var content = File.ReadAllText(logFile);

        // We expect the fix to replace these with [USER] and [MACHINE]
        if (!string.IsNullOrEmpty(userName) && userName.Length > 1)
        {
            Assert.DoesNotContain(userName, content);
            Assert.Contains("[USER]", content);
        }

        if (!string.IsNullOrEmpty(machineName) && machineName.Length > 1)
        {
            Assert.DoesNotContain(machineName, content);
            Assert.Contains("[MACHINE]", content);
        }

        // Clean up
        try { Directory.Delete(tempPath, true); } catch { }
    }

    [Fact]
    public void Error_HandlesNullException()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "VelaTests_" + Guid.NewGuid().ToString());
        var logger = new AppLogger(tempPath);

        // Act - should not throw even if null is passed
        logger.Error("Something failed", null!);

        // Assert
        var logFile = Directory.GetFiles(tempPath, "*.log").First();
        var content = File.ReadAllText(logFile);
        Assert.Contains("ERROR: Something failed", content);

        // Clean up
        try { Directory.Delete(tempPath, true); } catch { }
    }
}

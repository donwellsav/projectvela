using System.Collections.Generic;
using ConferencePlayer.Core;
using Xunit;

namespace ConferencePlayer.Core.Tests;

public class MediaFileFilterTests
{
    [Fact]
    public void IsAllowed_ShouldReturnTrue_ForValidExtensions()
    {
        var settings = new AppSettings();
        // Default allowed extensions include .mp4, .mp3, etc.
        var filter = new MediaFileFilter(settings);

        Assert.True(filter.IsAllowed("video.mp4"));
        Assert.True(filter.IsAllowed("VIDEO.MP4")); // Case insensitive
        Assert.True(filter.IsAllowed("audio.mp3"));
        Assert.True(filter.IsAllowed("c:\\path\\to\\file.mkv"));
    }

    [Fact]
    public void IsAllowed_ShouldReturnFalse_ForInvalidExtensions()
    {
        var settings = new AppSettings();
        var filter = new MediaFileFilter(settings);

        Assert.False(filter.IsAllowed("document.txt"));
        Assert.False(filter.IsAllowed("script.exe"));
        Assert.False(filter.IsAllowed("image.jpg")); // Assuming jpg is not in default allowed list (it's not)
        Assert.False(filter.IsAllowed(""));
        Assert.False(filter.IsAllowed("   "));
    }

    [Fact]
    public void IsAllowed_ShouldRespectCustomExtensions()
    {
        var settings = new AppSettings();
        settings.AllowedExtensions = new List<string> { ".custom", ".test" };
        var filter = new MediaFileFilter(settings);

        Assert.True(filter.IsAllowed("file.custom"));
        Assert.True(filter.IsAllowed("file.test"));
        Assert.False(filter.IsAllowed("file.mp4")); // Default no longer allowed if overwritten
    }

    [Fact]
    public void IsAllowed_ShouldReturnFalse_ForNullOrWhitespace()
    {
        var settings = new AppSettings();
        var filter = new MediaFileFilter(settings);

        Assert.False(filter.IsAllowed(null!));
        Assert.False(filter.IsAllowed(""));
        Assert.False(filter.IsAllowed("   "));
    }

    [Fact]
    public void IsAllowed_ShouldReturnFalse_ForFilesWithoutExtension()
    {
        var settings = new AppSettings();
        var filter = new MediaFileFilter(settings);

        Assert.False(filter.IsAllowed("README"));
        Assert.False(filter.IsAllowed("Makefile"));
        Assert.False(filter.IsAllowed("LICENSE"));
        Assert.False(filter.IsAllowed("path/to/file"));
    }

    [Fact]
    public void IsAllowed_ShouldReturnFalse_ForDotFiles()
    {
        var settings = new AppSettings();
        var filter = new MediaFileFilter(settings);

        Assert.False(filter.IsAllowed(".gitignore"));
        Assert.False(filter.IsAllowed(".env"));
        Assert.False(filter.IsAllowed(".config"));
        // Unless specifically added to allowed extensions, these should fail
    }

    [Fact]
    public void IsAllowed_ShouldHandleTrailingDotsAndSpaces()
    {
        var settings = new AppSettings();
        var filter = new MediaFileFilter(settings);

        // "file.mp4." usually results in an empty extension or just "." depending on OS/Path implementation
        // On Windows/Linux generally: Path.GetExtension("file.mp4.") -> "." or empty string.
        // We expect false because "." is not in allowed extensions.
        Assert.False(filter.IsAllowed("video.mp4."));

        // "file.mp4 " usually results in ".mp4 " which is not ".mp4"
        Assert.False(filter.IsAllowed("video.mp4 "));
    }

    [Fact]
    public void IsAllowed_ShouldHandleDirectoryDots()
    {
        var settings = new AppSettings();
        var filter = new MediaFileFilter(settings);

        // The directory has dots, but the file has a valid extension
        Assert.True(filter.IsAllowed("folder.with.dots/video.mp4"));

        // The directory has dots, but the file has no extension
        Assert.False(filter.IsAllowed("folder.with.dots/file"));
    }

    [Fact]
    public void IsAllowed_ShouldHandleComplexExtensions()
    {
        var settings = new AppSettings();
        // Add a complex extension to test if we can handle multi-dot scenarios if needed
        // Note: Path.GetExtension only returns the last segment (e.g., .gz for .tar.gz)
        // So unless we add .gz, .tar.gz won't work.

        settings.AllowedExtensions = new List<string> { ".tar.gz", ".gz" };
        var filter = new MediaFileFilter(settings);

        // Path.GetExtension("archive.tar.gz") returns ".gz"
        Assert.True(filter.IsAllowed("archive.tar.gz"));

        // If we only allowed .tar.gz but not .gz, it would fail with standard Path.GetExtension
        settings.AllowedExtensions = new List<string> { ".tar.gz" };
        filter = new MediaFileFilter(settings);

        // This is expected behavior of Path.GetExtension: it returns .gz
        // So checking for .tar.gz will fail unless we implement custom logic.
        // The test verifies this limitation/behavior.
        Assert.False(filter.IsAllowed("archive.tar.gz"));
    }
}

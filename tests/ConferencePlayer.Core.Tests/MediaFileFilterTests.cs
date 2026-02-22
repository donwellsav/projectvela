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
}

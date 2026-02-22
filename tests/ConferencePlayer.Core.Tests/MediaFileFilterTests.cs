using ConferencePlayer.Core;
using Xunit;

namespace ConferencePlayer.Core.Tests;

public class MediaFileFilterTests
{
    [Fact]
    public void Filter_Allows_Known_Extension_CaseInsensitive()
    {
        var settings = new AppSettings
        {
            FilterEnabled = true,
            AllowedExtensions = new() { ".mp4" }
        };

        var filter = new MediaFileFilter(settings);

        Assert.True(filter.IsAllowed(@"C:\media\test.MP4"));
        Assert.True(filter.IsAllowed(@"C:\media\test.mp4"));
        Assert.False(filter.IsAllowed(@"C:\media\test.mkv"));
    }

    [Fact]
    public void Filter_Disabled_Allows_Anything_With_Extension()
    {
        var settings = new AppSettings
        {
            FilterEnabled = false,
            AllowedExtensions = new()
        };

        var filter = new MediaFileFilter(settings);

        Assert.True(filter.IsAllowed(@"C:\media\anything.mkv"));
        Assert.False(filter.IsAllowed(@"")); // still rejects empty path
    }
}

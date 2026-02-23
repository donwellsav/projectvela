using Xunit;

namespace ConferencePlayer.Core.Tests;

public class AppSettingsTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        Assert.False(settings.WatchFolderEnabled);
        Assert.Empty(settings.WatchedFolderPath);
        Assert.True(settings.IncludeSubfolders);
        Assert.True(settings.AutoAdvancePlaylist);
        Assert.False(settings.LoopPlaylist);
        Assert.True(settings.PersistPlaylist);
        Assert.True(settings.EnablePreviewWindow);
        Assert.False(settings.PreviewCuesSelectedItem);
        Assert.False(settings.PreviewAudioEnabled);
        Assert.True(settings.PanicMutesAudio);
        Assert.True(settings.RestoreAudioAfterPanic);
        Assert.False(settings.ResumePlaybackAfterPanic);
        Assert.Equal("Space", settings.HotKey_PlayPause);
        Assert.Equal("S", settings.HotKey_Stop);
        Assert.Equal("PageDown", settings.HotKey_PlayNext);
        Assert.Equal("PageUp", settings.HotKey_PlayPrev);
        Assert.Equal("F12", settings.HotKey_Panic);
        Assert.Contains(".mp4", settings.AllowedExtensions);
        Assert.Contains(".mp3", settings.AllowedExtensions);
    }
}

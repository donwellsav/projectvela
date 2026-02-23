using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ConferencePlayer.Core.Tests;

public class PlaylistStoreBenchmark : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempFile;
    private readonly string _logsRoot;
    private readonly AppLogger _logger;

    public PlaylistStoreBenchmark(ITestOutputHelper output)
    {
        _output = output;
        _tempFile = Path.Combine(Path.GetTempPath(), $"playlist_{Guid.NewGuid()}.json");
        _logsRoot = Path.Combine(Path.GetTempPath(), $"logs_{Guid.NewGuid()}");
        _logger = new AppLogger(_logsRoot);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
        if (Directory.Exists(_logsRoot)) Directory.Delete(_logsRoot, true);
    }

    [Fact]
    public async Task Benchmark_SaveAsync_Performance()
    {
        // Generate a large playlist
        var state = new PlaylistState
        {
            Items = Enumerable.Range(0, 50000)
                .Select(i => Path.Combine("C:\\Content\\Videos", $"video_{i:00000}.mp4"))
                .ToList(),
            SelectedIndex = 1234,
            PositionSeconds = 45.67
        };

        var store = new PlaylistStore(_tempFile);

        // Warmup
        await store.SaveAsync(state, _logger);

        // Measure
        var sw = Stopwatch.StartNew();
        await store.SaveAsync(state, _logger);
        sw.Stop();

        _output.WriteLine($"SaveAsync Time (50k items): {sw.ElapsedMilliseconds} ms");

        // Verify file exists and size
        var info = new FileInfo(_tempFile);
        _output.WriteLine($"File Size: {info.Length / 1024.0 / 1024.0:F2} MB");
        Assert.True(info.Length > 0);
    }

    [Fact]
    public void Benchmark_Save_Performance()
    {
        // Generate a large playlist
        var state = new PlaylistState
        {
            Items = Enumerable.Range(0, 50000)
                .Select(i => Path.Combine("C:\\Content\\Videos", $"video_{i:00000}.mp4"))
                .ToList(),
            SelectedIndex = 1234,
            PositionSeconds = 45.67
        };

        var store = new PlaylistStore(_tempFile);

        // Warmup
        store.Save(state, _logger);

        // Measure
        var sw = Stopwatch.StartNew();
        store.Save(state, _logger);
        sw.Stop();

        _output.WriteLine($"Save (Sync) Time (50k items): {sw.ElapsedMilliseconds} ms");

        // Verify file exists and size
        var info = new FileInfo(_tempFile);
        Assert.True(info.Length > 0);
    }
}

using System;
using System.Collections.Generic;
using ConferencePlayer.Core;
using Xunit;
using Xunit.Abstractions;

namespace ConferencePlayer.Core.Tests;

public class MediaFileFilterBenchmark
{
    private readonly ITestOutputHelper _output;

    public MediaFileFilterBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_IsAllowed_Allocation()
    {
        // Arrange
        var settings = new AppSettings();
        var filter = new MediaFileFilter(settings);
        string[] testFiles = {
            "video.mp4",
            "audio.mp3",
            "image.jpg",
            "document.txt",
            "folder/subfolder/test.mkv"
        };

        // Warmup JIT
        for (int i = 0; i < 100; i++)
        {
            foreach (var file in testFiles)
            {
                filter.IsAllowed(file);
            }
        }

        // Measure
        long initialBytes = GC.GetAllocatedBytesForCurrentThread();
        const int iterations = 100_000;

        for (int i = 0; i < iterations; i++)
        {
            foreach (var file in testFiles)
            {
                filter.IsAllowed(file);
            }
        }

        long totalBytes = GC.GetAllocatedBytesForCurrentThread() - initialBytes;

        _output.WriteLine($"Allocated bytes for {iterations * testFiles.Length} calls: {totalBytes:N0} bytes");

        // Assert that we have zero allocations (allowing slight buffer for unexpected runtime overhead, but < 1KB is safe).
        // Before optimization: ~16,000,000 bytes.
        // After optimization: 0 bytes.
        Assert.True(totalBytes < 1024, $"Allocations should be near zero, but were {totalBytes} bytes.");
    }
}

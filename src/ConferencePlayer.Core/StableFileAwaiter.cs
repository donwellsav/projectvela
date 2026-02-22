using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConferencePlayer.Core;

public static class StableFileAwaiter
{
    /// <summary>
    /// Wait until a file stops changing size (and last write time) for a given stability window.
    /// This reduces the chance we add a file while it is still being copied into the watched folder.
    /// </summary>
    public static async Task<bool> WaitForStableAsync(
        string filePath,
        TimeSpan stableFor,
        TimeSpan timeout,
        AppLogger logger,
        CancellationToken ct)
    {
        var start = DateTime.UtcNow;

        long lastSize = -1;
        DateTime lastWrite = DateTime.MinValue;
        DateTime stableSince = DateTime.MinValue;

        while (DateTime.UtcNow - start < timeout)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (!File.Exists(filePath))
                {
                    stableSince = DateTime.MinValue;
                }
                else
                {
                    var fi = new FileInfo(filePath);
                    var size = fi.Length;
                    var write = fi.LastWriteTimeUtc;

                    if (size == lastSize && write == lastWrite)
                    {
                        if (stableSince == DateTime.MinValue)
                            stableSince = DateTime.UtcNow;

                        if (DateTime.UtcNow - stableSince >= stableFor)
                            return true;
                    }
                    else
                    {
                        lastSize = size;
                        lastWrite = write;
                        stableSince = DateTime.MinValue;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"StableFileAwaiter: transient error reading '{filePath}': {ex.Message}");
                stableSince = DateTime.MinValue;
            }

            await Task.Delay(250, ct);
        }

        logger.Warn($"StableFileAwaiter: timed out waiting for stable file: '{filePath}'");
        return false;
    }
}

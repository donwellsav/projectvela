using System;
using System.Collections.Generic;
using System.IO;

namespace ConferencePlayer.Core;

public sealed class MediaFileFilter
{
    private readonly AppSettings _settings;
    private readonly HashSet<string> _allowed;

    public MediaFileFilter(AppSettings settings)
    {
        _settings = settings;

        _allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ext in settings.AllowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(ext))
                continue;

            var normalized = ext.StartsWith(".") ? ext : "." + ext;
            _allowed.Add(normalized);
        }
    }

    public bool IsAllowed(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        // Even if filtering is disabled, we still require a non-empty path.
        if (!_settings.FilterEnabled)
            return true;

        var ext = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(ext))
            return false;

        return _allowed.Contains(ext);
    }
}

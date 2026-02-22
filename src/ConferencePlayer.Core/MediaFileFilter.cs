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

        // ⚡ Bolt: Use Span-based check to avoid allocating strings for every file extension.
        // This significantly reduces GC pressure when scanning large folders.
        ReadOnlySpan<char> pathSpan = filePath.AsSpan();
        ReadOnlySpan<char> extSpan = Path.GetExtension(pathSpan);

        if (extSpan.IsEmpty || extSpan.IsWhiteSpace())
            return false;

        // Use AlternateLookup to check existence without allocating a string.
        return _allowed.GetAlternateLookup<ReadOnlySpan<char>>().Contains(extSpan);
    }
}

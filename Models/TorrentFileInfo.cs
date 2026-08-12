namespace TorrentApp.Models;

/// <summary>
/// Represents a single file within a torrent.
/// </summary>
public sealed class TorrentFileInfo
{
    /// <summary>Relative path of the file within the torrent.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Total size of the file in bytes.</summary>
    public long Length { get; init; }

    /// <summary>Download progress of this file in [0.0, 1.0].</summary>
    public double Progress { get; set; }
}

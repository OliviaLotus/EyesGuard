namespace EyesGuard.Core.Models;

public enum UpdateCheckStatus
{
    Available,
    UpToDate,
    NotConfigured,
    InvalidManifest,
    Failed
}

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    Version CurrentVersion,
    Version? AvailableVersion,
    string Message,
    string? DownloadUrl = null,
    string? Sha256 = null,
    string? ReleaseNotes = null);

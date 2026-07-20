using EyesGuard.Core.Models;

namespace EyesGuard.Core.Abstractions;

public interface IUpdateService
{
    Version CurrentVersion { get; }
    Task<UpdateCheckResult> CheckAsync(string? manifestUrl, CancellationToken cancellationToken = default);
    Task<UpdateCheckResult> DownloadAndLaunchAsync(UpdateCheckResult update, CancellationToken cancellationToken = default);
}

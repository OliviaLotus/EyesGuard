using EyesGuard.Core.Models;

namespace EyesGuard.Core.Abstractions;

public interface ISyncTransport
{
    bool IsConfigured { get; }

    Task<SyncDocument?> PullAsync(CancellationToken cancellationToken = default);
    Task PushAsync(SyncDocument document, CancellationToken cancellationToken = default);
}

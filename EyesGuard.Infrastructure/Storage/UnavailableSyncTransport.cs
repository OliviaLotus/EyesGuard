using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;

namespace EyesGuard.Infrastructure.Storage;

public sealed class UnavailableSyncTransport : ISyncTransport
{
    public bool IsConfigured => false;

    public Task<SyncDocument?> PullAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<SyncDocument?>(null);

    public Task PushAsync(SyncDocument document, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

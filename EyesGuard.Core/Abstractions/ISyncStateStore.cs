using EyesGuard.Core.Models;

namespace EyesGuard.Core.Abstractions;

public interface ISyncStateStore
{
    Task<SyncState> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SyncState state, CancellationToken cancellationToken = default);
}

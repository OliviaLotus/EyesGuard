using EyesGuard.Core.Models;

namespace EyesGuard.Core.Abstractions;

public interface ISettingsStore
{
    Task<BreakSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(BreakSettings settings, CancellationToken cancellationToken = default);
}

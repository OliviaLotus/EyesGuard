using EyesGuard.Core.Models;

namespace EyesGuard.Core.Abstractions;

public interface IActivityRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task AddUsageAsync(DateOnly day, TimeSpan activeDelta, TimeSpan sessionElapsed, bool isNight, CancellationToken cancellationToken = default);
    Task AddBreakAsync(BreakRecord record, CancellationToken cancellationToken = default);
    Task<UsageSummary> GetSummaryAsync(DateOnly day, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsageSummary>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

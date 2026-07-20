namespace EyesGuard.Core.Models;

public enum BreakOutcome
{
    Completed,
    Skipped,
    Snoozed
}

public sealed record BreakRecord(
    DateTimeOffset OccurredAt,
    BreakOutcome Outcome,
    int PlannedSeconds,
    int CompletedSeconds);

namespace EyesGuard.Core.Models;

public sealed record UsageSummary(
    DateOnly Day,
    TimeSpan ActiveTime,
    TimeSpan LongestSession,
    TimeSpan NightTime,
    int BreaksCompleted,
    int BreaksSkipped,
    int BreaksSnoozed)
{
    public static UsageSummary Empty(DateOnly day) =>
        new(day, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, 0, 0, 0);

    public int CompletionRate
    {
        get
        {
            var total = BreaksCompleted + BreaksSkipped;
            return total == 0 ? 100 : (int)Math.Round(BreaksCompleted * 100d / total);
        }
    }
}

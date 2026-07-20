namespace EyesGuard.Core.Models;

public sealed record EyeGuardSnapshot(
    BreakPhase Phase,
    TimeSpan SessionElapsed,
    TimeSpan NextBreakIn,
    TimeSpan RestRemaining,
    TimeSpan ActiveDelta,
    double WorkProgress,
    bool IsEnabled,
    DateTimeOffset CapturedAt);

using EyesGuard.Core.Models;

namespace EyesGuard.Core.Services;

public static class ScenarioModeSelector
{
    public static EyeGuardScene? SelectScene(IEnumerable<EyeGuardScene> scenes, Func<IEnumerable<string>, bool> isAnyRunning)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(isAnyRunning);

        return scenes.Select(scene => scene.Normalize())
            .FirstOrDefault(scene => ParseProcessNames(scene.ProcessNames).Count > 0
                && isAnyRunning(ParseProcessNames(scene.ProcessNames)));
    }

    public static ScenarioMode? Select(BreakSettings settings, Func<IEnumerable<string>, bool> isAnyRunning)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(isAnyRunning);

        // More protective profiles take precedence when several programs are open.
        if (isAnyRunning(ParseProcessNames(settings.ChildProcessNames))) return ScenarioMode.Child;
        if (isAnyRunning(ParseProcessNames(settings.MovieProcessNames))) return ScenarioMode.Movie;
        if (isAnyRunning(ParseProcessNames(settings.ReadingProcessNames))) return ScenarioMode.Reading;
        if (isAnyRunning(ParseProcessNames(settings.OfficeProcessNames))) return ScenarioMode.Office;
        return null;
    }

    public static IReadOnlyList<string> ParseProcessNames(string? value) =>
        (value ?? string.Empty)
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => Path.GetFileNameWithoutExtension(name) ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}

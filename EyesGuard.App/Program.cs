using Avalonia;
using System;

namespace EyesGuard.App;

sealed class Program
{
    internal static SingleInstanceGuard? InstanceGuard { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using var guard = new SingleInstanceGuard();
        if (!guard.IsPrimary)
        {
            return;
        }

        InstanceGuard = guard;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        InstanceGuard = null;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}

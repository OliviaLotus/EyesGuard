namespace EyesGuard.Core.Abstractions;

public interface IStartupService
{
    bool IsSupported { get; }
    bool IsEnabled { get; }
    bool TrySetEnabled(bool enabled, out string status);
}

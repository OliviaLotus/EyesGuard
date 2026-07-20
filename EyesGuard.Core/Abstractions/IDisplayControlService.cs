using EyesGuard.Core.Models;

namespace EyesGuard.Core.Abstractions;

public interface IDisplayControlService
{
    DisplayControlCapabilities GetCapabilities();

    bool TryGetBrightness(out int brightnessPercent);

    bool TrySetBrightness(int brightnessPercent, out string status);
}

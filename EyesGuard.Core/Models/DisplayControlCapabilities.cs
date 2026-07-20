namespace EyesGuard.Core.Models;

public sealed record DisplayControlCapabilities(
    bool IsSupported,
    bool CanReadBrightness,
    bool CanWriteBrightness,
    int DisplayCount,
    string Status);

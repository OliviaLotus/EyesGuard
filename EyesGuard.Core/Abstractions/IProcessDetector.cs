namespace EyesGuard.Core.Abstractions;

public interface IProcessDetector
{
    bool IsAnyRunning(IEnumerable<string> processNames);
}

namespace EyesGuard.Core.Abstractions;

public interface IIdleTimeProvider
{
    TimeSpan GetIdleTime();
}

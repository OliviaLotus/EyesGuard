using System.Threading;

namespace EyesGuard.App;

internal sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = "Global\\EyesGuard.SingleInstance.1";
    private const string SignalName = "Global\\EyesGuard.Activate.1";
    private readonly Mutex _mutex;
    private readonly EventWaitHandle? _signal;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task? _listener;
    private readonly bool _ownsMutex;

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(true, MutexName, out var isPrimary);
        _ownsMutex = isPrimary;
        IsPrimary = isPrimary || !OperatingSystem.IsWindows();
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (isPrimary)
        {
            try
            {
                _signal = new EventWaitHandle(false, EventResetMode.AutoReset, SignalName);
                _listener = Task.Run(ListenAsync);
            }
            catch (UnauthorizedAccessException)
            {
                _signal = null;
            }
        }
        else
        {
            try
            {
                using var signal = EventWaitHandle.OpenExisting(SignalName);
                signal.Set();
            }
            catch (WaitHandleCannotBeOpenedException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public bool IsPrimary { get; }
    public event EventHandler? SecondInstanceRequested;

    private async Task ListenAsync()
    {
        if (_signal is null)
        {
            return;
        }

        while (!_shutdown.IsCancellationRequested)
        {
            try
            {
                await Task.Run(() => _signal.WaitOne(1000), _shutdown.Token);
                if (!_shutdown.IsCancellationRequested)
                {
                    SecondInstanceRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        _signal?.Dispose();
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
        _shutdown.Dispose();
    }
}

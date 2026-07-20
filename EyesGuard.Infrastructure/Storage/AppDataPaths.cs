namespace EyesGuard.Infrastructure.Storage;

public sealed class AppDataPaths
{
    public AppDataPaths(string? root = null)
    {
        Root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EyesGuard");
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }
    public string DatabasePath => Path.Combine(Root, "eyesguard.db");
    public string SyncStatePath => Path.Combine(Root, "sync-state.json");
}

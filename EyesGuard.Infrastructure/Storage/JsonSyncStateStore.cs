using System.Text.Json;
using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;

namespace EyesGuard.Infrastructure.Storage;

public sealed class JsonSyncStateStore(AppDataPaths paths) : ISyncStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<SyncState> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(paths.SyncStatePath))
        {
            return new SyncState();
        }

        try
        {
            await using var stream = File.OpenRead(paths.SyncStatePath);
            return (await JsonSerializer.DeserializeAsync<SyncState>(stream, JsonOptions, cancellationToken)
                    ?? new SyncState()).Normalize();
        }
        catch (JsonException)
        {
            return new SyncState();
        }
    }

    public async Task SaveAsync(SyncState state, CancellationToken cancellationToken = default)
    {
        var normalized = state.Normalize();
        var temporaryPath = $"{paths.SyncStatePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions, cancellationToken);
            }

            File.Move(temporaryPath, paths.SyncStatePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}

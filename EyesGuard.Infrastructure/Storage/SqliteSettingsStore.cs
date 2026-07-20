using System.Text.Json;
using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;
using Microsoft.Data.Sqlite;

namespace EyesGuard.Infrastructure.Storage;

public sealed class SqliteSettingsStore(AppDataPaths paths) : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = paths.DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate
    }.ToString();

    public async Task<BreakSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Json FROM AppSettings WHERE Id = 1;";
        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        return string.IsNullOrWhiteSpace(json)
            ? new BreakSettings()
            : (JsonSerializer.Deserialize<BreakSettings>(json, JsonOptions) ?? new BreakSettings()).Normalize();
    }

    public async Task SaveAsync(BreakSettings settings, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AppSettings (Id, Json) VALUES (1, $json)
            ON CONFLICT(Id) DO UPDATE SET Json = excluded.Json;
            """;
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(settings.Normalize(), JsonOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

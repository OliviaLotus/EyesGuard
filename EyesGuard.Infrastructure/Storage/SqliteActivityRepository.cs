using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;
using Microsoft.Data.Sqlite;

namespace EyesGuard.Infrastructure.Storage;

public sealed class SqliteActivityRepository(AppDataPaths paths) : IActivityRepository
{
    private string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = paths.DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared
    }.ToString();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS AppSettings (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS DailyUsage (
                Day TEXT PRIMARY KEY,
                ActiveSeconds INTEGER NOT NULL DEFAULT 0,
                LongestSessionSeconds INTEGER NOT NULL DEFAULT 0,
                NightSeconds INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS BreakRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OccurredAt TEXT NOT NULL,
                Day TEXT NOT NULL,
                Outcome INTEGER NOT NULL,
                PlannedSeconds INTEGER NOT NULL,
                CompletedSeconds INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_BreakRecords_Day ON BreakRecords (Day);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddUsageAsync(
        DateOnly day,
        TimeSpan activeDelta,
        TimeSpan sessionElapsed,
        bool isNight,
        CancellationToken cancellationToken = default)
    {
        var seconds = Math.Max(0, (long)Math.Round(activeDelta.TotalSeconds));
        if (seconds == 0)
        {
            return;
        }

        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DailyUsage (Day, ActiveSeconds, LongestSessionSeconds, NightSeconds)
            VALUES ($day, $active, $session, $night)
            ON CONFLICT(Day) DO UPDATE SET
                ActiveSeconds = ActiveSeconds + excluded.ActiveSeconds,
                LongestSessionSeconds = MAX(LongestSessionSeconds, excluded.LongestSessionSeconds),
                NightSeconds = NightSeconds + excluded.NightSeconds;
            """;
        command.Parameters.AddWithValue("$day", day.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$active", seconds);
        command.Parameters.AddWithValue("$session", Math.Max(0, (long)sessionElapsed.TotalSeconds));
        command.Parameters.AddWithValue("$night", isNight ? seconds : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddBreakAsync(BreakRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO BreakRecords (OccurredAt, Day, Outcome, PlannedSeconds, CompletedSeconds)
            VALUES ($occurredAt, $day, $outcome, $planned, $completed);
            """;
        command.Parameters.AddWithValue("$occurredAt", record.OccurredAt.ToString("O"));
        command.Parameters.AddWithValue("$day", DateOnly.FromDateTime(record.OccurredAt.LocalDateTime).ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$outcome", (int)record.Outcome);
        command.Parameters.AddWithValue("$planned", record.PlannedSeconds);
        command.Parameters.AddWithValue("$completed", record.CompletedSeconds);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<UsageSummary> GetSummaryAsync(DateOnly day, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COALESCE(u.ActiveSeconds, 0),
                COALESCE(u.LongestSessionSeconds, 0),
                COALESCE(u.NightSeconds, 0),
                COALESCE(SUM(CASE WHEN b.Outcome = 0 THEN 1 ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN b.Outcome = 1 THEN 1 ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN b.Outcome = 2 THEN 1 ELSE 0 END), 0)
            FROM (SELECT 1) seed
            LEFT JOIN DailyUsage u ON u.Day = $day
            LEFT JOIN BreakRecords b ON b.Day = $day
            GROUP BY u.ActiveSeconds, u.LongestSessionSeconds, u.NightSeconds;
            """;
        command.Parameters.AddWithValue("$day", day.ToString("yyyy-MM-dd"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return UsageSummary.Empty(day);
        }

        return new UsageSummary(
            day,
            TimeSpan.FromSeconds(reader.GetInt64(0)),
            TimeSpan.FromSeconds(reader.GetInt64(1)),
            TimeSpan.FromSeconds(reader.GetInt64(2)),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5));
    }

    public async Task<IReadOnlyList<UsageSummary>> GetRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            (from, to) = (to, from);
        }

        var summaries = new Dictionary<DateOnly, UsageSummary>();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH Days AS (
                SELECT Day FROM DailyUsage WHERE Day BETWEEN $from AND $to
                UNION
                SELECT Day FROM BreakRecords WHERE Day BETWEEN $from AND $to
            ),
            BreakStats AS (
                SELECT Day,
                    SUM(CASE WHEN Outcome = 0 THEN 1 ELSE 0 END) AS Completed,
                    SUM(CASE WHEN Outcome = 1 THEN 1 ELSE 0 END) AS Skipped,
                    SUM(CASE WHEN Outcome = 2 THEN 1 ELSE 0 END) AS Snoozed
                FROM BreakRecords
                WHERE Day BETWEEN $from AND $to
                GROUP BY Day
            )
            SELECT d.Day,
                COALESCE(u.ActiveSeconds, 0),
                COALESCE(u.LongestSessionSeconds, 0),
                COALESCE(u.NightSeconds, 0),
                COALESCE(b.Completed, 0),
                COALESCE(b.Skipped, 0),
                COALESCE(b.Snoozed, 0)
            FROM Days d
            LEFT JOIN DailyUsage u ON u.Day = d.Day
            LEFT JOIN BreakStats b ON b.Day = d.Day
            ORDER BY d.Day;
            """;
        command.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var day = DateOnly.ParseExact(reader.GetString(0), "yyyy-MM-dd");
            summaries[day] = new UsageSummary(
                day,
                TimeSpan.FromSeconds(reader.GetInt64(1)),
                TimeSpan.FromSeconds(reader.GetInt64(2)),
                TimeSpan.FromSeconds(reader.GetInt64(3)),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetInt32(6));
        }

        var result = new List<UsageSummary>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            result.Add(summaries.GetValueOrDefault(day) ?? UsageSummary.Empty(day));
        }

        return result;
    }
}

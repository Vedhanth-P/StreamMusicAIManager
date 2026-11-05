using Dapper;
using Microsoft.Data.Sqlite;
using StreamMusicAIManager.Models;
using System.IO;

namespace StreamMusicAIManager.Services;

public sealed class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string? filePath = null)
    {
        var path = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StreamMusicAIManager", "library.db");
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
    }

    public async Task<IReadOnlyList<AudioTrack>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection);
        var rows = await connection.QueryAsync<AudioTrackRow>(
            new CommandDefinition("SELECT * FROM AudioTracks ORDER BY ImportedAt DESC", cancellationToken: cancellationToken));
        return rows.Select(ToModel).ToList();
    }

    public async Task SaveAsync(IEnumerable<AudioTrack> tracks, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection);
        using var transaction = connection.BeginTransaction();
        foreach (var track in tracks)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO AudioTracks
                (Id, FilePath, FileName, FileHashSHA256, AudioFingerprint, Title, Artist, Album, Genre,
                 Bpm, DurationSeconds, Year, EnergyScore, MoodScore, Category, CopyrightStatus, CopyrightReason, ImportedAt)
                VALUES (@Id, @FilePath, @FileName, @FileHashSHA256, @AudioFingerprint, @Title, @Artist, @Album, @Genre,
                 @Bpm, @DurationSeconds, @Year, @EnergyScore, @MoodScore, @Category, @CopyrightStatus, @CopyrightReason, @ImportedAt)
                ON CONFLICT(Id) DO UPDATE SET FilePath=@FilePath, Category=@Category,
                 CopyrightStatus=@CopyrightStatus, CopyrightReason=@CopyrightReason
                """, track, transaction, cancellationToken: cancellationToken));
        }
        transaction.Commit();
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection) =>
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS AudioTracks (
                Id TEXT PRIMARY KEY, FilePath TEXT NOT NULL, FileName TEXT NOT NULL,
                FileHashSHA256 TEXT, AudioFingerprint TEXT, Title TEXT, Artist TEXT, Album TEXT,
                Genre TEXT, Bpm REAL, DurationSeconds REAL, Year INTEGER, EnergyScore REAL,
                MoodScore TEXT, Category TEXT, CopyrightStatus TEXT, CopyrightReason TEXT, ImportedAt TEXT
            )
            """);

    private static AudioTrack ToModel(AudioTrackRow row) => new()
    {
        Id = row.Id, FilePath = row.FilePath, FileName = row.FileName, FileHashSHA256 = row.FileHashSHA256 ?? "",
        AudioFingerprint = row.AudioFingerprint ?? "", Title = row.Title ?? "Unknown Title",
        Artist = row.Artist ?? "Unknown Artist", Album = row.Album ?? "Unknown Album",
        Genre = row.Genre ?? "Unknown Genre", Bpm = row.Bpm, DurationSeconds = row.DurationSeconds,
        Year = row.Year, EnergyScore = row.EnergyScore, MoodScore = row.MoodScore ?? "Neutral",
        Category = Enum.TryParse<MusicCategory>(row.Category, out var category) ? category : MusicCategory.Review_Required,
        CopyrightStatus = Enum.TryParse<CopyrightStatus>(row.CopyrightStatus, out var status) ? status : CopyrightStatus.REVIEW_REQUIRED,
        CopyrightReason = row.CopyrightReason ?? "", ImportedAt = DateTime.TryParse(row.ImportedAt, out var imported) ? imported : DateTime.UtcNow
    };

    private sealed class AudioTrackRow
    {
        public string Id { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string? FileHashSHA256 { get; set; }
        public string? AudioFingerprint { get; set; }
        public string? Title { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? Genre { get; set; }
        public double Bpm { get; set; }
        public double DurationSeconds { get; set; }
        public int Year { get; set; }
        public double EnergyScore { get; set; }
        public string? MoodScore { get; set; }
        public string? Category { get; set; }
        public string? CopyrightStatus { get; set; }
        public string? CopyrightReason { get; set; }
        public string? ImportedAt { get; set; }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using StreamMusicAIManager.Core;
using StreamMusicAIManager.Models;

namespace StreamMusicAIManager.Services
{
    public class ImportPipeline
    {
        private readonly AudioAnalyzer _analyzer = new();
        private readonly CopyrightScanner _copyrightScanner;
        private readonly DuplicateDetector _duplicateDetector = new();
        private readonly LibraryOrganizer _organizer;
        private readonly DatabaseService _database;
        private readonly string _libraryRootDir;
        public IReadOnlyList<AudioTrack> LastImportedTracks { get; private set; } = Array.Empty<AudioTrack>();

        public ImportPipeline(string configDir, string libraryRootDir)
        {
            _copyrightScanner = new CopyrightScanner(Path.Combine(configDir, "CopyrightDatabase.xml"));
            _organizer = new LibraryOrganizer(libraryRootDir);
            _database = new DatabaseService(Path.Combine(libraryRootDir, "library.db"));
            _libraryRootDir = libraryRootDir;
        }

        public async Task ExecuteImportAsync(
            List<string> inputPaths, 
            IProgress<ProcessingProgress> progressReporter, 
            Action<string> logCallback, 
            CancellationToken ct)
        {
            string tempDirectory = Path.Combine(_libraryRootDir, ".staging", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                logCallback("Discovering audio files...");
                var discoveredFiles = new List<string>();

                foreach (var path in inputPaths)
                {
                    if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        logCallback($"Extracting ZIP archive: {Path.GetFileName(path)}");
                        ZipFile.ExtractToDirectory(path, tempDirectory);
                    }
                    else if (Directory.Exists(path))
                    {
                        discoveredFiles.AddRange(GetAudioFiles(path));
                    }
                    else if (File.Exists(path) && IsSupportedAudio(path))
                    {
                        discoveredFiles.Add(path);
                    }
                }

                if (Directory.Exists(tempDirectory))
                {
                    discoveredFiles.AddRange(GetAudioFiles(tempDirectory));
                }

                discoveredFiles = discoveredFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                int totalCount = discoveredFiles.Count;
                logCallback($"Found {totalCount} total tracks for batch processing.");
                if (totalCount == 0)
                {
                    logCallback("No supported audio files were found.");
                    return;
                }

                // Concurrency Pipeline using System.Threading.Channels
                var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(1000)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleWriter = true
                });

                int processedCount = 0;
                var processedTracks = new ConcurrentBag<AudioTrack>();

                // Worker Tasks
                int workerCount = Math.Max(2, Environment.ProcessorCount);
                var workers = Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
                {
                    while (await channel.Reader.WaitToReadAsync(ct))
                    {
                        while (channel.Reader.TryRead(out var filePath))
                        {
                            if (ct.IsCancellationRequested) break;

                            // 1. Audio & Metadata Analysis
                            var track = _analyzer.AnalyzeFile(filePath);

                            // 2. Duplicate Detection
                            if (_duplicateDetector.IsDuplicate(track, out string dupReason))
                            {
                                track.Category = MusicCategory.Duplicates;
                                track.CopyrightReason = dupReason;
                            }
                            else
                            {
                                // 3. Copyright Inspection
                                var audit = _copyrightScanner.AuditTrack(track);
                                track.CopyrightStatus = audit.Status;
                                track.CopyrightReason = audit.Reason;

                                // 4. AI Categorization Matrix
                                if (track.CopyrightStatus == CopyrightStatus.UNSAFE)
                                {
                                    track.Category = MusicCategory.Unsafe_Copyright;
                                }
                                else if (track.CopyrightStatus == CopyrightStatus.REVIEW_REQUIRED)
                                {
                                    track.Category = MusicCategory.Review_Required;
                                }
                                else
                                {
                                    track.Category = ClassifyTrack(track);
                                }
                            }

                            // 5. Physical File Organization
                            _organizer.SortTrack(track);

                            processedTracks.Add(track);
                            int current = Interlocked.Increment(ref processedCount);

                            progressReporter?.Report(new ProcessingProgress
                            {
                                ProcessedItems = current,
                                TotalItems = totalCount,
                                CurrentFile = track.FileName,
                                Percentage = (double)current / totalCount * 100
                            });

                            logCallback($"[{current}/{totalCount}] Processed: {track.FileName} -> {track.Category}");
                        }
                    }
                })).ToArray();

                // Producer Task
                foreach (var file in discoveredFiles)
                {
                    await channel.Writer.WriteAsync(file, ct);
                }
                channel.Writer.Complete();

                await Task.WhenAll(workers);

                // Playlist & Report Generation
                logCallback("Generating Playlists and Excel Audit Reports...");
                var trackList = processedTracks.ToList();
                LastImportedTracks = trackList;
                await _database.SaveAsync(trackList, ct);
                var playlistGen = new PlaylistGenerator(_organizer.GetPlaylistDirectory());
                playlistGen.GenerateAllPlaylists(trackList);

                ReportExporter.ExportToExcel(trackList, Path.Combine(_organizer.GetReportDirectory(), "MusicLibraryReport.xlsx"));

                logCallback("Import and Organization Pipeline Completed Successfully!");
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    try { Directory.Delete(tempDirectory, true); } catch { }
                }
            }
        }

        private MusicCategory ClassifyTrack(AudioTrack track)
        {
            string genre = track.Genre.ToLowerInvariant();

            if (track.EnergyScore >= 75 || genre.Contains("edm") || genre.Contains("phonk") || genre.Contains("drum and bass") || genre.Contains("dnb"))
                return MusicCategory.PvP_Hype;

            if (genre.Contains("epic") || genre.Contains("motivational") || genre.Contains("cinematic") || genre.Contains("celebration"))
                return MusicCategory.Victory_Moments;

            if (track.EnergyScore <= 44 || genre.Contains("ambient") || genre.Contains("chill") || genre.Contains("lo-fi"))
                return MusicCategory.Lobby_Chill;

            return MusicCategory.Gameplay_Energy;
        }

        private IEnumerable<string> GetAudioFiles(string dir)
        {
            string[] extensions = { "*.mp3", "*.wav", "*.flac", "*.ogg", "*.m4a" };
            return extensions.SelectMany(ext => Directory.EnumerateFiles(dir, ext, SearchOption.AllDirectories));
        }

        private bool IsSupportedAudio(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".mp3" || ext == ".wav" || ext == ".flac" || ext == ".ogg" || ext == ".m4a";
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StreamMusicAIManager.Core;
using StreamMusicAIManager.Models;
using StreamMusicAIManager.Services;

namespace StreamMusicAIManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ObsWebSocketManager _obsManager = new();
        private readonly ImportPipeline _importPipeline;
        private double _progressValue;
        private string _statusMessage = "Ready";
        private int _totalSongs;
        private int _safeSongs;
        private int _unsafeSongs;
        private int _duplicatesRemoved;

        public ObservableCollection<AudioTrack> Tracks { get; } = new();
        public ObservableCollection<string> ActivityLogs { get; } = new();

        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public int TotalSongs
        {
            get => _totalSongs;
            set { _totalSongs = value; OnPropertyChanged(); }
        }

        public int SafeSongs
        {
            get => _safeSongs;
            set { _safeSongs = value; OnPropertyChanged(); }
        }

        public int UnsafeSongs
        {
            get => _unsafeSongs;
            set { _unsafeSongs = value; OnPropertyChanged(); }
        }

        public int DuplicatesRemoved
        {
            get => _duplicatesRemoved;
            set { _duplicatesRemoved = value; OnPropertyChanged(); }
        }

        public ICommand SwitchStreamModeCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportReportCommand { get; }

        public MainViewModel()
        {
            SwitchStreamModeCommand = new RelayCommand(async (param) => await ExecuteSwitchMode(param?.ToString()));
            ImportCommand = new RelayCommand(_ => ImportLibrary());
            ExportReportCommand = new RelayCommand(_ => ExportReport());
            var root = @"C:\StreamMusicLibrary";
            _importPipeline = new ImportPipeline(Path.Combine(AppContext.BaseDirectory, "Config"), root);
            InitObs();
        }

        private async void InitObs()
        {
            await _obsManager.ConnectAsync();
        }

        private async Task ExecuteSwitchMode(string? mode)
        {
            if (string.IsNullOrEmpty(mode)) return;
            
            AddLog($"Stream Mode Switched to: {mode}");
            string playlistFile = Path.Combine(@"C:\StreamMusicLibrary", "Playlists", $"{mode}.m3u");
            await _obsManager.SwitchPlaylistModeAsync(mode, playlistFile);
        }

        private void ImportLibrary()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Audio and archives|*.mp3;*.wav;*.flac;*.ogg;*.m4a;*.zip|All files|*.*"
            };
            if (dialog.ShowDialog() != true) return;
            ImportPaths(dialog.FileNames);
        }

        public async void ImportPaths(IEnumerable<string> paths)
        {
            var selectedPaths = paths.Where(path => File.Exists(path) || Directory.Exists(path)).ToList();
            if (selectedPaths.Count == 0) return;
            StatusMessage = "Importing...";
            try
            {
                var progress = new Progress<ProcessingProgress>(p =>
                {
                    ProgressValue = p.Percentage;
                    StatusMessage = $"{p.ProcessedItems}/{p.TotalItems}: {p.CurrentFile}";
                });
                await _importPipeline.ExecuteImportAsync(selectedPaths, progress, AddLog, CancellationToken.None);
                Tracks.Clear();
                foreach (var track in _importPipeline.LastImportedTracks) Tracks.Add(track);
                TotalSongs = Tracks.Count;
                SafeSongs = Tracks.Count(t => t.CopyrightStatus == CopyrightStatus.SAFE);
                UnsafeSongs = Tracks.Count(t => t.CopyrightStatus != CopyrightStatus.SAFE);
                DuplicatesRemoved = Tracks.Count(t => t.Category == MusicCategory.Duplicates);
                AddLog("Import completed.");
            }
            catch (Exception ex)
            {
                AddLog($"Import failed: {ex.Message}");
                MessageBox.Show(ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { StatusMessage = "Ready"; }
        }

        private void ExportReport()
        {
            var dialog = new SaveFileDialog { Filter = "Excel workbook|*.xlsx", FileName = "MusicLibraryReport.xlsx" };
            if (dialog.ShowDialog() != true) return;
            try { ReportExporter.ExportToExcel(Tracks.ToList(), dialog.FileName); AddLog($"Report exported to {dialog.FileName}"); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void AddLog(string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ActivityLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter!);
        public event EventHandler? CanExecuteChanged;
    }
}
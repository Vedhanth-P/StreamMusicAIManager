using System;
using System.IO;
using StreamMusicAIManager.Models;

namespace StreamMusicAIManager.Core
{
    public class LibraryOrganizer
    {
        private readonly string _baseDirectory;

        public LibraryOrganizer(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            InitializeDirectories();
        }

        private void InitializeDirectories()
        {
            string[] folders = {
                "PvP_Hype",
                "Gameplay_Energy",
                "Victory_Moments",
                "Lobby_Chill",
                "Duplicates",
                "Review_Required",
                "Unsafe_Copyright",
                "Reports",
                "Playlists"
            };

            foreach (var folder in folders)
            {
                Directory.CreateDirectory(Path.Combine(_baseDirectory, folder));
            }
        }

        public string SortTrack(AudioTrack track)
        {
            string targetFolder = track.Category switch
            {
                MusicCategory.Duplicates => "Duplicates",
                MusicCategory.Review_Required => "Review_Required",
                MusicCategory.Unsafe_Copyright => "Unsafe_Copyright",
                MusicCategory.PvP_Hype => "PvP_Hype",
                MusicCategory.Gameplay_Energy => "Gameplay_Energy",
                MusicCategory.Victory_Moments => "Victory_Moments",
                MusicCategory.Lobby_Chill => "Lobby_Chill",
                _ => "Review_Required"
            };

            string destinationDir = Path.Combine(_baseDirectory, targetFolder);
            string destinationPath = Path.Combine(destinationDir, track.FileName);

            // Handle filename collision within the same category
            if (File.Exists(destinationPath) && track.FilePath != destinationPath)
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(track.FileName);
                string ext = Path.GetExtension(track.FileName);
                destinationPath = Path.Combine(destinationDir, $"{fileNameNoExt}_{Guid.NewGuid().ToString().Substring(0, 5)}{ext}");
            }

            if (File.Exists(track.FilePath) && track.FilePath != destinationPath)
            {
                File.Move(track.FilePath, destinationPath);
                track.FilePath = destinationPath;
            }

            return destinationPath;
        }

        public string GetPlaylistDirectory() => Path.Combine(_baseDirectory, "Playlists");
        public string GetReportDirectory() => Path.Combine(_baseDirectory, "Reports");
    }
}
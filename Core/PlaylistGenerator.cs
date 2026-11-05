using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StreamMusicAIManager.Models;

namespace StreamMusicAIManager.Core
{
    public class PlaylistGenerator
    {
        private readonly string _outputDirectory;

        public PlaylistGenerator(string outputDirectory)
        {
            _outputDirectory = outputDirectory;
            Directory.CreateDirectory(_outputDirectory);
        }

        public void GenerateAllPlaylists(List<AudioTrack> tracks)
        {
            var validTracks = tracks.Where(t => t.CopyrightStatus == CopyrightStatus.SAFE).ToList();

            GenerateM3U("PvP_Hype.m3u", validTracks.Where(t => t.Category == MusicCategory.PvP_Hype));
            GenerateM3U("Gameplay_Energy.m3u", validTracks.Where(t => t.Category == MusicCategory.Gameplay_Energy));
            GenerateM3U("Victory_Moments.m3u", validTracks.Where(t => t.Category == MusicCategory.Victory_Moments));
            GenerateM3U("Lobby_Chill.m3u", validTracks.Where(t => t.Category == MusicCategory.Lobby_Chill));

            GenerateMegaPlaylist(validTracks);
        }

        private void GenerateM3U(string filename, IEnumerable<AudioTrack> tracks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#EXTM3U");

            foreach (var track in tracks)
            {
                sb.AppendLine($"#EXTINF:{(int)track.DurationSeconds},{track.Artist} - {track.Title}");
                sb.AppendLine(track.FilePath);
            }

            File.WriteAllText(Path.Combine(_outputDirectory, filename), sb.ToString(), Encoding.UTF8);
        }

        private void GenerateMegaPlaylist(List<AudioTrack> tracks)
        {
            var pvp = tracks.Where(t => t.Category == MusicCategory.PvP_Hype).ToList();
            var gameplay = tracks.Where(t => t.Category == MusicCategory.Gameplay_Energy).ToList();
            var victory = tracks.Where(t => t.Category == MusicCategory.Victory_Moments).ToList();
            var lobby = tracks.Where(t => t.Category == MusicCategory.Lobby_Chill).ToList();

            int targetTotal = tracks.Count;
            int countGameplay = (int)(targetTotal * 0.60);
            int countPvP = (int)(targetTotal * 0.25);
            int countVictory = (int)(targetTotal * 0.10);
            int countLobby = (int)(targetTotal * 0.05);

            var rng = new Random();
            var megaList = new List<AudioTrack>();

            megaList.AddRange(gameplay.OrderBy(_ => rng.Next()).Take(countGameplay));
            megaList.AddRange(pvp.OrderBy(_ => rng.Next()).Take(countPvP));
            megaList.AddRange(victory.OrderBy(_ => rng.Next()).Take(countVictory));
            megaList.AddRange(lobby.OrderBy(_ => rng.Next()).Take(countLobby));

            // Fisher-Yates Random Shuffle
            int n = megaList.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = megaList[k];
                megaList[k] = megaList[n];
                megaList[n] = value;
            }

            GenerateM3U("Mega_Stream_Playlist.m3u", megaList);
        }
    }
}
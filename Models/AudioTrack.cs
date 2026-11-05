using System;

namespace StreamMusicAIManager.Models
{
    public class AudioTrack
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileHashSHA256 { get; set; } = string.Empty;
        public string AudioFingerprint { get; set; } = string.Empty;
        public string Title { get; set; } = "Unknown Title";
        public string Artist { get; set; } = "Unknown Artist";
        public string Album { get; set; } = "Unknown Album";
        public string Genre { get; set; } = "Unknown Genre";
        public double Bpm { get; set; }
        public double DurationSeconds { get; set; }
        public int Year { get; set; }
        public double EnergyScore { get; set; }
        public string MoodScore { get; set; } = "Neutral";
        public MusicCategory Category { get; set; }
        public CopyrightStatus CopyrightStatus { get; set; }
        public string CopyrightReason { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
}
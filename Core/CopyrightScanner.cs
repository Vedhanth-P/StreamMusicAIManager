using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using StreamMusicAIManager.Models;

namespace StreamMusicAIManager.Core
{
    public class CopyrightScanner
    {
        private readonly HashSet<string> _safeLabels = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _safeLicenses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unsafePublishers = new(StringComparer.OrdinalIgnoreCase);

        public CopyrightScanner(string configPath)
        {
            LoadDatabase(configPath);
        }

        private void LoadDatabase(string xmlPath)
        {
            if (!File.Exists(xmlPath)) return;

            XDocument doc = XDocument.Load(xmlPath);
            foreach (var elem in doc.Descendants("Label")) _safeLabels.Add(elem.Value.Trim());
            foreach (var elem in doc.Descendants("License")) _safeLicenses.Add(elem.Value.Trim());
            foreach (var elem in doc.Descendants("Publisher")) _unsafePublishers.Add(elem.Value.Trim());
        }

        public (CopyrightStatus Status, string Reason) AuditTrack(AudioTrack track)
        {
            // Check Major Record Labels (Unsafe)
            foreach (var unsafePub in _unsafePublishers)
            {
                if (track.Artist.Contains(unsafePub, StringComparison.OrdinalIgnoreCase) ||
                    track.Album.Contains(unsafePub, StringComparison.OrdinalIgnoreCase))
                {
                    return (CopyrightStatus.UNSAFE, $"Matched major commercial copyright holder: {unsafePub}");
                }
            }

            // Check Known Royalty-Free Labels/Artists (Safe)
            foreach (var safeLabel in _safeLabels)
            {
                if (track.Artist.Contains(safeLabel, StringComparison.OrdinalIgnoreCase) ||
                    track.Album.Contains(safeLabel, StringComparison.OrdinalIgnoreCase) ||
                    track.Title.Contains(safeLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return (CopyrightStatus.SAFE, $"Verified royalty-free stream publisher: {safeLabel}");
                }
            }

            // Check Non-Commercial / Unknown
            return (CopyrightStatus.REVIEW_REQUIRED, "Metadata unverified against public royalty-free database. Manual review recommended.");
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using StreamMusicAIManager.Models;

namespace StreamMusicAIManager.Core
{
    public class DuplicateDetector
    {
        private readonly ConcurrentDictionary<string, AudioTrack> _hashRegistry = new();
        private readonly ConcurrentDictionary<string, AudioTrack> _fingerprintRegistry = new();

        public bool IsDuplicate(AudioTrack track, out string reason)
        {
            reason = string.Empty;

            // Step 1: Exact Bitwise Match (SHA256)
            if (_hashRegistry.TryGetValue(track.FileHashSHA256, out var existingHashTrack))
            {
                reason = $"Exact byte-for-byte duplicate of '{existingHashTrack.FileName}'";
                return true;
            }

            // Step 2: Acoustic Fingerprint Match
            if (!string.IsNullOrEmpty(track.AudioFingerprint) && 
                _fingerprintRegistry.TryGetValue(track.AudioFingerprint, out var existingPrintTrack))
            {
                reason = $"Acoustic audio match detected with '{existingPrintTrack.FileName}'";
                return true;
            }

            // Register track if unique
            _hashRegistry.TryAdd(track.FileHashSHA256, track);
            if (!string.IsNullOrEmpty(track.AudioFingerprint))
            {
                _fingerprintRegistry.TryAdd(track.AudioFingerprint, track);
            }

            return false;
        }
    }
}
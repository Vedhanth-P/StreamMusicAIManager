using System;
using System.Collections.Generic;
using System.Linq;
using StreamMusicAIManager.Models;

namespace StreamMusicAIManager.Services
{
    public class RecommendationEngine
    {
        public List<AudioTrack> QuerySmartAI(List<AudioTrack> library, string prompt)
        {
            string query = prompt.ToLowerInvariant();

            if (query.Contains("minecraft pvp") || query.Contains("bedwars") || query.Contains("eggwars"))
            {
                return library.Where(t => t.Category == MusicCategory.PvP_Hype && t.Bpm >= 120)
                              .OrderByDescending(t => t.EnergyScore)
                              .Take(25)
                              .ToList();
            }

            if (query.Contains("energetic tracks") || query.Contains("high energy"))
            {
                return library.Where(t => t.EnergyScore >= 70)
                              .OrderByDescending(t => t.EnergyScore)
                              .Take(30)
                              .ToList();
            }

            if (query.Contains("over 130 bpm") || query.Contains("fast"))
            {
                return library.Where(t => t.Bpm >= 130)
                              .OrderByDescending(t => t.Bpm)
                              .ToList();
            }

            // Fallback general safe search
            return library.Where(t => t.CopyrightStatus == CopyrightStatus.SAFE)
                          .OrderByDescending(t => t.EnergyScore)
                          .Take(20)
                          .ToList();
        }
    }
}
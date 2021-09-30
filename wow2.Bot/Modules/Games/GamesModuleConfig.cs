using System.Collections.Generic;
using wow2.Bot.Modules.Games.Counting;
using wow2.Bot.Modules.Games.VerbalMemory;

namespace wow2.Bot.Modules.Games
{
    public class GamesModuleConfig
    {
        public List<CountingLeaderboardEntry> CountingLeaderboardEntries { get; set; } = new();

        public List<VerbalMemoryLeaderboardEntry> VerbalMemoryLeaderboardEntries { get; set; } = new();
    }
}
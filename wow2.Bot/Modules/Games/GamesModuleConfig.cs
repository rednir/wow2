using System.Collections.Generic;
using System.Text.Json.Serialization;
using wow2.Bot.Modules.Games.Counting;
using wow2.Bot.Modules.Games.Typing;
using wow2.Bot.Modules.Games.VerbalMemory;

namespace wow2.Bot.Modules.Games
{
    public class GamesModuleConfig
    {
        public List<CountingLeaderboardEntry> CountingLeaderboard { get; set; } = new();

        public List<VerbalMemoryLeaderboardEntry> VerbalMemoryLeaderboard { get; set; } = new();

        public List<TypingLeaderboardEntry> TypingLeaderboard { get; set; } = new();

        [JsonIgnore]
        public GameResourceService GameResourceService { get; } = new();
    }
}
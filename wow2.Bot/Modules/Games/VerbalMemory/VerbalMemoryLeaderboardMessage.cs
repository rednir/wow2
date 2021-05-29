using System.Collections.Generic;
using System.Linq;

namespace wow2.Bot.Modules.Games.VerbalMemory
{
    public class VerbalMemoryLeaderboardMessage : LeaderboardMessage
    {
        public VerbalMemoryLeaderboardMessage(List<VerbalMemoryLeaderboardEntry> leaderboardEntries, int page = -1)
            : base(
                leaderboardEntries: leaderboardEntries.Cast<LeaderboardEntry>().ToArray(),
                detailsPredicate: e =>
                {
                    var entry = (VerbalMemoryLeaderboardEntry)e;
                    return $"Unique words: {entry.UniqueWords}";
                },
                title: "💬 Verbal memory leaderboard",
                description: "*The number of points is the total number of correct words.*",
                page: page)
        {
        }
    }
}
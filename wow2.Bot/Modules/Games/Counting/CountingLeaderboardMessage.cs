using System.Collections.Generic;
using System.Linq;

namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingLeaderboardMessage : LeaderboardMessage
    {
        public CountingLeaderboardMessage(IEnumerable<CountingLeaderboardEntry> leaderboardEntries, int? page = null)
            : base(
                leaderboardEntries: leaderboardEntries.Cast<LeaderboardEntry>(),
                detailsPredicate: e =>
                {
                    var entry = (CountingLeaderboardEntry)e;
                    return $"Increment: {entry.Increment} • Final number: {entry.FinalNumber}";
                },
                title: "🔢 Counting leaderboard",
                description: "*The number of points is the final number divided by the increment.*",
                page: page)
        {
        }
    }
}
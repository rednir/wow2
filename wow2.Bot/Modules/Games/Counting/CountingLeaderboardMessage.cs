using System.Collections.Generic;
using System.Linq;

namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingLeaderboardMessage : LeaderboardMessage
    {
        public CountingLeaderboardMessage(List<CountingLeaderboardEntry> leaderboardEntries, int page = -1)
            : base(
                leaderboardEntries: leaderboardEntries.Cast<LeaderboardEntry>().ToArray(),
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
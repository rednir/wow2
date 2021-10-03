using System.Collections.Generic;
using System.Linq;

namespace wow2.Bot.Modules.Games.Typing
{
    public class TypingLeaderboardMessage : LeaderboardMessage
    {
        public TypingLeaderboardMessage(List<TypingLeaderboardEntry> leaderboardEntries, int? page = null)
            : base(
                leaderboardEntries: leaderboardEntries.Cast<LeaderboardEntry>().ToArray(),
                detailsPredicate: e =>
                {
                    var entry = (TypingLeaderboardEntry)e;
                    return "todo";
                },
                title: "⌨ Typing leaderboard",
                description: "*The number of points is the words per minute.*",
                page: page)
        {
        }

        protected override bool OnlyShowUsersBestScore => true;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace wow2.Bot.Modules.Games.Typing
{
    public class TypingLeaderboardMessage : LeaderboardMessage
    {
        public TypingLeaderboardMessage(List<TypingLeaderboardEntry> leaderboardEntries, int? page = null)
            : base(
                leaderboardEntries: leaderboardEntries.Cast<LeaderboardEntry>().DistinctBy(e => e.PlayedByMention),
                detailsPredicate: e =>
                {
                    var entry = (TypingLeaderboardEntry)e;
                    return $"{Math.Round(entry.Wpm)} wpm, {Math.Round(entry.Accuracy * 100)}% accuracy";
                },
                title: "⌨ Typing leaderboard",
                description: "*The number of points is the words per minute multiplied by the accuracy.*",
                page: page)
        {
        }
    }
}
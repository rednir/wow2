using System.Collections.Generic;
using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingLeaderboardMessage : PagedMessage
    {
        public CountingLeaderboardMessage(List<CountingLeaderboardEntry> leaderboardEntries, int page = -1)
            : base(new List<EmbedFieldBuilder>(), string.Empty, "🔢 Counting leaderboard")
        {
            for (int i = 0; i < leaderboardEntries.Count; i++)
            {
                AllFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{i + 1}) {leaderboardEntries[i].NumberOfCorrectMessages} points",
                    Value = $"Started by {leaderboardEntries[i].PlayedByMention} at {leaderboardEntries[i].PlayedAt.ToShortDateString()}",
                });
            }

            Page = page;
            SetEmbedFields();
        }
    }
}
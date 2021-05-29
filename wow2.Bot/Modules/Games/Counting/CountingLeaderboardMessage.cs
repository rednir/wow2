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
                CountingLeaderboardEntry entry = leaderboardEntries[i];
                AllFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{i + 1}) {entry.NumberOfCorrectMessages} points",
                    Value = $"Started by {entry.PlayedByMention} at {entry.PlayedAt.ToShortDateString()}\nIncrement: {entry.Increment} • Final number: {entry.FinalNumber}",
                });
            }

            EmbedBuilder.Description = "*The number of points is the final number divided by the increment.";
            Page = page;
            SetEmbedFields();
        }
    }
}
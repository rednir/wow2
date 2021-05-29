using System.Collections.Generic;
using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.VerbalMemory
{
    public class VerbalMemoryLeaderboardMessage : PagedMessage
    {
        public VerbalMemoryLeaderboardMessage(List<VerbalMemoryLeaderboardEntry> leaderboardEntries, int page = -1)
            : base(new List<EmbedFieldBuilder>(), string.Empty, "💬 Verbal memory leaderboard")
        {
            for (int i = 0; i < leaderboardEntries.Count; i++)
            {
                VerbalMemoryLeaderboardEntry entry = leaderboardEntries[i];
                AllFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{i + 1}) {entry.Points} points",
                    Value = $"Played by {entry.PlayedByMention} at {entry.PlayedAt.ToShortDateString()}\nUnique words: {entry.UniqueWords}",
                });
            }

            EmbedBuilder.Description = "*The number of points is the total number of correct words.*";
            Page = page;
            SetEmbedFields();
        }
    }
}
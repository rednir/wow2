using System;
using System.Collections.Generic;
using Discord;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games
{
    public abstract class LeaderboardMessage : PagedMessage
    {
        protected LeaderboardMessage(
            LeaderboardEntry[] leaderboardEntries,
            Func<LeaderboardEntry, string> detailsPredicate,
            string title,
            string description = null,
            int page = -1)
            : base(new List<EmbedFieldBuilder>(), description, title)
        {
            for (int i = 0; i < leaderboardEntries.Length; i++)
            {
                LeaderboardEntry entry = leaderboardEntries[i];
                AllFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{i + 1}) {entry.Points} points",
                    Value = $"Game started by {entry.PlayedByMention} at {entry.PlayedAt.ToShortDateString()}\n{detailsPredicate.Invoke(entry)}",
                });
            }

            Page = page;
            SetEmbedFields();
        }
    }
}
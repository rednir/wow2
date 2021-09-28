using System;
using System.Collections.Generic;
using Discord;
using wow2.Bot.Extensions;
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
            int? page = null)
            : base(new List<EmbedFieldBuilder>(), description, title)
        {
            for (int i = 0; i < leaderboardEntries.Length; i++)
            {
                LeaderboardEntry entry = leaderboardEntries[i];
                AllFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{i + 1}) {entry.Points} points",
                    Value = $"Game started by {entry.PlayedByMention} at {entry.PlayedAt.ToDiscordTimestamp("f")}\n{detailsPredicate.Invoke(entry)}",
                });
            }

            Page = page;
            UpdateEmbed();
        }
    }
}
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
            IEnumerable<LeaderboardEntry> leaderboardEntries,
            Func<LeaderboardEntry, string> detailsPredicate,
            string title,
            string description = null,
            int? page = null)
            : base(new List<EmbedFieldBuilder>(), description, title)
        {
            int num = 1;
            foreach (var entry in leaderboardEntries)
            {
                AllFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{num}) {entry.Points} points",
                    Value = $"{entry.PlayedByMention} at {entry.PlayedAt.ToDiscordTimestamp("f")}\n{detailsPredicate.Invoke(entry)}",
                });
                num++;
            }

            Page = page;
            UpdateProperties();
        }
    }
}
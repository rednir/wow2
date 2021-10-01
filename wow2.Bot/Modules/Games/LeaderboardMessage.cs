using System;
using System.Collections.Generic;
using Discord;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games
{
    public abstract class LeaderboardMessage : PagedMessage
    {
        protected virtual bool OnlyShowUsersBestScore => false;

        protected LeaderboardMessage(
            LeaderboardEntry[] leaderboardEntries,
            Func<LeaderboardEntry, string> detailsPredicate,
            string title,
            string description = null,
            int? page = null)
            : base(new List<EmbedFieldBuilder>(), description, title)
        {
            var seenUsers = new List<string>();
            for (int i = 0; i < leaderboardEntries.Length; i++)
            {
                LeaderboardEntry entry = leaderboardEntries[i];
                if (!seenUsers.Contains(entry.PlayedByMention) || !OnlyShowUsersBestScore)
                {
                    seenUsers.Add(entry.PlayedByMention);
                    AllFieldBuilders.Add(new EmbedFieldBuilder()
                    {
                        Name = $"{i + 1}) {entry.Points} points",
                        Value = $"{entry.PlayedByMention} at {entry.PlayedAt.ToDiscordTimestamp("f")}\n{detailsPredicate.Invoke(entry)}",
                    });
                }
            }

            Page = page;
            UpdateProperties();
        }
    }
}
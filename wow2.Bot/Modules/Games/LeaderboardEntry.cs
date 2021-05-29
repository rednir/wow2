using System;
using Discord;

namespace wow2.Bot.Modules.Games
{
    public abstract class LeaderboardEntry
    {
        protected LeaderboardEntry(IUser user = null)
        {
            PlayedAt = DateTime.Now;
            PlayedByMention = user?.Mention;
        }

        public int Points { get; set; }

        public DateTime PlayedAt { get; set; }

        public string PlayedByMention { get; set; }
    }
}
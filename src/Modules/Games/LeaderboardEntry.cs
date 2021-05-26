using System;
using Discord;

namespace wow2.Modules.Games
{
    public abstract class LeaderboardEntry
    {
        protected LeaderboardEntry(IUser user = null)
        {
            PlayedAt = DateTime.Now;
            PlayedByMention = user.Mention;
        }

        public DateTime PlayedAt { get; }

        public string PlayedByMention { get; }
    }
}
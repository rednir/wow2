using System;

namespace wow2.Bot.Modules.Games
{
    public abstract class LeaderboardEntry
    {
        protected LeaderboardEntry(GameMessage gameMessage)
        {
            Points = gameMessage?.Points ?? default;
            PlayedAt = DateTime.Now;
            PlayedByMention = gameMessage?.InitialContext.User.Mention;
        }

        public int Points { get; set; }

        public DateTime PlayedAt { get; set; }

        public string PlayedByMention { get; set; }
    }
}
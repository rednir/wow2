using System;
using wow2.Modules.Games.Counting;

namespace wow2.Modules.Games
{
    public abstract class LeaderboardEntry
    {
        public DateTime PlayedAt { get; }
    }
}
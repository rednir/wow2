using System;
using System.Collections.Generic;
using Discord.Commands;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games
{
    public abstract class GameMessage : InteractiveMessage
    {
        protected GameMessage(SocketCommandContext context, LeaderboardEntry[] leaderboard, GameResourceService resourceService)
        {
            InitialContext = context;
            Leaderboard = leaderboard;
            ResourceService = resourceService;
        }

        public SocketCommandContext InitialContext { get; }

        public Func<int> SubmitGame { get; set; }

        protected LeaderboardEntry[] Leaderboard { get; }

        protected GameResourceService ResourceService { get; }
    }
}
using System;
using Discord.Commands;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games
{
    public abstract class GameMessage : InteractiveMessage
    {
        protected GameMessage(SocketCommandContext context)
        {
            InitialContext = context;
        }

        public SocketCommandContext InitialContext { get; }

        public Func<int> SubmitGame { get; set; }
    }
}
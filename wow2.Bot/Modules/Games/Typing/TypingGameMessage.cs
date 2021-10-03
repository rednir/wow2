using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.Typing
{
    public class TypingGameMessage : GameMessage
    {
        public TypingGameMessage(SocketCommandContext context, GameResourceService resourceService)
            : base(context, null, resourceService)
        {
        }
    }
}
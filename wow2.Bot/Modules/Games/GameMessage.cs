using Discord.Commands;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games
{
    public abstract class GameMessage : InteractiveMessage
    {
        public SocketCommandContext InitialContext { get; set; }
    }
}
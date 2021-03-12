using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Modules.Games
{
    public class GamesModuleConfig
    {
        // Might be a good idea to make this a seperate class.
        public float CountingNextNumber { get; set; } = 1;
        public float CountingIncrement { get; set; } = 1;
        public List<SocketMessage> CountingListOfMessages { get; set; } = new List<SocketMessage>();
        public ISocketMessageChannel CountingChannel { get; set; }
    }
}
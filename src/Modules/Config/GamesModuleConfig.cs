using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Modules.Config
{
    public class GamesModuleConfig
    {
        public int CountingNextNumber { get; set; } = 1;
        public List<SocketMessage> CountingListOfMessages { get; set; } = new List<SocketMessage>();
        public ISocketMessageChannel CountingChannel { get; set; }
    }
}
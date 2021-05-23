using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingGameConfig : GameConfig
    {
        public float Increment { get; set; }
        public List<SocketMessage> ListOfMessages { get; set; } = new List<SocketMessage>();
        public float NextNumber { get; set; }
    }
}
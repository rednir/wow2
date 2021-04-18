using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Modules.Games.Counting
{
    public class CountingGameConfig : GameConfigBase
    {
        public float Increment { get; set; }
        public List<SocketMessage> ListOfMessages { get; set; } = new List<SocketMessage>();
        public float NextNumber { get; set; }
    }
}
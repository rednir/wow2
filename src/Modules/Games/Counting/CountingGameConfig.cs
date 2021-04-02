using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Modules.Games.Counting
{
    public class CountingGameConfig : GameConfigBase
    {
        public float Increment { get; set; }
        public List<SocketMessage> ListOfMessages { get; set; } = new List<SocketMessage>();

        /// <summary>Represents the next correct number when counting, or null if counting has ended.</summary>
        public float? NextNumber { get; set; }
    }
}
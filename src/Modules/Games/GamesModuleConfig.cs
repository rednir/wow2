using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Modules.Games
{
    public class GamesModuleConfig
    {
        // Might be a good idea to make this a seperate class.
        /// <summary>Represents the next correct number when counting, or null if counting has ended.</summary>
        public float? CountingNextNumber { get; set; }
        public float CountingIncrement { get; set; }
        public List<SocketMessage> CountingListOfMessages { get; set; } = new List<SocketMessage>();
        public ISocketMessageChannel CountingChannel { get; set; }
    }
}
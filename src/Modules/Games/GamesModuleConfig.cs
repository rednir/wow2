using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace wow2.Modules.Games
{
    public class GamesModuleConfig
    {
        [JsonIgnore]
        public CountingConfig Counting { get; set; } = new CountingConfig();
    }

    public class CountingConfig
    {
        /// <summary>Represents the next correct number when counting, or null if counting has ended.</summary>
        public float? NextNumber { get; set; }
        public float Increment { get; set; }
        public List<SocketMessage> ListOfMessages { get; set; } = new List<SocketMessage>();
        public ISocketMessageChannel Channel { get; set; }
    }
}
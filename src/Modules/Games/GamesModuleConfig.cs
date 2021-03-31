using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord.WebSocket;
using Discord.Commands;

namespace wow2.Modules.Games
{
    public class GamesModuleConfig
    {
        [JsonIgnore]
        public CountingConfig Counting { get; set; } = new CountingConfig();

        [JsonIgnore]
        public VerbalMemoryConfig VerbalMemory { get; set; } = new VerbalMemoryConfig();
    }

    public class CountingConfig
    {
        /// <summary>Represents the next correct number when counting, or null if counting has ended.</summary>
        public float? NextNumber { get; set; }
        public float Increment { get; set; }
        public List<SocketMessage> ListOfMessages { get; set; } = new List<SocketMessage>();
        public ISocketMessageChannel Channel { get; set; }
    }

    public class VerbalMemoryConfig
    {
        public string[] UnseenWords { get; set; } = { };
        public string[] SeenWords { get; set; } = { };

        public ICommandContext InitalContext { get; set; }
    }
}
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace wow2.Modules.Games.Counting
{
    public class CountingGameConfig : GameConfig
    {
        [JsonIgnore]
        public float Increment { get; set; }

        [JsonIgnore]
        public List<SocketMessage> ListOfMessages { get; set; } = new();

        [JsonIgnore]
        public float NextNumber { get; set; }
        
        public List<CountingLeaderboardEntry> LeaderboardEntries { get; } = new();
    }
}
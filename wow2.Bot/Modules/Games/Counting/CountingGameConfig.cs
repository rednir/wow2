using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord.Commands;
using Discord.WebSocket;

namespace wow2.Bot.Modules.Games.Counting
{
    public class CountingGameConfig
    {
        [JsonIgnore]
        public ICommandContext InitialContext { get; set; }

        [JsonIgnore]
        public bool IsGameStarted { get; set; }

        [JsonIgnore]
        public float Increment { get; set; }

        [JsonIgnore]
        public List<SocketMessage> ListOfMessages { get; set; } = new();

        [JsonIgnore]
        public float NextNumber { get; set; }

        public List<CountingLeaderboardEntry> LeaderboardEntries { get; set; } = new();
    }
}
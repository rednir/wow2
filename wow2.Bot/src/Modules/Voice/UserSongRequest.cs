using System;
using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace wow2.Bot.Modules.Voice
{
    public class UserSongRequest
    {
        public VideoMetadata VideoMetadata { get; set; }
        public DateTime TimeRequested { get; set; }

        [JsonIgnore]
        public SocketUser RequestedBy { get; set; }
    }
}
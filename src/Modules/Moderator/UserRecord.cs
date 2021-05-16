using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace wow2.Modules.Moderator
{
    public class UserRecord
    {
        public ulong UserId { get; set; }
        public List<Warning> Warnings { get; set; } = new List<Warning>();
        public List<Mute> Mutes { get; set; } = new List<Mute>();

        [JsonIgnore]
        public List<SocketMessage> Messages { get; set; } = new List<SocketMessage>();
    }
}
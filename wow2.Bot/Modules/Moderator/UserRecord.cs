using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace wow2.Bot.Modules.Moderator
{
    public class UserRecord
    {
        public ulong UserId { get; set; }

        public List<Warning> Warnings { get; set; } = new();

        public List<Mute> Mutes { get; set; } = new();

        [JsonIgnore]
        public List<SocketMessage> Messages { get; set; } = new();

        [JsonIgnore]
        public List<DateTimeOffset> CommandExecutedDateTimes { get; set; } = new();
    }
}
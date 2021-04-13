using System.Collections.Generic;
using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace wow2.Modules.Moderator
{
    public class ModeratorModuleConfig
    {
        public List<UserRecord> UserRecords { get; set; } = new List<UserRecord>();
        public int WarningsUntilBan { get; set; } = -1;
        public bool IsAutoModOn { get; set; } = false;
    }

    public class UserRecord
    {
        public ulong UserId { get; set; } 
        public List<Warning> Warnings { get; set; } = new List<Warning>();
        public List<Mute> Mutes { get; set; } = new List<Mute>();

        [JsonIgnore]
        public List<SocketMessage> Messages { get; set; } = new List<SocketMessage>();
    }

    public abstract class Incident
    {
        public ulong RequestedBy { get; set; }
        public long DateTimeBinary { get; set; }
    }

    public class Warning : Incident
    {
    }

    public class Mute : Incident
    {
    }
}
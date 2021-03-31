using System.Collections.Generic;

namespace wow2.Modules.Moderator
{
    public class ModeratorModuleConfig
    {
        public List<UserRecord> UserRecords = new List<UserRecord>();
    }

    public class UserRecord
    {
        public ulong UserId { get; set; }
        public List<Warning> Warnings { get; set; } = new List<Warning>();
        public List<Mute> Mutes { get; set; } = new List<Mute>();
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
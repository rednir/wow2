using System.Collections.Generic;

namespace wow2.Bot.Modules.Moderator
{
    public class ModeratorModuleConfig
    {
        public List<UserRecord> UserRecords { get; set; } = new List<UserRecord>();

        public int WarningsUntilBan { get; set; } = -1;

        public bool IsAutoModOn { get; set; } = false;
    }
}
using System.Collections.Generic;

namespace wow2.Bot.Modules.Timers
{
    public class TimersModuleConfig
    {
        public List<UserTimer> UserTimers { get; set; } = new();

        public bool IsTimerElapsedMessagesOn { get; set; } = true;

        public ulong AnnouncementsChannelId { get; set; }
    }
}
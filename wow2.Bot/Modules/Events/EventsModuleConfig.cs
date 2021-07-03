using System.Collections.Generic;

namespace wow2.Bot.Modules.Events
{
    public class EventsModuleConfig
    {
        public ulong AnnouncementsChannelId { get; set; }

        public List<DateTimeSelectorMessage> DateTimeSelectorMessages { get; set; } = new();
    }
}
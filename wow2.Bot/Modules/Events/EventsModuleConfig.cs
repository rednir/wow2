using System.Collections.Generic;

namespace wow2.Bot.Modules.Events
{
    public class EventsModuleConfig
    {
        public List<Event> Events { get; set; } = new();

        public ulong AnnouncementsChannelId { get; set; }
    }
}
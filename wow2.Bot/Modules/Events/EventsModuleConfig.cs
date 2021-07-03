using System.Collections.Generic;
using System.Text.Json.Serialization;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Events
{
    public class EventsModuleConfig
    {
        public ulong AnnouncementsChannelId { get; set; }
    }
}
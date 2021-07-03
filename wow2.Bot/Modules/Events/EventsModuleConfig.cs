using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace wow2.Bot.Modules.Events
{
    public class EventsModuleConfig
    {
        public ulong AnnouncementsChannelId { get; set; }

        [JsonIgnore]
        public List<DateTimeSelectorMessage> DateTimeSelectorMessages { get; set; } = new();
    }
}
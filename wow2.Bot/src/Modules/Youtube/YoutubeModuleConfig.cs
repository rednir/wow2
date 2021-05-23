using System.Collections.Generic;

namespace wow2.Modules.YouTube
{
    public class YouTubeModuleConfig
    {
        public ulong AnnouncementsChannelId { get; set; }
        public List<SubscribedChannel> SubscribedChannels { get; set; } = new List<SubscribedChannel>();
    }
}
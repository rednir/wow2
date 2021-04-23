using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Modules.Youtube
{
    public class YoutubeModuleConfig
    {
        public ulong AnnouncementsChannelId { get; set; }
        public List<SubscribedChannel> SubscribedChannelIds { get; set; } = new List<SubscribedChannel>();
    }
}
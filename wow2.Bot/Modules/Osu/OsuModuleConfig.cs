using System.Collections.Generic;

namespace wow2.Bot.Modules.Osu
{
    public class OsuModuleConfig
    {
        public List<SubscribedUserData> SubscribedUsers { get; set; } = new();

        public ulong AnnouncementsChannelId { get; set; }
    }
}
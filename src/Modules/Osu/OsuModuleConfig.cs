using System.Collections.Generic;

namespace wow2.Modules.Osu
{
    public class OsuModuleConfig
    {
        public List<UserData> SubscribedUsers { get; set; } = new();
    }
}
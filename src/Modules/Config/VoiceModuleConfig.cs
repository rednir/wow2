using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Modules.Config
{
    public class VoiceModuleConfig
    {
        public Queue<UserSongRequest> SongRequests { get; set; }
    }

    public class UserSongRequest
    {
        public string Url { get; set; }
        public SocketUser Author { get; set; }
    }
}
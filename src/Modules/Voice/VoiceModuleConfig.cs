using System;
using System.Collections.Generic;
using Discord.WebSocket;

namespace wow2.Modules.Voice
{
    public class VoiceModuleConfig
    {
        public Queue<UserSongRequest> SongRequests { get; set; } = new Queue<UserSongRequest>();
    }

    public class UserSongRequest
    {
        public YoutubeVideoMetadata VideoMetadata { get; set; }
        public DateTime TimeRequested { get; set; }
        public SocketUser Author { get; set; }
    }
}
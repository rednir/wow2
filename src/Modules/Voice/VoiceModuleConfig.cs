using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Discord.WebSocket;
using Discord.Audio;

namespace wow2.Modules.Voice
{
    public class VoiceModuleConfig
    {
        public Queue<UserSongRequest> SongRequests { get; set; } = new Queue<UserSongRequest>();
        
        [JsonIgnore]
        public IAudioClient AudioClient { get; set; }
    }

    public class UserSongRequest
    {
        public YoutubeVideoMetadata VideoMetadata { get; set; }
        public DateTime TimeRequested { get; set; }
        public SocketUser Author { get; set; }
    }
}
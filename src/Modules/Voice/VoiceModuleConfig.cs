using System;
using System.Threading;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Discord.WebSocket;
using Discord.Audio;

namespace wow2.Modules.Voice
{
    public class VoiceModuleConfig
    {
        [JsonIgnore]
        public Queue<UserSongRequest> SongRequests { get; set; } = new Queue<UserSongRequest>();

        [JsonIgnore]
        public IAudioClient AudioClient { get; set; }

        [JsonIgnore]
        public bool CurrentlyPlayingAudio { get; set; } = false;

        [JsonIgnore]
        public CancellationTokenSource CtsForAudioStreaming { get; set; } = new CancellationTokenSource();
    }

    public class UserSongRequest
    {
        public YoutubeVideoMetadata VideoMetadata { get; set; }
        public DateTime TimeRequested { get; set; }
        public SocketUser RequestedBy { get; set; }
    }
}
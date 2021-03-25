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
        public bool IsAutoNpOn { get; set; } = true;
        public int VoteSkipsNeeded { get; set; } = 1;
        
        [JsonIgnore]
        public List<ulong> ListOfUserIdsThatVoteSkipped { get; set; } = new List<ulong>();

        [JsonIgnore]
        public Queue<UserSongRequest> SongRequests { get; set; } = new Queue<UserSongRequest>();

        [JsonIgnore]
        public UserSongRequest CurrentlyPlayingSongRequest { get; set; }

        [JsonIgnore]
        public IAudioClient AudioClient { get; set; }

        [JsonIgnore]
        public CancellationTokenSource CtsForAudioStreaming { get; set; } = new CancellationTokenSource();
    }

    public class UserSongRequest
    {
        public VideoMetadata VideoMetadata { get; set; }
        public DateTime TimeRequested { get; set; }
        public SocketUser RequestedBy { get; set; }
    }
}
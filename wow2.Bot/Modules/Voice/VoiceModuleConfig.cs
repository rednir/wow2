using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using Discord.Audio;

namespace wow2.Bot.Modules.Voice
{
    public class VoiceModuleConfig
    {
        public bool IsAutoNpOn { get; set; } = true;
        public bool IsAutoJoinOn { get; set; } = true;
        public int VoteSkipsNeeded { get; set; } = 1;
        public Queue<UserSongRequest> CurrentSongRequestQueue { get; set; } = new();
        public Dictionary<string, Queue<UserSongRequest>> SavedSongRequestQueues { get; set; } = new();

        [JsonIgnore]
        public List<ulong> ListOfUserIdsThatVoteSkipped { get; set; } = new();

        [JsonIgnore]
        public UserSongRequest CurrentlyPlayingSongRequest { get; set; }

        [JsonIgnore]
        public bool IsLoopEnabled { get; set; } = false;

        [JsonIgnore]
        public IAudioClient AudioClient { get; set; }

        [JsonIgnore]
        public CancellationTokenSource CtsForAudioStreaming { get; set; } = new();
    }
}
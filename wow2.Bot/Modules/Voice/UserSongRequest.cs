using System;

namespace wow2.Bot.Modules.Voice
{
    public class UserSongRequest
    {
        public VideoMetadata VideoMetadata { get; set; }

        public DateTime TimeRequested { get; set; }

        public string RequestedByMention { get; set; }
    }
}
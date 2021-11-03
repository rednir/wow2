using System;
using Discord;

namespace wow2.Bot.Modules.Voice
{
    public class UserSongRequest
    {
        public UserSongRequest()
        {
        }

        public UserSongRequest(VideoMetadata metadata, IUser requestedBy)
        {
            VideoMetadata = metadata;
            TimeRequested = DateTime.Now;
            RequestedByMention = requestedBy.Mention;
        }

        public VideoMetadata VideoMetadata { get; set; }

        public DateTime TimeRequested { get; set; }

        public string RequestedByMention { get; set; }
    }
}
using System;

namespace wow2.Bot.Modules.Main
{
    public class VotingEnabledAttachment
    {
        public string Link { get; set; }

        public string AuthorMention { get; set; }

        public DateTime DateTime { get; set; }

        public int Likes { get; set; }

        public int Dislikes { get; set; }
    }
}
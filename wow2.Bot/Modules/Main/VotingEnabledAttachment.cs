using System;
using System.Linq;
using Discord.Commands;

namespace wow2.Bot.Modules.Main
{
    public class VotingEnabledAttachment
    {
        public VotingEnabledAttachment()
        {
        }

        public VotingEnabledAttachment(SocketCommandContext context)
        {
            Link = context.Message.Attachments.FirstOrDefault()?.Url;
            AuthorMention = context.User.Mention;
            DateTime = context.Message.Timestamp;
        }

        public string Link { get; set; }

        public string AuthorMention { get; set; }

        public DateTimeOffset DateTime { get; set; }

        public int Likes { get; set; }

        public int Dislikes { get; set; }
    }
}
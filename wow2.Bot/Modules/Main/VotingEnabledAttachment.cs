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
            ChannelId = context.Channel.Id;
            MessageId = context.Message.Id;
            FileName = context.Message.Attachments.FirstOrDefault()?.Url.Split('/').Last();
            AuthorMention = context.User.Mention;
            DateTime = context.Message.Timestamp;
        }

        public string AttachmentUrl => $"https://cdn.discordapp.com/attachments/{ChannelId}/{MessageId}/{FileName}";

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }

        public string FileName { get; set; }

        public string AuthorMention { get; set; }

        public DateTimeOffset DateTime { get; set; }

        public int Likes { get; set; }

        public int Dislikes { get; set; }
    }
}
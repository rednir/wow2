using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;

namespace wow2.Bot.Modules.Main
{
    public class VotingEnabledAttachment
    {
        public VotingEnabledAttachment()
        {
        }

        public VotingEnabledAttachment(ICommandContext context)
        {
            GuildId = context.Guild.Id;
            ChannelId = context.Channel.Id;
            MessageId = context.Message.Id;
            FileName = context.Message.Attachments.FirstOrDefault()?.Url.Split('/').Last();
            AuthorMention = context.User.Mention;
            DateTime = context.Message.Timestamp.DateTime;
        }

        public string MessageUrl => $"https://cdn.discordapp.com/channels/{GuildId}/{ChannelId}/{MessageId}";

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public ulong MessageId { get; set; }

        public string FileName { get; set; }

        public string AuthorMention { get; set; }

        public DateTime DateTime { get; set; }

        public int Points => UsersLikedIds.Count - UsersDislikedIds.Count;

        public List<ulong> UsersLikedIds { get; set; } = new();

        public List<ulong> UsersDislikedIds { get; set; } = new();
    }
}
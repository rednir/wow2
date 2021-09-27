using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.AttachmentVoting
{
    [Name("Attachment Voting")]
    [Group("attachment")]
    [Alias("attachments", "voting", "attachment-voting")]
    [Summary("Enable users to vote on attachments in messages.")]
    public class AttachmentVotingModule : Module
    {
        public static readonly IEmote LikeReactionEmote = new Emoji("👍");
        public static readonly IEmote DislikeReactionEmote = new Emoji("👎");

        public AttachmentVotingModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].AttachmentVoting;

        public static async Task CheckMessageAsync(SocketCommandContext context)
        {
            var config = DataManager.AllGuildData[context.Guild.Id].AttachmentVoting;

            if (!DataManager.AllGuildData[context.Guild.Id].AttachmentVoting.VotingEnabledChannelIds.Contains(context.Channel.Id))
                return;

            // TODO: probably a better way of doing this.
            if (context.Message.Attachments.Count == 0
                && !context.Message.Content.Contains("://www.youtube.com/watch?v=")
                && !context.Message.Content.Contains("://youtu.be/")
                && !(context.Message.Content.Contains("twitch.tv/")
                    && (context.Message.Content.Contains("/clip/") || context.Message.Content.Contains("/videos/") || context.Message.Content.Contains("://clips."))))
            {
                return;
            }

            _ = context.Message.AddReactionsAsync(new[] { LikeReactionEmote, DislikeReactionEmote });
            config.VotingEnabledAttachments.Add(new VotingEnabledAttachment(context));
        }

        public static bool ActOnReactionAdded(SocketReaction reaction)
        {
            var config = DataManager.AllGuildData[reaction.Channel.GetGuild().Id].AttachmentVoting;

            VotingEnabledAttachment attachment = config.VotingEnabledAttachments.Find(a => a.MessageId == reaction.MessageId);
            if (attachment == null)
                return false;

            if (reaction.Emote.Name == LikeReactionEmote.Name && !attachment.UsersLikedIds.Contains(reaction.UserId))
                attachment.UsersLikedIds.Add(reaction.UserId);
            else if (reaction.Emote.Name == DislikeReactionEmote.Name && !attachment.UsersDislikedIds.Contains(reaction.UserId))
                attachment.UsersDislikedIds.Add(reaction.UserId);
            else
                return false;

            return true;
        }

        public static bool ActOnReactionRemoved(SocketReaction reaction)
        {
            var config = DataManager.AllGuildData[reaction.Channel.GetGuild().Id].AttachmentVoting;

            VotingEnabledAttachment attachment = config.VotingEnabledAttachments.Find(a => a.MessageId == reaction.MessageId);
            if (attachment == null)
                return false;

            if (reaction.Emote.Name == LikeReactionEmote.Name && attachment.UsersLikedIds.Contains(reaction.UserId))
                attachment.UsersLikedIds.Remove(reaction.UserId);
            else if (reaction.Emote.Name == DislikeReactionEmote.Name && attachment.UsersDislikedIds.Contains(reaction.UserId))
                attachment.UsersDislikedIds.Remove(reaction.UserId);
            else
                return false;

            return true;
        }

        [Command("toggle")]
        [Summary("Toggles whether the specified text channel will have thumbs up/down reactions for each new message with attachment posted there.")]
        public async Task ToggleAsync(SocketTextChannel channel)
        {
            bool currentlyOn = Config.VotingEnabledChannelIds.Contains(channel.Id);
            await SendToggleQuestionAsync(
                currentState: currentlyOn,
                setter: x =>
                {
                    if (x && !currentlyOn)
                        Config.VotingEnabledChannelIds.Add(channel.Id);
                    else if (!x && currentlyOn)
                        Config.VotingEnabledChannelIds.Remove(channel.Id);
                },
                toggledOnMessage: $"Every new message with an attachment in {channel.Mention} will have thumbs up/down reactions added.",
                toggledOffMessage: $"Messages in {channel.Mention} will no longer have thumbs up/down reactions added.");
        }

        [Command("list")]
        [Summary("Lists all attachments with voting enabled. SORT can be points/users/date/likes/deletions/values, default is likes.")]
        public async Task ListAsync(AttachmentSorts sort = AttachmentSorts.Points, int page = 1)
        {
            if (Config.VotingEnabledAttachments.Count < 1)
                throw new CommandReturnException(Context, "There are no voting enabled attachments in this server.", "Nothing to show");

            var listOfFieldBuilders = new List<EmbedFieldBuilder>(GetStatisticsFieldBuilders());
            var attachmentsCollection = getAttachments();

            int num = 1;
            foreach (var attachment in attachmentsCollection)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{num}) {attachment.Points} points",
                    Value = $"[{attachment.FileName}]({attachment.MessageUrl}) by {attachment.AuthorMention} at {attachment.DateTime.ToShortDateString()}\n{attachment.UsersLikedIds.Count} {LikeReactionEmote}   |   {attachment.UsersDislikedIds.Count} {DislikeReactionEmote}",
                });
                num++;
            }

            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                description: $"**Total attachments: **{Config.VotingEnabledAttachments.Count}\n**Total likes: **{Config.VotingEnabledAttachments.Sum(a => a.UsersLikedIds.Count)}\n**Total dislikes: **{Config.VotingEnabledAttachments.Sum(a => a.UsersDislikedIds.Count)}\n**Total contributing users: **{Config.VotingEnabledAttachments.GroupBy(a => a.AuthorMention).Count()}\n",
                title: "🖼 Voting-enabled Attachments",
                page: page)
                    .SendAsync(Context.Channel);

            VotingEnabledAttachment[] getAttachments()
            {
                return sort switch
                {
                    AttachmentSorts.Users => Config.VotingEnabledAttachments.OrderByDescending(p => p.UsersLikedIds.Concat(p.UsersDislikedIds).Distinct()).ToArray(),
                    AttachmentSorts.Likes => Config.VotingEnabledAttachments.OrderByDescending(p => p.UsersLikedIds.Count).ToArray(),
                    AttachmentSorts.Dislikes => Config.VotingEnabledAttachments.OrderByDescending(p => p.UsersDislikedIds.Count).ToArray(),
                    AttachmentSorts.Points => Config.VotingEnabledAttachments.OrderByDescending(p => p.Points).ToArray(),
                    _ => Config.VotingEnabledAttachments.ToArray(),
                };
            }
        }

        private List<EmbedFieldBuilder> GetStatisticsFieldBuilders()
        {
            var groupedUsers = Config.VotingEnabledAttachments.GroupBy(a => a.AuthorMention);

            var statBuilders = new Dictionary<string, Func<string>>()
            {
                ["Most loved"] = () =>
                {
                    string userMention = null;
                    decimal averageLikes = 0;
                    foreach (var user in groupedUsers)
                    {
                        decimal thisAverageLikes = user.Select(a => (decimal)a.UsersLikedIds.Count).Average();
                        if (thisAverageLikes >= averageLikes)
                        {
                            userMention = user.Key;
                            averageLikes = thisAverageLikes;
                        }
                    }

                    return $"{userMention} with an average of `{Math.Round(averageLikes, 2)}` likes per attachment";
                },
                ["Most hated"] = () =>
                {
                    string userMention = null;
                    decimal averageDislikes = 0;
                    foreach (var user in groupedUsers)
                    {
                        decimal thisAverageDislikes = user.Select(a => (decimal)a.UsersDislikedIds.Count).Average();
                        if (thisAverageDislikes >= averageDislikes)
                        {
                            userMention = user.Key;
                            averageDislikes = thisAverageDislikes;
                        }
                    }

                    return $"{userMention} with an average of `{Math.Round(averageDislikes, 2)}` dislikes per attachment";
                },
                ["Most prolific"] = () =>
                {
                    string userMention = null;
                    int count = 0;
                    foreach (var user in groupedUsers)
                    {
                        int thisCount = user.Count();
                        if (thisCount >= count)
                        {
                            userMention = user.Key;
                            count = thisCount;
                        }
                    }

                    return $"{userMention} with `{count}` attachments sent";
                },
                ["Most ignored"] = () =>
                {
                    string userMention = null;
                    int ignored = 0;
                    foreach (var user in groupedUsers)
                    {
                        int thisIgnored = user.Count(a => a.UsersLikedIds.Count == 0 && a.UsersDislikedIds.Count == 0);
                        if (thisIgnored >= ignored)
                        {
                            userMention = user.Key;
                            ignored = thisIgnored;
                        }
                    }

                    return $"{userMention} with `{ignored}` attachments that have no likes or dislikes";
                },
                ["Most controversial"] = () =>
                {
                    string userMention = null;
                    decimal rating = decimal.MaxValue;
                    foreach (var user in groupedUsers)
                    {
                        int interactions = user.SelectMany(u => u.UsersLikedIds).Concat(user.SelectMany(u => u.UsersDislikedIds)).Distinct().Count();
                        if (interactions != 0)
                        {
                            decimal thisRating = Math.Abs((decimal)user.Sum(a => a.Points) / interactions);
                            if (thisRating <= rating)
                            {
                                userMention = user.Key;
                                rating = thisRating;
                            }
                        }
                    }

                    return $"{userMention} with a controversial rating of `{Math.Round(rating * 100, 2)}`";
                },
            };

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var stat in statBuilders)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"*{stat.Key}*",
                    Value = stat.Value.Invoke(),
                    IsInline = true,
                });
            }

            return listOfFieldBuilders;
        }
    }
}
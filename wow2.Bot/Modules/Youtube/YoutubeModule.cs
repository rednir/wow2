using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.YouTube.v3.Data;
using wow2.Bot.Data;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.YouTube
{
    [Name("YouTube")]
    [Group("yt")]
    [Alias("youtube")]
    [Summary("Integrations with YouTube, like getting notified for new videos.")]
    public class YouTubeModule : Module
    {
        public IYoutubeModuleService Service { get; set; }

        public YouTubeModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].YouTube;

        [Command("channel")]
        [Alias("user")]
        [Summary("Shows some basic data about a channel.")]
        public async Task ChannelAsync([Name("CHANNEL")][Remainder] string userInput)
        {
            Channel channel;
            try
            {
                channel = await Service.GetChannelAsync(userInput.Trim('\"'));
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, "That channel doesn't exist.");
            }

            await ReplyAsync(embed: await BuildChannelOverviewEmbedAsync(channel));
        }

        [Command("subscribe")]
        [Alias("sub")]
        [Summary("Toggle whether your server will get notified when CHANNEL uploads a new video.")]
        public async Task SubscribeAsync([Name("CHANNEL")][Remainder] string userInput)
        {
            var channel = await Service.GetChannelAsync(userInput.Trim('\"'));

            if (Config.SubscribedChannels.RemoveAll(ch => ch.Id == channel.Id) != 0)
            {
                await new SuccessMessage($"You'll no longer get notifications from `{channel.Snippet.Title}`.")
                    .SendAsync(Context.Channel);
            }
            else
            {
                if (Config.SubscribedChannels.Count > 20)
                    throw new CommandReturnException(Context, "Remove some channels before adding more.", "Too many subscribers");

                Config.SubscribedChannels.Add(new SubscribedChannel()
                {
                    Id = channel.Id,
                    Name = channel.Snippet.Title,
                });
                await new SuccessMessage(Config.AnnouncementsChannelId == 0 ?
                    $"Once you use `set-announcements-channel`, you'll get notifications when {channel.Snippet.Title} uploads a new video." :
                    $"You'll get notifications when `{channel.Snippet.Title}` uploads a new video.")
                        .SendAsync(Context.Channel);
            }
        }

        [Command("list-subs")]
        [Alias("list")]
        [Summary("Lists the channels your server will get notified about.")]
        public async Task ListSubsAsync(int page = 1)
        {
            if (Config.SubscribedChannels.Count == 0)
                throw new CommandReturnException(Context, "Add some channels to the subscriber list first.", "Nothing to show");

            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (SubscribedChannel channel in Config.SubscribedChannels)
            {
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = channel.Name,
                    Value = $"[View channel](https://www.youtube.com/channel/{channel.Id})",
                    IsInline = true,
                });
            }

            await new PagedMessage(
                title: "Subscribed Channels",
                fieldBuilders: fieldBuilders,
                page: page)
                    .SendAsync(Context.Channel);
        }

        [Command("set-announcements-channel")]
        [Alias("announcements-channel", "set-announce-channel", "set-channel")]
        [Summary("Sets the channel where notifications about new videos will be sent.")]
        public async Task SetAnnoucementsChannelAsync(SocketTextChannel channel)
        {
            Config.AnnouncementsChannelId = channel.Id;
            await new SuccessMessage($"You'll get YouTube announcements in {channel.Mention}")
                .SendAsync(Context.Channel);
        }

        [Command("test-poll")]
        [RequireOwner(Group = "Permission")]
        [Summary("Check for new videos.")]
        public async Task TestPollAsync()
        {
            await Service.CheckForNewVideosAsync();
            await new SuccessMessage("Done!")
                .SendAsync(Context.Channel);
        }

        private static async Task NotifyGuildForNewVideoAsync(Video video, SocketTextChannel channel)
        {
            await channel.SendMessageAsync(
                $"**{video.Snippet.ChannelTitle}** just uploaded a new video! Check it out:\nhttps://www.youtube.com/watch?v={video.Id}");
        }

        private async Task<Embed> BuildChannelOverviewEmbedAsync(Channel channel)
        {
            var uploads = await Service.GetChannelUploadsAsync(channel);

            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var upload in uploads)
            {
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = upload.Snippet.Title,
                    Value = $"[{upload.Snippet.PublishedAt.Value:dd MMM yyyy}](https://www.youtube.com/watch?v={upload.Id})",
                    IsInline = true,
                });
            }

            // TODO: truncate sub count in description.
            return new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = channel.Snippet.Title,
                    IconUrl = channel.Snippet.Thumbnails.Default__.Url,
                    Url = "https://www.youtube.com/channel/" + channel.Id,
                },
                Title = "Channel Overview",
                Description = $"{channel.Statistics.SubscriberCount} subscribers • {channel.Statistics.VideoCount} uploads\n",
                Fields = fieldBuilders,
                Color = Color.LightGrey,
            }
            .Build();
        }
    }
}
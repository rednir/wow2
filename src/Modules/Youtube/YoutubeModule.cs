using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Google;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Services;
using wow2.Verbose;
using wow2.Verbose.Messages;
using wow2.Data;

namespace wow2.Modules.YouTube
{
    [Name("YouTube")]
    [Group("yt")]
    [Alias("youtube")]
    [Summary("Integrations with YouTube, like getting notified for new videos.")]
    public class YouTube : ModuleBase<SocketCommandContext>
    {
        private static DateTime TimeOfLastVideoCheck = DateTime.Now;
        private static readonly YouTubeService Service;
        private static readonly Thread YouTubePollingThread = new(async () =>
        {
            const int delayMins = 10;
            while (true)
            {
                try
                {
                    await Task.Delay(delayMins * 60000);
                    await CheckForNewVideosAsync();
                    Logger.Log("Successfully checked for new Youtube videos.", LogSeverity.Debug);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Failure to check for new YouTube videos.");
                    await Task.Delay(delayMins * 60000);
                }
            }
        });

        static YouTube()
        {
            Service = new YouTubeService(new BaseClientService.Initializer()
            {
                // TODO: fix this if no file
                ApiKey = DataManager.Secrets.GoogleApiKey,
                ApplicationName = "wow2"
            });
            YouTubePollingThread.Start();
        }

        [Command("channel")]
        [Summary("Shows some basic data about a channel.")]
        public async Task ChannelAsync([Name("CHANNEL")] params string[] userInputSplit)
        {
            Channel channel;
            try
            {
                channel = await GetChannelAsync(string.Join(' ', userInputSplit));
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
        public async Task SubscribeAsync([Name("CHANNEL")] params string[] userInputSplit)
        {
            var config = GetConfigForGuild(Context.Guild);
            var channel = await GetChannelAsync(string.Join(' ', userInputSplit));

            if (config.SubscribedChannels.RemoveAll(ch => ch.Id == channel.Id) != 0)
            {
                await new SuccessMessage($"You'll no longer get notifications from `{channel.Snippet.Title}`.")
                    .SendAsync(Context.Channel);
            }
            else
            {
                if (config.SubscribedChannels.Count > 20)
                    throw new CommandReturnException(Context, "Remove some channels before adding more.", "Too many subscribers");

                config.SubscribedChannels.Add(new SubscribedChannel()
                {
                    Id = channel.Id,
                    Name = channel.Snippet.Title
                });
                await new SuccessMessage(config.AnnouncementsChannelId == 0 ?
                    $"Once you use `set-announcements-channel`, you'll get notifications when {channel.Snippet.Title} uploads a new video." :
                    $"You'll get notifications when `{channel.Snippet.Title}` uploads a new video.")
                        .SendAsync(Context.Channel);
            }

            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("list-subs")]
        [Alias("list")]
        [Summary("Lists the channels your server will get notified about.")]
        public async Task ListSubsAsync(int page = 1)
        {
            var config = GetConfigForGuild(Context.Guild);

            if (config.SubscribedChannels.Count == 0)
                throw new CommandReturnException(Context, "Add some channels to the subscriber list first.", "Nothing to show");

            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (SubscribedChannel channel in config.SubscribedChannels)
            {
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = channel.Name,
                    Value = $"[View channel](https://www.youtube.com/channel/{channel.Id})",
                    IsInline = true
                });
            }

            await new GenericMessage(
                title: "Subscribed Channels",
                fieldBuilders: fieldBuilders,
                fieldBuildersPage: page)
                    .SendAsync(Context.Channel);
        }

        [Command("set-announcements-channel")]
        [Alias("announcements-channel", "set-announce-channel", "set-channel")]
        [Summary("Sets the channel where notifications about new videos will be sent.")]
        public async Task SetAnnoucementsChannelAsync(SocketTextChannel channel)
        {
            var config = GetConfigForGuild(Context.Guild);

            config.AnnouncementsChannelId = channel.Id;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage($"You'll get YouTube announcements in {channel.Mention}")
                .SendAsync(Context.Channel);
        }

        [Command("test-poll")]
        [RequireOwner(Group = "Permission")]
        [Summary("Check for new videos.")]
        public async Task TestPollAsync()
        {
            await CheckForNewVideosAsync();
            await new SuccessMessage("Done!")
                .SendAsync(Context.Channel);
        }

        private static async Task CheckForNewVideosAsync()
        {
            // Dictionary where the key is the video ID, and the
            // value is a list of ID's of the text channels to notify.
            var newVideosDictionary = new Dictionary<string, List<ulong>>();

            foreach (GuildData guildData in DataManager.DictionaryOfGuildData.Values)
            {
                // Guild hasn't set a announcements channel, so ignore it.
                if (guildData.YouTube.AnnouncementsChannelId == 0) continue;

                var subscribedChannelIds = guildData.YouTube.SubscribedChannels;
                foreach (string id in subscribedChannelIds.Select(c => c.Id))
                {
                    // TODO: proper error handling.
                    var latestUpload = (await GetChannelUploadsAsync(await GetChannelAsync(id), 1))[0];
                    string latestUploadVideoId = latestUpload.ContentDetails.VideoId;

                    if (latestUpload.Snippet.PublishedAt.Value > TimeOfLastVideoCheck)
                    {
                        // Add to dictionary if video is new.
                        newVideosDictionary.TryAdd(latestUploadVideoId, new List<ulong>());
                        newVideosDictionary[latestUploadVideoId].Add(guildData.YouTube.AnnouncementsChannelId);
                    }
                }
            }

            foreach (var pair in newVideosDictionary)
            {
                foreach (ulong channelId in pair.Value)
                {
                    try
                    {
                        await NotifyGuildForNewVideoAsync(
                            video: await GetVideoAsync(pair.Key),
                            channel: (SocketTextChannel)Bot.Client.GetChannel(channelId));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, "Exception thrown when notifying guild for new YouTube video.");
                    }
                }
            }

            TimeOfLastVideoCheck = DateTime.Now;
        }

        private static async Task NotifyGuildForNewVideoAsync(Video video, SocketTextChannel channel)
        {
            await channel.SendMessageAsync(
                $"**{video.Snippet.ChannelTitle}** just uploaded a new video! Check it out:\nhttps://www.youtube.com/watch?v={video.Id}");
        }

        private static async Task<Channel> GetChannelAsync(string channelIdOrUsername)
        {
            var listRequest = Service.Channels.List("snippet, statistics, contentDetails");

            if (!channelIdOrUsername.StartsWith("UC") && channelIdOrUsername.Contains("/UC"))
            {
                // Get channel ID from assumed-to-be channel URL.
                listRequest.Id = Array.Find(
                    channelIdOrUsername.Split('/'), part => part.StartsWith("UC"));
            }
            else if (channelIdOrUsername.StartsWith("UC"))
            {
                // Assume input is channel ID.
                listRequest.Id = channelIdOrUsername;
            }
            else
            {
                // Default to searching for username.
                listRequest.ForUsername = channelIdOrUsername;
            }

            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Items == null)
                throw new ArgumentException("No channels found");

            return listResponse.Items[0];
        }

        private static async Task<IList<PlaylistItem>> GetChannelUploadsAsync(Channel channel, long maxResults = 5)
        {
            var listRequest = Service.PlaylistItems.List("snippet, contentDetails");
            listRequest.PlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads;
            listRequest.MaxResults = maxResults;
            try
            {
                var listResponse = await listRequest.ExecuteAsync();
                return listResponse.Items;
            }
            catch (GoogleApiException)
            {
                // Empty list, assume the channel has no uploads
                return new List<PlaylistItem>();
            }
        }

        private static async Task<Video> GetVideoAsync(string id)
        {
            var listRequest = Service.Videos.List("snippet");
            listRequest.Id = id;
            listRequest.MaxResults = 1;
            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Items.Count == 0)
                throw new ArgumentException("No videos found");

            return listResponse.Items[0];
        }

        private static async Task<Embed> BuildChannelOverviewEmbedAsync(Channel channel)
        {
            var uploads = await GetChannelUploadsAsync(channel);

            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var upload in uploads)
            {
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = upload.Snippet.Title,
                    Value = $"[{upload.Snippet.PublishedAt.Value:dd MMM yyyy}](https://www.youtube.com/watch?v={upload.Id})",
                    IsInline = true
                });
            }

            return new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = channel.Snippet.Title,
                    IconUrl = channel.Snippet.Thumbnails.Default__.Url,
                    Url = "https://www.youtube.com/channel/" + channel.Id
                },
                Title = "Channel Overview",
                // TODO: truncate sub count
                Description = $"{channel.Statistics.SubscriberCount} subscribers • {channel.Statistics.VideoCount} uploads\n",
                Fields = fieldBuilders,
                Color = Color.LightGrey
            }
            .Build();
        }

        public static YouTubeModuleConfig GetConfigForGuild(IGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].YouTube;
    }
}
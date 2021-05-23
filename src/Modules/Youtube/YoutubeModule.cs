using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using wow2.Data;
using wow2.Verbose;
using wow2.Verbose.Messages;
using SearchResult = Google.Apis.YouTube.v3.Data.SearchResult;

namespace wow2.Modules.YouTube
{
    [Name("YouTube")]
    [Group("yt")]
    [Alias("youtube")]
    [Summary("Integrations with YouTube, like getting notified for new videos.")]
    public class YouTubeModule : Module
    {
        private static readonly YouTubeService Service;
        private static DateTime TimeOfLastVideoCheck = DateTime.Now;

        static YouTubeModule()
        {
            Service = new(new BaseClientService.Initializer()
            {
                ApiKey = DataManager.Secrets.GoogleApiKey,
                ApplicationName = "wow2-youtube",
            });
            PollingService.CreateService(CheckForNewVideosAsync, 12);
        }

        public YouTubeModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].YouTube;

        public static async Task<Channel> GetChannelAsync(string channelIdOrUsername)
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

        public static async Task<IList<PlaylistItem>> GetChannelUploadsAsync(Channel channel, long maxResults = 5)
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

        public static async Task<Video> GetVideoAsync(string id)
        {
            var listRequest = Service.Videos.List("snippet, contentDetails, statistics");
            listRequest.Id = id;
            listRequest.MaxResults = 1;
            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Items.Count == 0)
                throw new ArgumentException("No videos found");

            return listResponse.Items[0];
        }

        public static async Task<SearchResult> SearchForAsync(string term, string type)
        {
            var listRequest = Service.Search.List("snippet");
            listRequest.Q = term;
            listRequest.MaxResults = 1;
            listRequest.Type = type;
            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Items.Count == 0)
                throw new ArgumentException("No videos found");

            return listResponse.Items[0];
        }

        [Command("channel")]
        [Alias("user")]
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
            var channel = await GetChannelAsync(string.Join(' ', userInputSplit));

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

            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
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

            foreach (var config in DataManager.AllGuildData.Select(g => g.Value.YouTube))
            {
                // Guild hasn't set a announcements channel, so ignore it.
                if (config.AnnouncementsChannelId == 0)
                    continue;

                var subscribedChannelIds = config.SubscribedChannels;
                foreach (string id in subscribedChannelIds.Select(c => c.Id))
                {
                    // TODO: proper error handling.
                    var uploads = await GetChannelUploadsAsync(await GetChannelAsync(id), 1);
                    if (uploads.Count == 0)
                        continue;

                    var latestUpload = uploads[0];
                    string latestUploadVideoId = latestUpload.ContentDetails.VideoId;
                    if (latestUpload.Snippet.PublishedAt.Value > TimeOfLastVideoCheck)
                    {
                        // Add to dictionary if video is new.
                        newVideosDictionary.TryAdd(latestUploadVideoId, new List<ulong>());
                        newVideosDictionary[latestUploadVideoId].Add(config.AnnouncementsChannelId);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using wow2.Bot.Data;
using wow2.Bot.Verbose;

namespace wow2.Bot.Modules.YouTube
{
    public class YoutubeModuleService : IYoutubeModuleService
    {
        private readonly YouTubeService Service;

        private DateTime TimeOfLastVideoCheck = DateTime.Now;

        public YoutubeModuleService(string apiKey)
        {
            Service = new(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "wow2-youtube",
            });

            if (!string.IsNullOrWhiteSpace(apiKey))
                PollingService.CreateTask(CheckForNewVideosAsync, 15);
        }

        public async Task<Channel> GetChannelAsync(string channelIdOrUsername)
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

        public async Task<IList<PlaylistItem>> GetChannelUploadsAsync(Channel channel, long maxResults)
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

        public async Task<Video> GetVideoAsync(string id)
        {
            var listRequest = Service.Videos.List("snippet, contentDetails, statistics");
            listRequest.Id = id;
            listRequest.MaxResults = 1;
            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Items.Count == 0)
                throw new ArgumentException("No videos found");

            return listResponse.Items[0];
        }

        public async Task<SearchResult> SearchForAsync(string term, string type)
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

        public async Task CheckForNewVideosAsync()
        {
            // Dictionary where the key is the video ID, and the
            // value is a list of ID's of the text channels to notify.
            var newVideosDictionary = new Dictionary<string, List<ulong>>();

            foreach (var config in DataManager.AllGuildData.Select(g => g.Value.YouTube).ToArray())
            {
                // Guild hasn't set a announcements channel, so ignore it.
                if (config.AnnouncementsChannelId == 0)
                    continue;

                var subscribedChannelIds = config.SubscribedChannels;
                foreach (string id in subscribedChannelIds.Select(c => c.Id))
                {
                    // TODO: proper error handling.
                    IList<PlaylistItem> uploads = await GetChannelUploadsAsync(await GetChannelAsync(id), 1);
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
                            channel: (SocketTextChannel)BotService.Client.GetChannel(channelId));
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
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.WebSocket;
using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using wow2.Bot.Data;
using wow2.Bot.Verbose;
using YoutubeExplode;

namespace wow2.Bot.Modules.YouTube
{
    public class YoutubeModuleService : IYoutubeModuleService
    {
        private readonly YouTubeService Service;

        public static YoutubeClient ExplodeClient { get; } = new();

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

            try
            {
                var listResponse = await listRequest.ExecuteAsync();
                if (listResponse.Items == null)
                    throw new ArgumentException("No channels found");

                return listResponse.Items[0];
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                Logger.Log($"Google API request '{ex.ServiceName}' returned 403, probably because your quota has been exceeded. Falling back to alternative library...", LogSeverity.Verbose);

                YoutubeExplode.Channels.Channel channel;
                if (listRequest.Id != null)
                {
                    channel = await ExplodeClient.Channels.GetAsync(listRequest.Id.ToString());
                }
                else
                {
                    channel = await ExplodeClient.Channels.GetByUserAsync(listRequest.ForUsername);
                }

                return new Channel()
                {
                    Id = channel.Id,
                    Snippet = new ChannelSnippet()
                    {
                        Title = channel.Title,
                        Thumbnails = ExplodeToGoogleThumbnails(channel.Thumbnails),
                    },
                };
            }
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
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                // Assume the channel has no uploaded videos.
                return new List<PlaylistItem>();
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                Logger.Log($"Google API request '{ex.ServiceName}' returned 403, probably because your quota has been exceeded. Falling back to alternative library...", LogSeverity.Verbose);

                var playlist = ExplodeClient.Channels.GetUploadsAsync(channel.Id);
                var result = new List<PlaylistItem>();
                await foreach (var video in playlist)
                {
                    if (result.Count >= maxResults)
                        break;

                    result.Add(new PlaylistItem()
                    {
                        Id = video.Id,
                        Snippet = new PlaylistItemSnippet()
                        {
                            Title = video.Title,
                            ChannelId = video.Author.ChannelId,
                            ChannelTitle = video.Author.Title,
                            Thumbnails = ExplodeToGoogleThumbnails(video.Thumbnails),
                        },
                    });
                }

                return result;
            }
        }

        public async Task<Video> GetVideoAsync(string id)
        {
            try
            {
                var listRequest = Service.Videos.List("snippet, contentDetails, statistics");
                listRequest.Id = id;
                listRequest.MaxResults = 1;
                var listResponse = await listRequest.ExecuteAsync();

                if (listResponse.Items.Count == 0)
                    throw new ArgumentException("No videos found");

                return listResponse.Items[0];
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                Logger.Log($"Google API request '{ex.ServiceName}' returned 403, probably because your quota has been exceeded. Falling back to alternative library...", LogSeverity.Verbose);

                var video = await ExplodeClient.Videos.GetAsync(id);
                return new Video()
                {
                    Id = video.Id,
                    ContentDetails = new VideoContentDetails()
                    {
                        Duration = video.Duration.HasValue ? $"PT{video.Duration.Value.Minutes}M{video.Duration.Value.Seconds}S" : null,
                    },
                    Statistics = new VideoStatistics()
                    {
                        LikeCount = (ulong)video.Engagement.LikeCount,
                        DislikeCount = (ulong)video.Engagement.DislikeCount,
                        ViewCount = (ulong)video.Engagement.ViewCount,
                    },
                    Snippet = new VideoSnippet()
                    {
                        Title = video.Title,
                        ChannelTitle = video.Author.Title,
                        ChannelId = video.Author.ChannelId,
                        Description = video.Description,
                        PublishedAt = video.UploadDate.DateTime,
                        Thumbnails = ExplodeToGoogleThumbnails(video.Thumbnails),
                    },
                };
            }
        }

        public async Task<SearchResult> SearchForAsync(string term, string type)
        {
            try
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
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                Logger.Log($"Google API request '{ex.ServiceName}' returned 403, probably because your quota has been exceeded. Falling back to alternative library...", LogSeverity.Verbose);

                switch (type)
                {
                    case "video":
                        var searchCollection = ExplodeClient.Search.GetVideosAsync(term);
                        var result = await searchCollection.FirstAsync();
                        return new SearchResult()
                        {
                            Kind = "youtube#searchListResponse",
                            Id = new ResourceId()
                            {
                                VideoId = result.Id,
                            },
                            Snippet = new SearchResultSnippet()
                            {
                                Title = result.Title,
                                ChannelTitle = result.Author.Title,
                                ChannelId = result.Author.ChannelId,
                                Thumbnails = ExplodeToGoogleThumbnails(result.Thumbnails),
                            },
                        };

                    default:
                        throw new NotImplementedException($"The search type {type} is not implemented for fallback.");
                }
            }
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

        private static ThumbnailDetails ExplodeToGoogleThumbnails(IReadOnlyList<YoutubeExplode.Common.Thumbnail> thumbnails)
        {
            return thumbnails.Count == 0 ? new ThumbnailDetails() : new ThumbnailDetails()
            {
                Default__ = new Thumbnail() { Url = thumbnails[0].Url, Width = thumbnails[0].Resolution.Width, Height = thumbnails[0].Resolution.Height },
                High = new Thumbnail() { Url = thumbnails[0].Url, Width = thumbnails[0].Resolution.Width, Height = thumbnails[0].Resolution.Height },
                Medium = new Thumbnail() { Url = thumbnails[0].Url, Width = thumbnails[0].Resolution.Width, Height = thumbnails[0].Resolution.Height },
                Standard = new Thumbnail() { Url = thumbnails[0].Url, Width = thumbnails[0].Resolution.Width, Height = thumbnails[0].Resolution.Height },
                Maxres = new Thumbnail() { Url = thumbnails[thumbnails.Count - 1].Url, Width = thumbnails[thumbnails.Count - 1].Resolution.Width, Height = thumbnails[thumbnails.Count - 1].Resolution.Height },
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Services;
using wow2.Verbose;
using wow2.Verbose.Messages;
using wow2.Data;

namespace wow2.Modules.Youtube
{
    [Name("Youtube")]
    [Group("yt")]
    [Alias("youtube")]
    [Summary("Get notified on new slkd;fjsa;lkdfsj")]
    public class Youtube : ModuleBase<SocketCommandContext>
    {
        private static YouTubeService Service;
        private static DateTime TimeOfLastVideoCheck = DateTime.Now;

        public Youtube()
        {
            Service = new YouTubeService(new BaseClientService.Initializer()
            {
                // TODO: fix this if no file
                ApiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ?? File.ReadAllText("google.key"),
                ApplicationName = "wow2"
            });
        }

        [Command("channel")]
        public async Task ChannelAsync([Name("CHANNEL")] string userInput)
        {
            Channel channel;
            try
            {
                channel = await GetChannelAsync(userInput);
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, "That channel doesn't exist.");
            }

            await ReplyAsync(embed: await BuildChannelOverviewEmbedAsync(channel));
        }

        [Command("subscribe")]
        public async Task SubscribeAsync([Name("CHANNEL")] string userInput)
        {
            var config = GetConfigForGuild(Context.Guild);

            var channel = await GetChannelAsync(userInput);
            config.SubscribedChannelIds.Add(channel.Id);
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage(config.AnnouncementsChannelId == 0 ? 
                $"Once you use `set-announcements-channel`, you'll get notifications when {channel.Snippet.Title} uploads a new video." :
                $"You'll get notifications when {channel.Snippet.Title} uploads a new video.")
                    .SendAsync(Context.Channel);
        }

        [Command("set-announcements-channel")]
        [Alias("announcements-channel", "set-announce-channel", "set-channel")]
        public async Task SetAnnoucementsChannel(SocketTextChannel channel)
        {
            var config = GetConfigForGuild(Context.Guild);

            config.AnnouncementsChannelId = channel.Id;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage($"You'll get Youtube announcements in {channel.Mention}")
                .SendAsync(Context.Channel);
        }

        private async Task CheckForNewVideos()
        {
            foreach (GuildData guild in DataManager.DictionaryOfGuildData.Values)
            {
                var subscribedChannelIds = guild.Youtube.SubscribedChannelIds;
                foreach (string id in subscribedChannelIds)
                {
                    // ERROR handling!!!!!!!!!!!!
                    var uploads = await GetChannelUploadsAsync(await GetChannelAsync(id), 1);
                    if (uploads[0].Snippet.PublishedAt.Value > TimeOfLastVideoCheck)
                    {
                        await NotifyGuildForNewVideo(
                            video: await GetVideoAsync(uploads[0].ContentDetails.VideoId),
                            channel: (SocketTextChannel)Program.Client.GetChannel(guild.Youtube.AnnouncementsChannelId));
                    }
                }
            }
            TimeOfLastVideoCheck = DateTime.Now;
        }

        private static async Task NotifyGuildForNewVideo(Video video, SocketTextChannel channel)
        {
            await new InfoMessage($"{video.Snippet.Title}")
                .SendAsync(channel);
        }

        private static async Task<Channel> GetChannelAsync(string channelIdOrUsername)
        {
            var listRequest = Service.Channels.List("snippet, statistics, contentDetails");

            if (!channelIdOrUsername.StartsWith("UC") && channelIdOrUsername.Contains("/UC"))
            {
                // Get channel ID from assumed-to-be channel URL.
                listRequest.Id = channelIdOrUsername.Split('/')
                    .Where(part => part.StartsWith("UC")).FirstOrDefault();
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
            // TODO: throw some exception if channel has no uploads
            listRequest.PlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads;
            listRequest.MaxResults = maxResults;
            var listResponse = await listRequest.ExecuteAsync();
            
            return listResponse.Items;
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
            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var upload in await GetChannelUploadsAsync(channel))
            {
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = upload.Snippet.Title,
                    Value = $"[{upload.Snippet.PublishedAt.Value.ToString("dd MMM yyyy")}](https://www.youtube.com/watch?v={upload.Id})",
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

        public static YoutubeModuleConfig GetConfigForGuild(IGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Youtube;
    }
}
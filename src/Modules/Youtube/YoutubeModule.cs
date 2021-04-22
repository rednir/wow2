using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.Commands;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Services;
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

        public Youtube()
        {
            Service = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ?? File.ReadAllText("google.key"),
                ApplicationName = "wow2"
            });
        }

        [Command("channel")]
        public async Task ChannelAsync([Name("CHANNEL")] string userInput)
        {
            // Youtube channel IDs always start with UC.

            string channelToFetchId = null;

            if (!userInput.StartsWith("UC") && userInput.Contains("/UC"))
            {
                // Get channel ID from assumed-to-be channel URL.
                channelToFetchId = userInput.Split('/')
                    .Where(part => part.StartsWith("UC")).FirstOrDefault();
            }
            else if (userInput.StartsWith("UC"))
            {
                channelToFetchId = userInput;
            }

            Channel channel;
            try
            {
                // Only pass in userInput if a channel ID was not found.
                channel = await GetChannelAsync(
                    channelToFetchId, channelToFetchId == null ? userInput : null);
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, "That channel doesn't exist.");
            }

            await ReplyAsync(embed: await BuildChannelOverviewEmbedAsync(channel));
        }

        private static async Task<Channel> GetChannelAsync(string id = null, string username = null)
        {
            var listRequest = Service.Channels.List("snippet, statistics, contentDetails");
            listRequest.Id = id;
            listRequest.ForUsername = username;
            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Items == null)
                throw new ArgumentException("No channels found");

            return listResponse.Items[0];
        }

        private static async Task<IList<PlaylistItem>> GetChannelUploadsAsync(Channel channel)
        {
            var listRequest = Service.PlaylistItems.List("snippet");
            listRequest.PlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads;
            var listResponse = await listRequest.ExecuteAsync();

            return listResponse.Items;
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
    }
}
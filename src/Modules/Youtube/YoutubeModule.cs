using System;
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
        public async Task Channel([Name("CHANNEL")] string userInput)
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
                channel = await GetChannel(
                    channelToFetchId, channelToFetchId == null ? userInput : null);
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, "That channel doesn't exist.");
            }

            await ReplyAsync(embed: BuildChannelOverviewEmbed(channel));
        }

        private static async Task<Channel> GetChannel(string id = null, string username = null)
        {
            var listRequest = Service.Channels.List("snippet, statistics, contentDetails");
            listRequest.Id = id;
            listRequest.ForUsername = username;
            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Items == null)
                throw new ArgumentException("No channels found");

            return listResponse.Items[0];
        }

        private static Embed BuildChannelOverviewEmbed(Channel channel)
        {
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
                Description = $"{channel.Statistics.SubscriberCount} subscribers   |   {channel.Statistics.VideoCount} uploads",
                Color = Color.LightGrey
            }
            .Build();
        }
    }
}
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
        [Command("channel")]
        public async Task Channel([Name("CHANNEL")] string channelId)
        {
            if (!channelId.Contains("UC"))
                throw new CommandReturnException(Context, "Couldn't find a channel ID.");

            if (!channelId.StartsWith("UC"))
            {
                // Get channel ID from assumed-to-be channel URL.
                channelId = channelId.Split('/')
                    .Where(part => part.StartsWith("UC")).FirstOrDefault();

                if (string.IsNullOrEmpty(channelId))
                    throw new CommandReturnException(Context, "Couldn't find a channel ID.");
            }

            Channel channel;
            try
            {
                channel = await GetChannel(channelId.Split('/')[0]);
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, "That channel doesn't exist.");
            }

            await ReplyAsync(embed: BuildChannelOverviewEmbed(channel));
        }

        private static async Task<Channel> GetChannel(string id)
        {
            var service = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ?? await File.ReadAllTextAsync("google.key"),
                ApplicationName = Program.ApplicationInfo.Name
            });

            var listRequest = service.Channels.List("snippet, statistics");
            listRequest.Id = id;
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
                Description = $"{channel.Statistics.SubscriberCount} subscribers   |   {channel.Statistics.VideoCount} uploads",
                Color = Color.LightGrey
            }
            .Build();
        }
    }
}
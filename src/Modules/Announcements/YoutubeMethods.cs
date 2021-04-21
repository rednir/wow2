using System;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.Commands;
using wow2.Data;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Services;

namespace wow2.Modules.Announcements
{
    public static class YoutubeMethods
    {
        public static async Task GetChannelStatistics(string id)
        {
            // TODO: update readme, env variable, error message
            var service = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = await File.ReadAllTextAsync("google.key"),
                ApplicationName = Program.ApplicationInfo.Name
            });

            var listRequest = service.Channels.List("statistics");
            listRequest.Id = id;
            var listResponse = await listRequest.ExecuteAsync();
        }
    }
}
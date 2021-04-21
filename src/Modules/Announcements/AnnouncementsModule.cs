using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Verbose.Messages;
using wow2.Data;

namespace wow2.Modules.Announcements
{
    [Name("Announcements")]
    [Group("announce")]
    [Summary("Get notified on new slkd;fjsa;lkdfsj")]
    public class AnnouncementsModule : ModuleBase<SocketCommandContext>
    {
        [Command("youtube")]
        [Alias("yt")]
        public async Task Youtube()
        {
            throw new NotImplementedException();
            await YoutubeMethods.GetChannelStatistics("");
        }
    }
}
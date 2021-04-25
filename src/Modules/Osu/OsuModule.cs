using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using wow2.Verbose.Messages;
using wow2.Data;

namespace wow2.Modules.Osu
{
    [Name("osu!")]
    [Group("osu")]
    [Summary("Integrations with the osu!api")]
    public class OsuModule : ModuleBase<SocketCommandContext>
    {
        public HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://osu.ppy.sh/api/v2")
        };

        [Command("subscribe")]
        [Alias("sub")]
        [Summary("Toggle whether your server will get notified about USER.")]
        public async Task SubscribeAsync(string user)
        {
            throw new NotImplementedException();
        }
    }
}
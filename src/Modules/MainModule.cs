using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ExtentionMethods;

namespace wow2.Modules
{
    public class MainModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync(embed: MessageEmbedPresets.GenericResponse("", "Help"));
        }
    }
}
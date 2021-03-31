using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;

namespace wow2.Modules.Moderator
{
    [Name("Moderator")]
    [Group("mod")]
    [Alias("moderator")]
    [Summary("For using tools to manage the server.")]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        [Command("mute")]
        [Alias("silence")]
        [Summary("Temporarily disables a user's permission to speak")]
        public async Task MuteAsync(SocketUser userToMute)
        {
        }
    }
}
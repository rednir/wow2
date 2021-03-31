using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Verbose;

namespace wow2.Modules.Moderator
{
    [Name("Moderator")]
    [Group("mod")]
    [Alias("moderator")]
    [Summary("For using tools to manage the server.")]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        [Command("warn")]
        [Summary("Sends a user a DM with an optional warning message. Requires the 'Kick Members' permission.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task WarnAsync(SocketGuildUser user, string message = "No reason given")
        {
            IDMChannel dmChannel = await user.GetOrCreateDMChannelAsync();
            await GenericMessenger.SendInfoAsync(dmChannel, "Warniing");
        }

        [Command("mute")]
        [Alias("silence")]
        [Summary("Temporarily disables a user's permission to speak. Requires the 'Kick Members' permission.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task MuteAsync(SocketUser user, string time, string message = "No reason given.")
        {
            throw new NotImplementedException(); 
        }
    }
}
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
        [Summary("Sends a warning to a user with an optional message. Requires the 'Kick Members' permission.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task WarnAsync(SocketGuildUser user, [Name("MESSAGE")] params string[] messageSplit)
        {
            string message = messageSplit.Length == 0 ? 
                "No reason was provided by the moderator." : $"Reason: {string.Join(' ', messageSplit)}";

            IDMChannel dmChannel = await user.GetOrCreateDMChannelAsync();
            await GenericMessenger.SendWarningAsync(dmChannel, $"You have recieved a warning from {Context.User.Mention} in the server '{Context.Guild.Name}'\nFurther warnings may result in a ban.\n```\n{message}\n```", "You have been warned!");
        }

        [Command("mute")]
        [Alias("silence", "timeout")]
        [Summary("Temporarily disables a user's permission to speak. Requires the 'Kick Members' permission.")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task MuteAsync(SocketUser user, string time = "30m", string message = "No reason given.")
        {
            throw new NotImplementedException(); 
        }
    }
}
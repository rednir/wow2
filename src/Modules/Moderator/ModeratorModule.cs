using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Verbose;
using wow2.Data;

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
            var config = DataManager.GetModeratorConfigForGuild(Context.Guild);

            string message = messageSplit.Length == 0 ? 
                "No reason was provided by the moderator." : $"Reason: {string.Join(' ', messageSplit)}";

            GetUserRecord(config, user.Id).Warnings.Add(new Warning()
            {
                RequestedBy = Context.User.Id,
                DateTimeBinary = DateTime.Now.ToBinary()
            });
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

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

        private UserRecord GetUserRecord(ModeratorModuleConfig config, ulong id)
        {
            UserRecord matchingRecord = config.UserRecords
                .Where(record => record.UserId == id)
                .FirstOrDefault();

            if (matchingRecord == null)
            {
                config.UserRecords.Add(new UserRecord()
                {
                    UserId = id
                });

                // Could potentially be unsafe?
                matchingRecord = config.UserRecords.Last();
            }
            return matchingRecord;
        }
        
    }
}
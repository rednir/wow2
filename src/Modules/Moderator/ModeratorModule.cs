using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;
using wow2.Verbose.Messages;
using wow2.Data;
using wow2.Extentions;

namespace wow2.Modules.Moderator
{
    [Name("Moderator")]
    [Group("mod")]
    [Alias("moderator")]
    [Summary("For using tools to manage the server. This is still very rudimentary and unfinished.  Requires the 'Ban Members' permission.")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        public static async Task CheckMessageWithAutoMod(SocketMessage message)
        {
            var config = GetConfigForGuild(message.GetGuild());

            if (!config.IsAutoModOn) return;

            UserRecord record = GetUserRecord(config, message.Author.Id);
            record.Messages.Add(message);

            string dueTo;
            string warningMessage;
            if (CheckMessagesForSpam(record.Messages))
            {
                warningMessage = "Recent messages were automatically deemed to be spam.";
                dueTo = "spam";
            }
            else if (CheckMessagesForRepeatedContent(record.Messages))
            {
                warningMessage = "Recent messages contained repeated content.";
                dueTo = "repeated messages";
            }
            else
            {
                // No need to act on the messages.
                return;
            }

            // Don't auto warn the user too many times in a short time period.
            if (record.Warnings.Count() > 0)
            {
                var lastWarningTime = DateTime.FromBinary(record.Warnings.Last().DateTimeBinary);
                if (DateTime.Now - lastWarningTime < TimeSpan.FromSeconds(20))
                    return;
            }

            await WarnOrBanUserAsync(
                config: config,
                victim: (SocketGuildUser)message.Author,
                requestedBy: await Program.GetClientGuildUserAsync(message.Channel),
                message: warningMessage);

            await new InfoMessage($"{message.Author.Mention} has been warned due to {dueTo}.")
                .SendAsync(message.Channel);
        }

        [Command("warn")]
        [Summary("Sends a warning to a user with an optional message.")]
        public async Task WarnAsync([Name("MENTION")] SocketGuildUser user, [Name("MESSAGE")][Remainder] string message = null)
        {
            var config = GetConfigForGuild(Context.Guild);

            await WarnOrBanUserAsync(config, user, (SocketGuildUser)Context.User, message);

            await new SuccessMessage($"The user {user.Mention} has been warned by {Context.User.Mention}.")
                .SendAsync(Context.Channel);
        }

        [Command("mute")]
        [Alias("silence", "timeout")]
        [Summary("Temporarily disables a user's permission to speak. (WIP)")]
        public async Task MuteAsync([Name("MENTION")] SocketGuildUser user, string time = "30m", string message = "No reason given.")
        {
            await new WarningMessage("This hasn't been implemented yet. Check back later!")
                .SendAsync(Context.Channel);
        }

        [Command("user-record")]
        [Alias("user", "record", "overview")]
        [Summary("Gets a user record.")]
        public async Task UserAsync([Name("MENTION")] SocketGuildUser user)
        {
            var config = GetConfigForGuild(Context.Guild);
            UserRecord record = GetUserRecord(config, user.Id);

            var embedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = user.ToString(),
                    IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
                },
                Title = "User record overview",
                Description = $"{record.Warnings.Count()} warnings, {record.Mutes.Count()} mutes.",
                Color = Color.LightGrey
            };

            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("set-warnings-until-ban")]
        [Summary("Sets the number of warnings required before a user is automatically banned. Set NUMBER to -1 to disable this.")]
        public async Task SetWarningsUntilBan(int number)
        {
            var config = GetConfigForGuild(Context.Guild);
            if (number == -1)
            {
                config.WarningsUntilBan = number;
                await new SuccessMessage($"A user will not get automatically banned from too many warnings.")
                    .SendAsync(Context.Channel);
            }
            else
            {
                if (number < 2)
                    throw new CommandReturnException(Context, "Number is too small.");

                config.WarningsUntilBan = number;
                await new SuccessMessage($"{number} warnings will result in a ban.")
                    .SendAsync(Context.Channel);
            }

            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("toggle-auto-mod")]
        [Summary("Toggles whether the bot give warnings to users, for example if spam is detected.")]
        public async Task ToggleAutoMod()
        {
            var config = GetConfigForGuild(Context.Guild);

            config.IsAutoModOn = !config.IsAutoModOn;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Auto mod is now `{(config.IsAutoModOn ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        private static async Task WarnOrBanUserAsync(ModeratorModuleConfig config, SocketGuildUser victim, SocketGuildUser requestedBy, string message)
        {
            var userRecord = GetUserRecord(config, victim.Id);
            message = string.IsNullOrWhiteSpace(message) ?
                "No reason was provided by the moderator." : $"Reason: {message}";

            userRecord.Warnings.Add(new Warning()
            {
                RequestedBy = requestedBy.Id,
                DateTimeBinary = DateTime.Now.ToBinary()
            });

            await DataManager.SaveGuildDataToFileAsync(requestedBy.Guild.Id);

            IDMChannel dmChannel = await victim.GetOrCreateDMChannelAsync();
            if (userRecord.Warnings.Count >= config.WarningsUntilBan &&
                config.WarningsUntilBan != -1)
            {
                await victim.BanAsync(1, message);
                await new WarningMessage(
                    description: $"You have recieved a warning from {requestedBy.Mention} in the server '{requestedBy.Guild.Name}'\nDue to the number of warnings you have recieved from this server, you have been permanently banned.\n```\n{message}\n```",
                    title: "You have been banned!")
                        .SendAsync(dmChannel);
            }
            else
            {
                await new WarningMessage(
                    description: $"You have recieved a warning from {requestedBy.Mention} in the server '{requestedBy.Guild.Name}'\nFurther warnings may result in a ban.\n```\n{message}\n```",
                    title: "You have been warned!")
                        .SendAsync(dmChannel);
            }
        }

        private static UserRecord GetUserRecord(ModeratorModuleConfig config, ulong id)
        {
            UserRecord matchingRecord = config.UserRecords
                .Where(record => record.UserId == id)
                .FirstOrDefault();

            // Ensure the user record exists
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

        private static bool CheckMessagesForSpam(IEnumerable<SocketMessage> messages)
        {
            const int numberOfMessagesToCheckForSpam = 7;

            // Order the list with newest messages first.
            messages = messages.OrderByDescending(message => message.Timestamp);

            if (messages.Count() > numberOfMessagesToCheckForSpam)
            {
                var timeSpan = messages.First().Timestamp - messages.ElementAt(numberOfMessagesToCheckForSpam).Timestamp;
                if (timeSpan < TimeSpan.FromSeconds(12))
                    return true;
            }

            return false;
        }

        private static bool CheckMessagesForRepeatedContent(IEnumerable<SocketMessage> messages)
        {
            const int numberOfMessagesToCheck = 4;

            if (messages.Count() < numberOfMessagesToCheck)
                return false;

            // Order the list with newest messages first, and get subsection of list.
            messages = messages.OrderByDescending(message => message.Timestamp)
                .ToList().GetRange(0, numberOfMessagesToCheck);

            return messages.All(m => m.Content == messages.First().Content);
        }

        private static ModeratorModuleConfig GetConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Moderator;
    }
}
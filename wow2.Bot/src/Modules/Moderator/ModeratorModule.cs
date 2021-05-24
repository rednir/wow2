using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Moderator
{
    [Name("Moderator")]
    [Group("mod")]
    [Alias("moderator")]
    [Summary("Use tools to manage the server. This is still very rudimentary and unfinished.  Requires the 'Ban Members' permission.")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public class ModeratorModule : Module
    {
        public ModeratorModule(BotService botService)
            : base(botService)
        {
        }

        public ModeratorModuleConfig Config => BotService.Data.AllGuildData[Context.Guild.Id].Moderator;

        public static async Task CheckMessageWithAutoMod(SocketCommandContext context, BotService botService)
        {
            var config = botService.Data.AllGuildData[context.Guild.Id].Moderator;

            UserRecord record;
            try
            {
                record = GetUserRecord(config, botService, context.User.Id);
                record.Messages.Add(context.Message);
            }
            catch (ArgumentException)
            {
                // Message was from bot.
                return;
            }

            // Conserve memory.
            record.Messages.Truncate(15);

            if (!config.IsAutoModOn)
                return;

            string dueTo;
            string warningMessage;
            if (AutoModMethods.CheckMessagesForSpam(record.Messages))
            {
                warningMessage = "Recent messages were automatically deemed to be spam.";
                dueTo = "spam";
            }
            else if (AutoModMethods.CheckMessagesForRepeatedContent(record.Messages))
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
            if (record.Warnings.Count > 0)
            {
                var lastWarningTime = DateTime.FromBinary(record.Warnings.Last().DateTimeBinary);
                if (DateTime.Now - lastWarningTime < TimeSpan.FromSeconds(20))
                    return;
            }

            await WarnOrBanUserAsync(
                config: config,
                botService: botService,
                victim: (SocketGuildUser)context.User,
                requestedBy: await botService.GetClientGuildUserAsync(context.Channel),
                message: warningMessage);

            await new InfoMessage($"{context.User.Mention} has been warned due to {dueTo}.")
                .SendAsync(context.Channel);
        }

        /// <summary>Checks whether a user is abusing commands.</summary>
        /// <returns>True if the user should be rate limited due to command abuse.</summary>
        public static bool CheckForCommandAbuse(SocketCommandContext context, BotService botService)
        {
            const int numOfCommandsToCheck = 12;

            UserRecord record = GetUserRecord(
                config: botService.Data.AllGuildData[context.Guild.Id].Moderator,
                botService: botService,
                id: context.Message.Author.Id);

            record.CommandExecutedDateTimes.Add(context.Message.Timestamp);

            if (record.CommandExecutedDateTimes.Count < numOfCommandsToCheck)
                return false;

            record.CommandExecutedDateTimes.Truncate(numOfCommandsToCheck);

            // Represents the time difference between a set number of commands (numOfCommandsToCheck).
            TimeSpan difference = context.Message.Timestamp.Subtract(record.CommandExecutedDateTimes[0]);
            return difference < TimeSpan.FromSeconds(30);
        }

        [Command("warn")]
        [Summary("Sends a warning to a user with an optional message.")]
        public async Task WarnAsync([Name("MENTION")] SocketGuildUser user, [Name("MESSAGE")][Remainder] string message = null)
        {
            try
            {
                await WarnOrBanUserAsync(
                    config: Config,
                    botService: BotService,
                    victim: user,
                    requestedBy: (SocketGuildUser)Context.User,
                    message: message);
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, "You can't warn a bot! Bots do no wrong.");
            }

            await new SuccessMessage($"The user {user.Mention} has been warned by {Context.User.Mention}.")
                .SendAsync(Context.Channel);
        }

        [Command("mute")]
        [Alias("silence", "timeout")]
        [Summary("Temporarily disables a user's permission to speak. (WIP)")]
        public Task MuteAsync([Name("MENTION")] SocketGuildUser user, string time = "30m", string message = "No reason given.")
        {
            throw new NotImplementedException();
        }

        [Command("user-record")]
        [Alias("user", "record", "overview")]
        [Summary("Gets a user record.")]
        public async Task UserAsync([Name("MENTION")] SocketGuildUser user)
        {
            UserRecord record;
            try
            {
                record = GetUserRecord(Config, BotService, user.Id);
            }
            catch (ArgumentException)
            {
                throw new CommandReturnException(Context, "Bots don't have records!");
            }

            await new UserRecordMessage(user, record)
                .SendAsync(Context.Channel);
        }

        [Command("set-warnings-until-ban")]
        [Summary("Sets the number of warnings required before a user is automatically banned. Set NUMBER to -1 to disable this.")]
        public async Task SetWarningsUntilBan(int number)
        {
            if (number == -1)
            {
                Config.WarningsUntilBan = number;
                await new SuccessMessage("A user will no longer get automatically banned from too many warnings.")
                    .SendAsync(Context.Channel);
            }
            else
            {
                if (number < 2)
                    throw new CommandReturnException(Context, "Number is too small.");

                Config.WarningsUntilBan = number;
                await new SuccessMessage($"{number} warnings will result in a ban.")
                    .SendAsync(Context.Channel);
            }

            await BotService.Data.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("toggle-auto-mod")]
        [Summary("Toggles whether the bot give warnings to users, for example if spam is detected.")]
        public async Task ToggleAutoMod()
        {
            Config.IsAutoModOn = !Config.IsAutoModOn;
            await BotService.Data.SaveGuildDataToFileAsync(Context.Guild.Id);
            await new SuccessMessage($"Auto mod is now `{(Config.IsAutoModOn ? "on" : "off")}`")
                .SendAsync(Context.Channel);
        }

        private static async Task WarnOrBanUserAsync(ModeratorModuleConfig config, BotService botService, SocketGuildUser victim, SocketGuildUser requestedBy, string message)
        {
            var userRecord = GetUserRecord(config, botService, victim.Id);
            message = string.IsNullOrWhiteSpace(message) ?
                "No reason was provided by the moderator." : $"Reason: {message}";

            userRecord.Warnings.Add(new Warning()
            {
                RequestedBy = requestedBy.Id,
                DateTimeBinary = DateTime.Now.ToBinary(),
            });

            await botService.Data.SaveGuildDataToFileAsync(requestedBy.Guild.Id);

            IDMChannel dmChannel = await victim.GetOrCreateDMChannelAsync();
            if (userRecord.Warnings.Count >= config.WarningsUntilBan &&
                config.WarningsUntilBan != -1)
            {
                try
                {
                    await victim.BanAsync(1, message);
                    await new WarningMessage(
                    description: $"You have recieved a warning from {requestedBy.Mention} in the server '{requestedBy.Guild.Name}'\nDue to the number of warnings you have recieved from this server, you have been permanently banned.\n```\n{message}\n```",
                    title: "You have been banned!")
                        .SendAsync(dmChannel);

                    return;
                }
                catch (HttpException)
                {
                    // User is most likely admin, so just give another warning.
                }
            }

            await new WarningMessage(
                description: $"You have recieved a warning from {requestedBy.Mention} in the server '{requestedBy.Guild.Name}'\nFurther warnings may result in a ban.\n```\n{message}\n```",
                title: "You have been warned!")
                    .SendAsync(dmChannel);
        }

        private static UserRecord GetUserRecord(ModeratorModuleConfig config, BotService botService, ulong id)
        {
            SocketUser user = botService.Client.GetUser(id);
            if (user == null)
                throw new ArgumentException("User was not found.");
            if (user.IsBot)
                throw new ArgumentException("Cannot get user record for bot.");

            UserRecord matchingRecord = config.UserRecords
                .Find(record => record.UserId == id);

            // Ensure the user record exists
            if (matchingRecord == null)
            {
                config.UserRecords.Add(new UserRecord()
                {
                    UserId = id,
                });

                // Could potentially be unsafe?
                matchingRecord = config.UserRecords.Last();
            }

            return matchingRecord;
        }
    }
}
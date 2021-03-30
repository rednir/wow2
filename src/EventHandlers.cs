using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Verbose;
using wow2.Modules;
using wow2.Modules.Main;
using wow2.Modules.Keywords;
using wow2.Modules.Games;
using wow2.Extentions;
using wow2.Data;

namespace wow2
{
    public static class EventHandlers
    {
        public static readonly string DefaultCommandPrefix = "!wow";
        public static CommandService BotCommandService;

        public static async Task InstallCommandsAsync()
        {
            var config = new CommandServiceConfig()
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async
            };
            BotCommandService = new CommandService(config);
            BotCommandService.Log += DiscordLogRecievedAsync;
            await BotCommandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        public static async Task ReadyAsync()
        {
            await Program.Client.SetGameAsync($"{DefaultCommandPrefix} help");
        }

        public static async Task JoinedGuildAsync(SocketGuild guild)
        {
            var embedBuilder = new EmbedBuilder()
            {
                Title = "👋 Hi there!",
                Description = $"Thanks for adding me to your server!\nTo get started, type `{EventHandlers.DefaultCommandPrefix} help` to see the wide range of commands available.\n",
                Color = Color.Gold
            };
            await guild.DefaultChannel.SendMessageAsync(embed: embedBuilder.Build());
        }

        public static async Task DiscordLogRecievedAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is CommandException commandException)
            {
                string verboseErrorMessage = $"An unhandled exception was thrown and was automatically reported.\n`{commandException.InnerException.Message}`";

                if (commandException.InnerException is CommandReturnException)
                    return;
                else if (commandException.InnerException is DirectoryNotFoundException || commandException.InnerException is FileNotFoundException)
                    verboseErrorMessage = "The host of the bot is missing required assets.";

                Logger.LogException(commandException, $"Command '{commandException.Command.Name}' threw an exception in guild '{commandException.Context.Guild.Name}' due to message '{commandException.Context.Message.Content}'");
                await GenericMessenger.SendErrorAsync((ISocketMessageChannel)commandException.Context.Channel, verboseErrorMessage);

                // TODO: consider making this a toggle.
                await (await Program.Client.GetApplicationInfoAsync()).Owner.SendMessageAsync($"```\n{commandException}\n```");
            }
            else if (logMessage.Exception != null)
            {
                await (await Program.Client.GetApplicationInfoAsync()).Owner.SendMessageAsync($"```\n{logMessage.Exception}\n```");
                Logger.LogException(logMessage.Exception);
            }
            else
            {
                Logger.Log(logMessage);
            }
        }

        public static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == Program.Client.CurrentUser.Id)
                return;

            IUserMessage message = await cachedMessage.GetOrDownloadAsync();

            if (reaction.Emote.Name == KeywordsModule.DeleteReactionEmote.Name)
            {
                if (await KeywordsModule.DeleteMessageIfKeywordResponse(message))
                {
                    Logger.Log($"Message was deleted in channel `{channel.Name}` due to reaction added by `{reaction.User}` ({reaction.UserId})", LogSeverity.Verbose);
                    return;
                }
            }
        }

        public static async Task MessageRecievedAsync(SocketMessage recievedMessage)
        {
            if (recievedMessage.Author.Id == Program.Client.CurrentUser.Id) return;

            await DataManager.EnsureGuildDataFileExistsAsync(recievedMessage.GetGuild().Id);

            await GamesModule.CheckMessageIsCountingAsync(recievedMessage);
            if (recievedMessage.Content.StartsWithWord(DefaultCommandPrefix, true))
            {
                // The message starts with the command prefix and the prefix is not part of another word.
                await CommandRecievedAsync(recievedMessage);
                return;
            }
            if (!await MainModule.CheckForAliasAsync(recievedMessage))
            {
                // Only check for keyword when the message is not an alias/command.
                KeywordsModule.CheckMessageForKeyword(recievedMessage);
                return;
            }
        }

        public static async Task CommandRecievedAsync(SocketMessage socketMessage)
        {
            var socketUserMessage = (SocketUserMessage)socketMessage;

            // Return if the message is not a user message.
            if (socketMessage == null) return;

            var context = new SocketCommandContext(Program.Client, socketUserMessage);

            if (socketMessage.Content == DefaultCommandPrefix)
            {
                await MainModule.SendAboutMessageToChannelAsync(context);
                return;
            }

            await ExecuteCommandAsync(
                context,
                socketMessage.Content.RemoveUnnecessaryWhiteSpace().Substring(DefaultCommandPrefix.Length + 1));
        }

        public static async Task<IResult> ExecuteCommandAsync(ICommandContext context, string input)
        {
            var typingState = context.Channel.EnterTypingState();
            try
            {
                IResult result = await BotCommandService.ExecuteAsync
                (
                    context: context,
                    input: input,
                    services: null
                );
                if (result.Error.HasValue)
                    await SendErrorMessageToChannel(result.Error, (ISocketMessageChannel)context.Channel);
                return result;
            }
            finally
            {
                typingState.Dispose();
            }
        }

        public static async Task SendErrorMessageToChannel(CommandError? commandError, ISocketMessageChannel channel)
        {
            switch (commandError)
            {
                case CommandError.BadArgCount:
                    await GenericMessenger.SendWarningAsync(
                        channel: channel,
                        description: "You either typed the wrong number of parameters, or forgot to put a parameter in \"quotes\"",
                        title: "Invalid usage of command");
                    return;

                case CommandError.ParseFailed:
                    await GenericMessenger.SendWarningAsync(
                        channel: channel,
                        description: "You might have typed an invalid parameter.",
                        title: "Parsing arguments failed");
                    return;

                case CommandError.UnknownCommand:
                    await GenericMessenger.SendWarningAsync(
                        channel: channel,
                        description: "Did you make a typo?",
                        title: "That command doesn't exist");
                    return;

                case CommandError.UnmetPrecondition:
                    await GenericMessenger.SendWarningAsync(
                        channel: channel,
                        description: "You most likely don't have the correct permissions to use this command.",
                        title: "Unmet precondition");
                    return;

                default:
                    return;
            }
        }
    }
}
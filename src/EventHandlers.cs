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
            }
            else if (logMessage.Exception != null)
            {
                Logger.LogException(logMessage.Exception);
            }
            else
            {
                Logger.Log(logMessage);
            }
        }

        public static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage message = await cachedMessage.GetOrDownloadAsync();

            if (reaction.UserId != Program.Client.CurrentUser.Id && reaction.Emote.Name == KeywordsModule.DeleteReactionEmote.Name)
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

            var typingState = socketMessage.Channel.EnterTypingState();
            try
            {
                IResult result = await BotCommandService.ExecuteAsync
                (
                    context: context,
                    input: socketMessage.Content
                        .RemoveUnnecessaryWhiteSpace()
                        .Substring(DefaultCommandPrefix.Length + 1),
                    services: null
                );

                if (result.Error.HasValue)
                    await SendErrorMessageToChannel(result.Error, socketMessage.Channel);
            }
            finally
            {
                // Always dispose this, otherwise the bot will forever be typing after an exception.
                typingState.Dispose();
            }
        }

        public static async Task SendErrorMessageToChannel(CommandError? commandError, ISocketMessageChannel channel)
        {
            switch (commandError)
            {
                case CommandError.BadArgCount:
                    await GenericMessenger.SendWarningAsync(channel, "**Invalid usage of command.**\nYou either typed the wrong number of parameters, or forgot to put a parameter in \"quotes\"");
                    break;

                case CommandError.ParseFailed:
                    await GenericMessenger.SendWarningAsync(channel, "**Parsing arguments failed.**\nYou might have typed an invalid parameter.");
                    break;

                case CommandError.UnknownCommand:
                    await GenericMessenger.SendWarningAsync(channel, "**That command doesn't exist.\n**Did you make a typo?");
                    break;

                case CommandError.UnmetPrecondition:
                    await GenericMessenger.SendWarningAsync(channel, "**Unmet precondition.**\nYou most likely don't have the correct permissions to use this command.");
                    break;

                default:
                    return;
            }
        }
    }
}
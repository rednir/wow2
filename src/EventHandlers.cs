using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Modules;
using wow2.Modules.Main;
using wow2.Modules.Keywords;
using wow2.Modules.Games;
using ExtentionMethods;

namespace wow2
{
    public static class EventHandlers
    {
        public static string CommandPrefix { get; } = "!wow";
        public static CommandService BotCommandService;

        public static async Task InstallCommandsAsync()
        {
            var config = new CommandServiceConfig()
            {
                LogLevel = LogSeverity.Verbose
            };
            BotCommandService = new CommandService(config);
            BotCommandService.Log += DiscordLogRecievedAsync;
            await BotCommandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        public static async Task ReadyAsync()
        {
            await Program.Client.SetGameAsync("!wow help");
        }

        public static async Task DiscordLogRecievedAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is CommandException commandException)
            {
                string verboseErrorMessage = "An unhandled exception was thrown and was automatically reported.";

                if (commandException.InnerException is CommandReturnException)
                    return;
                else if (commandException.InnerException is DirectoryNotFoundException || commandException.InnerException is FileNotFoundException)
                    verboseErrorMessage = "The host of the bot is missing required assets.";

                Logger.Log(commandException, $"Command '{commandException.Command.Name}' threw an exception in guild '{commandException.Context.Guild.Name}' due to message '{commandException.Context.Message.Content}'");
                
                await commandException.Context.Channel.SendMessageAsync(
                    embed: MessageEmbedPresets.Verbose(verboseErrorMessage, VerboseMessageSeverity.Error)
                );
            }
            else if (logMessage.Exception != null)
            {
                Logger.Log(logMessage.Exception);
            }
            else
            {
                Logger.Log(logMessage);
            }
        }

        public static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage message = await cachedMessage.GetOrDownloadAsync();

            if (reaction.UserId != Program.Client.CurrentUser.Id && reaction.Emote.Name == KeywordsModule.ReactToDeleteEmote.Name)
            {
                if (await KeywordsModule.DeleteMessageIfKeywordResponse(message))
                {
                    Console.WriteLine($"Message was deleted in channel `{channel.Name}` due to reaction added by `{reaction.User}` ({reaction.UserId})");
                    return;
                }
            }
        }

        public static async Task MessageRecievedAsync(SocketMessage recievedMessage)
        {
            if (recievedMessage.Author.Id == Program.Client.CurrentUser.Id) return;

            await DataManager.EnsureGuildDataFileExistsAsync(recievedMessage.GetGuild().Id);

            if (recievedMessage.Content.StartsWithWord(CommandPrefix))
            {
                // The message starts with the command prefix and the prefix is not part of another word.
                await CommandRecievedAsync(recievedMessage);
                return;
            }

            await MainModule.CheckForAliasAsync(recievedMessage);
            await KeywordsModule.CheckMessageForKeywordAsync(recievedMessage);
            await GamesModule.CheckMessageIsCountingAsync(recievedMessage);
        }

        public static async Task CommandRecievedAsync(SocketMessage socketMessage)
        {
            var socketUserMessage = (SocketUserMessage)socketMessage;
            var context = new SocketCommandContext(Program.Client, (SocketUserMessage)socketMessage);

            // Return if the message is not a user message.
            if (socketMessage == null) return;

            Console.WriteLine($"{socketMessage.Author} executed a command ({socketMessage.Content})");

            if (socketMessage.Content == CommandPrefix)
            {
                await MainModule.ShowAboutAsync(context);
                return;
            }

            IResult result = await BotCommandService.ExecuteAsync
            (
                context: context,
                argPos: CommandPrefix.Length + 1,
                services: null
            );

            if (result.Error.HasValue)
                await SendErrorMessageToChannel(result.Error, socketMessage.Channel);
        }

        public static async Task SendErrorMessageToChannel(CommandError? commandError, IMessageChannel channel)
        {
            Embed embedToSend;
            switch (commandError)
            {
                case CommandError.BadArgCount:
                    embedToSend = MessageEmbedPresets.Verbose("**Invalid usage of command.**\nYou either typed the wrong number of parameters, or forgot to put a parameter in \"quotes\"", VerboseMessageSeverity.Warning);
                    break;

                case CommandError.ParseFailed:
                    embedToSend = MessageEmbedPresets.Verbose("**Parsing arguments failed.**\nYou probably typed a parameter that was the incorrect type.", VerboseMessageSeverity.Warning);
                    break;

                case CommandError.UnknownCommand:
                    embedToSend = MessageEmbedPresets.Verbose("**That command doesn't exist.**", VerboseMessageSeverity.Warning);
                    break;

                case CommandError.UnmetPrecondition:
                    embedToSend = MessageEmbedPresets.Verbose("**Unmet precondition.**\nYou most likely don't have the correct permissions to use this command.", VerboseMessageSeverity.Warning);
                    break;

                default:
                    return;
            }
            await channel.SendMessageAsync(embed: embedToSend);
        }
    }
}
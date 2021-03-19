using System;
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
            BotCommandService.Log += LogAsync;
            await BotCommandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        public static async Task ReadyAsync()
        {
            await Program.Client.SetGameAsync("!wow help");
        }

        public static async Task LogAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is CommandException commandException)
            {
                if (commandException.InnerException is CommandReturnException)
                    return;

                Console.WriteLine($"Log: [{logMessage.Source}: {logMessage.Severity}] Command '{commandException.Command.Name}' threw an exception in guild '{commandException.Context.Guild.Name}' due to message '{commandException.Context.Message.Content}'");
                Console.WriteLine($" ------ START OF EXCEPTION ------\n{commandException}\n------ END OF EXCEPTION ------");

                await commandException.Context.Channel.SendMessageAsync(
                    embed: MessageEmbedPresets.Verbose($"An unhandled exception was thrown and was automatically reported.", VerboseMessageSeverity.Error)
                );
            }
            else if (logMessage.Exception != null)
            {
                Console.WriteLine($"Log: [{logMessage.Source}: {logMessage.Severity}] Exception was thrown:");
                Console.WriteLine($" ------ START OF EXCEPTION ------\n{logMessage.Exception}\n------ END OF EXCEPTION ------");
            }
            else
            {
                Console.WriteLine($"Log: [{logMessage.Source}: {logMessage.Severity}] {logMessage.Message}");
            }
        }

        public static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage message = await cachedMessage.GetOrDownloadAsync();

            if (reaction.UserId != Program.Client.CurrentUser.Id)
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

            if (recievedMessage.Content.StartsWith(CommandPrefix))
            {
                await CommandRecievedAsync(recievedMessage);
                return;
            }

            await KeywordsModule.CheckMessageForKeywordAsync(recievedMessage);
            await GamesModule.MessageRecievedForCountingAsync(recievedMessage);
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

        private static async Task SendErrorMessageToChannel(CommandError? commandError, IMessageChannel channel)
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
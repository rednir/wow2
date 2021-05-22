using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using wow2.Data;
using wow2.Extensions;
using wow2.Modules;
using wow2.Modules.Games.Counting;
using wow2.Modules.Games.NumberMemory;
using wow2.Modules.Games.VerbalMemory;
using wow2.Modules.Keywords;
using wow2.Modules.Main;
using wow2.Modules.Moderator;
using wow2.Verbose;
using wow2.Verbose.Messages;

namespace wow2
{
    public static class Bot
    {
        public static DiscordSocketClient Client { get; set; } = new(new DiscordSocketConfig()
        {
            ExclusiveBulkDelete = false,
        });

        public static RestApplication ApplicationInfo { get; set; }
        public static CommandService CommandService { get; set; }

        public static async Task<SocketGuildUser> GetClientGuildUserAsync(ISocketMessageChannel channel)
            => (SocketGuildUser)await channel.GetUserAsync(Client.CurrentUser.Id);

        public static async Task InitializeAndStartClientAsync()
        {
            Client.Ready += ReadyAsync;
            Client.Log += DiscordLogRecievedAsync;
            Client.ReactionAdded += ReactionAddedAsync;
            Client.ReactionRemoved += ReactionRemovedAsync;
            Client.MessageReceived += MessageRecievedAsync;
            Client.MessageDeleted += MessageDeletedAsync;
            Client.JoinedGuild += JoinedGuildAsync;
            Client.LeftGuild += LeftGuildAsync;

            await Client.LoginAsync(TokenType.Bot, DataManager.Secrets.DiscordBotToken);
            await Client.StartAsync();

            ApplicationInfo = await Client.GetApplicationInfoAsync();
        }

        public static async Task InstallCommandsAsync()
        {
            var config = new CommandServiceConfig()
            {
                IgnoreExtraArgs = true,
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
            };
            CommandService = new CommandService(config);
            CommandService.Log += DiscordLogRecievedAsync;
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        public static async Task ReadyAsync()
        {
            await DataManager.InitializeAsync();
            await InstallCommandsAsync();

            await Client.SetGameAsync("!wow help");
        }

        public static async Task JoinedGuildAsync(SocketGuild guild)
        {
            var guildData = await DataManager.EnsureGuildDataExistsAsync(guild.Id);

            // Only set if it's the first time the bot has joined this guild.
            if (guildData.DateTimeJoinedBinary == 0)
            {
                DataManager.AllGuildData[guild.Id]
                    .DateTimeJoinedBinary = DateTime.Now.ToBinary();
            }

            await new WelcomeMessage(guild)
                .SendToBestChannelAsync();
        }

        public static async Task LeftGuildAsync(SocketGuild guild)
        {
            await DataManager.UnloadGuildDataAsync(guild.Id);
        }

        public static async Task DiscordLogRecievedAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is Exception)
            {
                // Return if command intentionally threw an exception.
                if (logMessage.Exception.InnerException is CommandReturnException)
                    return;

                if (logMessage.Exception is CommandException commandException)
                {
                    string errorMessageText = $"An unhandled exception was thrown and was automatically reported.\n```{commandException.InnerException.Message}\n```";
                    switch (commandException.InnerException)
                    {
                        case NotImplementedException:
                            // Another intentional exception.
                            await new WarningMessage("This hasn't been implemented yet. Check back later!")
                                .SendAsync(commandException.Context.Channel);
                            return;

                        case DirectoryNotFoundException:
                        case FileNotFoundException:
                            errorMessageText = "The host is missing required assets.";
                            break;
                    }

                    await new ErrorMessage(errorMessageText)
                        .SendAsync(commandException.Context.Channel);
                }

                if ((logMessage.Exception is GatewayReconnectException ||
                    logMessage.Exception.InnerException is WebSocketClosedException ||
                    logMessage.Exception.InnerException is WebSocketException) &&
                    !Program.IsDebug)
                {
                    // Client will immediately reconnect after these
                    // exceptions, so no need to dm bot owner.
                    Logger.LogException(logMessage.Exception, notifyOwner: false);
                }
                else
                {
                    Logger.LogException(logMessage.Exception, notifyOwner: true);
                }
            }
            else
            {
                Logger.Log(logMessage);
            }
        }

        public static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == Client.CurrentUser.Id)
                return;

            IUserMessage message = await cachedMessage.GetOrDownloadAsync();
            if (message == null)
                return;

            if (!await PagedMessage.ActOnReactionAsync(reaction))
                await ResponseMessage.ActOnReactionAddedAsync(reaction, message);
        }

        public static async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == Client.CurrentUser.Id)
                return;

            IUserMessage message = await cachedMessage.GetOrDownloadAsync();
            if (message == null)
                return;

            ResponseMessage.ActOnReactionRemoved(reaction, message);
        }

        public static Task MessageDeletedAsync(Cacheable<IMessage, ulong> cachedMessage, ISocketMessageChannel channel)
        {
            PagedMessage message = PagedMessage.ListOfPagedMessages.Find(
                m => m.SentMessage.Id == cachedMessage.Id);
            message?.Dispose();
            return Task.CompletedTask;
        }

        public static async Task MessageRecievedAsync(SocketMessage socketMessage)
        {
            if (CommandService == null)
                return;
            if (socketMessage.Author.Id == Client.CurrentUser.Id)
                return;
            if (socketMessage.Channel is SocketDMChannel)
                return;
            if (socketMessage is not SocketUserMessage socketUserMessage)
                return;

            var context = new SocketCommandContext(Client, socketUserMessage);
            await DataManager.EnsureGuildDataExistsAsync(context.Guild.Id);
            await ActOnMessageAsync(context);
        }

        public static async Task<IResult> ExecuteCommandAsync(SocketCommandContext context, string input)
        {
            using var _ = context.Channel.EnterTypingState();

            if (ModeratorModule.CheckForCommandAbuse(context))
            {
                await new WarningMessage("Please don't spam commands, it's annoying.\nWait a minute or so before executing another command.", "Calm yourself.")
                    .SendAsync(context.Channel);
                return null;
            }

            IResult result = await CommandService.ExecuteAsync(
                context: context,
                input: input,
                services: null);

            if (result.Error.HasValue)
                await SendErrorMessageToChannel(result.Error, context);
            return result;
        }

        public static async Task SendErrorMessageToChannel(CommandError? commandError, ICommandContext context)
        {
            string commandPrefix = context.Guild.GetCommandPrefix();

            var matchingCommands = await SearchCommandsAsync(
                context, context.Message.Content.MakeCommandInput(commandPrefix));
            var bestMatchingCommandString = $"``{matchingCommands.FirstOrDefault()?.MakeFullCommandString(commandPrefix)}``";

            switch (commandError)
            {
                case CommandError.BadArgCount:
                    await new WarningMessage(
                        description: $"You might have typed too many or too little parameters. {bestMatchingCommandString}",
                        title: "Invalid usage of command")
                            .SendAsync(context.Channel);
                    return;

                case CommandError.ObjectNotFound:
                case CommandError.ParseFailed:
                    await new WarningMessage(
                        description: $"You might have typed an invalid parameter. {bestMatchingCommandString}",
                        title: "Parsing arguments failed")
                            .SendAsync(context.Channel);
                    return;

                case CommandError.UnknownCommand:
                    await new WarningMessage(
                        description: !matchingCommands.Any() ?
                            "Did you make a typo?" : $"Maybe you meant to type:\n{matchingCommands.MakeReadableString(commandPrefix)}",
                        title: "That command doesn't exist")
                            .SendAsync(context.Channel);
                    return;

                case CommandError.UnmetPrecondition:
                    await new WarningMessage(
                        description: "You most likely don't have the correct permissions to use this command.",
                        title: "Unmet precondition")
                            .SendAsync(context.Channel);
                    return;

                default:
                    return;
            }
        }

        private static async Task<IEnumerable<CommandInfo>> SearchCommandsAsync(ICommandContext context, string term)
        {
            if (term.Length < 2)
                return Array.Empty<CommandInfo>();

            SearchResult result = CommandService.Search(
                context: new CommandContext(context.Client, context.Message),
                input: term);

            if (result.Commands == null)
            {
                // This provides more results but is not as accurate.
                return (await CommandService.GetExecutableCommandsAsync(
                    new CommandContext(context.Client, context.Message), null))
                        .Where(command =>
                            command.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            command.Module.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            term.Contains(command.Name, StringComparison.OrdinalIgnoreCase) ||
                            term.Contains(command.Module.Name, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return result.Commands.Select(c => c.Command);
            }
        }

        private static async Task ActOnMessageAsync(SocketCommandContext context)
        {
            SocketUserMessage message = context.Message;

            // Only auto mod message if not related to a game.
            if (!await CountingGame.CheckMessageAsync(context) &&
                !await VerbalMemoryGame.CheckMessageAsync(message) &&
                !await NumberMemoryGame.CheckMessageAsync(message))
            {
                await ModeratorModule.CheckMessageWithAutoMod(message);
            }

            if (message.Content.StartsWithWord(
                context.Guild.GetCommandPrefix(), true))
            {
                // The message starts with the command prefix and the prefix is not part of another word.
                await ActOnMessageAsCommandAsync(context);
                return;
            }
            else if (!await MainModule.CheckForAliasAsync(message))
            {
                // Only check for keyword when the message is not an alias/command.
                KeywordsModule.CheckMessageForKeyword(message);
                return;
            }
        }

        private static async Task ActOnMessageAsCommandAsync(SocketCommandContext context)
        {
            string commandPrefix = context.Guild.GetCommandPrefix();

            if (context.Message.Content == commandPrefix)
            {
                await new AboutMessage(commandPrefix)
                    .SendAsync(context.Channel);
                return;
            }

            await ExecuteCommandAsync(
                context,
                context.Message.Content.MakeCommandInput(commandPrefix));
        }
    }
}
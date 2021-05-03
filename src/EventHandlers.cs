using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Verbose;
using wow2.Verbose.Messages;
using wow2.Modules;
using wow2.Modules.Main;
using wow2.Modules.Keywords;
using wow2.Modules.Games.Counting;
using wow2.Modules.Games.VerbalMemory;
using wow2.Modules.Games.NumberMemory;
using wow2.Modules.Moderator;
using wow2.Extentions;
using wow2.Data;

namespace wow2
{
    public static class Bot
    {
        public static CommandService CommandService { get; set; }

        public static async Task InstallCommandsAsync()
        {
            var config = new CommandServiceConfig()
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async
            };
            CommandService = new CommandService(config);
            CommandService.Log += DiscordLogRecievedAsync;
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        public static async Task ReadyAsync()
        {
            await DataManager.InitializeAsync();
            await Bot.InstallCommandsAsync();

            await Program.Client.SetGameAsync("!wow help");
        }

        public static async Task JoinedGuildAsync(SocketGuild guild)
        {
            var guildData = await DataManager.EnsureGuildDataExistsAsync(guild.Id);
            string commandPrefix = MainModule.GetConfigForGuild(guild).CommandPrefix;

            // Only set if it's the first time the bot has joined this guild.
            if (guildData.DateTimeJoinedBinary == 0)
            {
                DataManager.DictionaryOfGuildData[guild.Id]
                    .DateTimeJoinedBinary = DateTime.Now.ToBinary();
            }
            var embedBuilder = new EmbedBuilder()
            {
                Title = "👋 Hi there!",
                Description = $"Thanks for adding me to your server!\nTo get started, type `{commandPrefix} help` to see the wide range of commands available.\n",
                Color = Color.Gold
            };
            await guild.DefaultChannel.SendMessageAsync(embed: embedBuilder.Build());
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

                Logger.LogException(logMessage.Exception);

                // Return as client will immediately reconnect after this exception. 
                if ((logMessage.Exception is GatewayReconnectException ||
                    logMessage.Exception.InnerException is WebSocketClosedException ||
                    logMessage.Exception.InnerException is WebSocketException) &&
                    !Program.IsDebug)
                {
                    return;
                }

                try
                {
                    await Program.ApplicationInfo.Owner.SendMessageAsync($"```\n{logMessage.Exception}\n```");
                }
                catch
                {
                }
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
                }
            }
        }

        public static async Task MessageRecievedAsync(SocketMessage receivedMessage)
        {
            if (receivedMessage.Author.Id == Program.Client.CurrentUser.Id) return;
            if (receivedMessage.Channel is SocketDMChannel) return;

            await DataManager.EnsureGuildDataExistsAsync(receivedMessage.GetGuild().Id);

            // Only auto mod message if not related to a game.
            if (!(await CountingGame.CheckMessageAsync(receivedMessage)) &&
                !(await VerbalMemoryGame.CheckMessageAsync(receivedMessage)) &&
                !(await NumberMemoryGame.CheckMessageAsync(receivedMessage)))
            {
                await ModeratorModule.CheckMessageWithAutoMod(receivedMessage);
            }

            string commandPrefix = MainModule.GetConfigForGuild(receivedMessage.GetGuild()).CommandPrefix;
            if (receivedMessage.Content.StartsWithWord(commandPrefix, true))
            {
                // The message starts with the command prefix and the prefix is not part of another word.
                await CommandRecievedAsync(receivedMessage);
                return;
            }
            else if (!await MainModule.CheckForAliasAsync(receivedMessage))
            {
                // Only check for keyword when the message is not an alias/command.
                KeywordsModule.CheckMessageForKeyword(receivedMessage);
                return;
            }
        }

        public static async Task CommandRecievedAsync(SocketMessage socketMessage)
        {
            var socketUserMessage = (SocketUserMessage)socketMessage;

            // Return if the message is not a user message.
            if (socketMessage == null) return;

            var context = new SocketCommandContext(Program.Client, socketUserMessage);

            string commandPrefix = MainModule.GetConfigForGuild(socketMessage.GetGuild()).CommandPrefix;
            if (socketMessage.Content == commandPrefix)
            {
                await MainModule.SendAboutMessageToChannelAsync(context);
                return;
            }

            await ExecuteCommandAsync(
                context,
                socketMessage.Content.MakeCommandInput(commandPrefix));
        }

        public static async Task<IResult> ExecuteCommandAsync(ICommandContext context, string input)
        {
            var typingState = context.Channel.EnterTypingState();
            try
            {
                IResult result = await CommandService.ExecuteAsync
                (
                    context: context,
                    input: input,
                    services: null
                );

                if (result.Error.HasValue)
                    await SendErrorMessageToChannel(result.Error, context);
                return result;
            }
            finally
            {
                typingState.Dispose();
            }
        }

        public static async Task SendErrorMessageToChannel(CommandError? commandError, ICommandContext context)
        {
            string commandPrefix = MainModule.GetConfigForGuild((SocketGuild)context.Guild).CommandPrefix;

            var matchingCommands = await SearchCommandsAsync(context,
                context.Message.Content.MakeCommandInput(commandPrefix));
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
            return (await CommandService.GetExecutableCommandsAsync(
                new CommandContext(context.Client, context.Message), null
            ))
            .Where(command =>
                command.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                command.Module.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                term.Contains(command.Name, StringComparison.OrdinalIgnoreCase) ||
                term.Contains(command.Module.Name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
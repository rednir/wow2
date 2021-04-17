using System;
using System.IO;
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
using wow2.Modules.Moderator;
using wow2.Extentions;
using wow2.Data;

namespace wow2
{
    public static class EventHandlers
    {
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
            await Program.Client.SetGameAsync("!wow help");
        }

        public static async Task JoinedGuildAsync(SocketGuild guild)
        {
            await DataManager.EnsureGuildDataFileExistsAsync(guild.Id);
            string commandPrefix = MainModule.GetConfigForGuild(guild).CommandPrefix;

            var embedBuilder = new EmbedBuilder()
            {
                Title = "👋 Hi there!",
                Description = $"Thanks for adding me to your server!\nTo get started, type `{commandPrefix} help` to see the wide range of commands available.\n",
                Color = Color.Gold
            };
            await guild.DefaultChannel.SendMessageAsync(embed: embedBuilder.Build());
        }

        public static async Task DiscordLogRecievedAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is Exception)
            {
                // Return if command intentionally threw.
                if (logMessage.Exception.InnerException is CommandReturnException)
                    return;

                Logger.LogException(logMessage.Exception);

                // Return as client will immediately reconnect after this exception. 
                if ((logMessage.Exception is GatewayReconnectException ||
                    logMessage.Exception.InnerException is WebSocketClosedException ||
                    logMessage.Exception.InnerException is WebSocketException) &&
                    !Program.IsDebug)
                    return;

                try
                {
                    await Program.ApplicationInfo.Owner.SendMessageAsync($"```\n{logMessage.Exception}\n```");
                }
                catch
                {
                }

                if (logMessage.Exception is CommandException commandException)
                {
                    string errorMessageText =
                        commandException.InnerException is DirectoryNotFoundException || commandException.InnerException is FileNotFoundException ?
                        "The host is missing required assets." :
                        $"An unhandled exception was thrown and was automatically reported.\n```{commandException.InnerException.Message}\n```";

                    await new ErrorMessage(errorMessageText)
                        .SendAsync(commandException.Context.Channel);
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
                    return;
                }
            }
        }

        public static async Task MessageRecievedAsync(SocketMessage recievedMessage)
        {
            if (recievedMessage.Author.Id == Program.Client.CurrentUser.Id) return;
            if (recievedMessage.Channel is SocketDMChannel) return;

            await DataManager.EnsureGuildDataFileExistsAsync(recievedMessage.GetGuild().Id);

            // Only auto mod message if not related to a game.
            if (!(await CountingGame.CheckMessageAsync(recievedMessage)) && !(await VerbalMemoryGame.CheckMessageAsync(recievedMessage)))
                await ModeratorModule.CheckMessageWithAutoMod(recievedMessage);

            string commandPrefix = MainModule.GetConfigForGuild(recievedMessage.GetGuild()).CommandPrefix;
            if (recievedMessage.Content.StartsWithWord(commandPrefix, true))
            {
                // The message starts with the command prefix and the prefix is not part of another word.
                await CommandRecievedAsync(recievedMessage);
                return;
            }
            else if (!await MainModule.CheckForAliasAsync(recievedMessage))
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

            string commandPrefix = MainModule.GetConfigForGuild(socketMessage.GetGuild()).CommandPrefix;
            if (socketMessage.Content == commandPrefix)
            {
                await MainModule.SendAboutMessageToChannelAsync(context);
                return;
            }

            await ExecuteCommandAsync(
                context,
                socketMessage.Content.RemoveUnnecessaryWhiteSpace().Substring(commandPrefix.Length + 1));
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
                    await new WarningMessage(
                        description: "You either typed the wrong number of parameters, or forgot to put a parameter in \"quotes\"",
                        title: "Invalid usage of command")
                            .SendAsync(channel);
                    return;

                case CommandError.ParseFailed:
                    await new WarningMessage(
                        description: "You might have typed an invalid parameter.",
                        title: "Parsing arguments failed")
                            .SendAsync(channel);
                    return;

                case CommandError.UnknownCommand:
                    await new WarningMessage(
                        description: "Did you make a typo?",
                        title: "That command doesn't exist")
                            .SendAsync(channel);
                    return;

                case CommandError.UnmetPrecondition:
                    await new WarningMessage(
                        description: "You most likely don't have the correct permissions to use this command.",
                        title: "Unmet precondition")
                            .SendAsync(channel);
                    return;

                case CommandError.ObjectNotFound:
                    await new WarningMessage(
                        description: "Object not found.",
                        title: "Invalid usage of command")
                            .SendAsync(channel);
                    return;

                default:
                    return;
            }
        }
    }
}
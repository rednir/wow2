using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Modules;
using wow2.Bot.Modules.AttachmentVoting;
using wow2.Bot.Modules.Games.Counting;
using wow2.Bot.Modules.Games.VerbalMemory;
using wow2.Bot.Modules.Keywords;
using wow2.Bot.Modules.Main;
using wow2.Bot.Modules.Moderator;
using wow2.Bot.Modules.Osu;
using wow2.Bot.Modules.ServerIcon;
using wow2.Bot.Modules.Spotify;
using wow2.Bot.Modules.Timers;
using wow2.Bot.Modules.YouTube;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot
{
    public static class BotService
    {
        public static DiscordSocketClient Client { get; set; }

        public static RestApplication ApplicationInfo { get; set; }

        public static CommandService CommandService { get; set; }

        public static IServiceProvider Services { get; set; }

        public static bool IsDisabled { get; set; } = false;

        public static async Task<SocketGuildUser> GetClientGuildUserAsync(ISocketMessageChannel channel)
            => (SocketGuildUser)await channel.GetUserAsync(Client.CurrentUser.Id);

        public static async Task InitializeAndStartClientAsync()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true,
            });

            Client.Ready += ReadyAsync;
            Client.Log += DiscordLogRecievedAsync;

            await Client.LoginAsync(TokenType.Bot, DataManager.Secrets.DiscordBotToken);
            await Client.StartAsync();

            await Client.SetStatusAsync(UserStatus.Online);
            await Client.SetGameAsync("!wow help");
            PollingService.CreateTask(DataManager.SaveGuildDataToFileAsync, 1800);

            ApplicationInfo = await Client.GetApplicationInfoAsync();
        }

        public static async Task InstallCommandsAsync()
        {
            if (CommandService != null)
            {
                Logger.Log("Not installing commands as they have already been installed.", LogSeverity.Debug);
                return;
            }

            Services = new ServiceCollection()
                .AddSingleton<IYoutubeModuleService>(new YoutubeModuleService(DataManager.Secrets.GoogleApiKey))
                .AddSingleton<IOsuModuleService>(new OsuModuleService(DataManager.Secrets.OsuClientId, DataManager.Secrets.OsuClientSecret))
                .AddSingleton<ISpotifyModuleService>(new SpotifyModuleService(DataManager.Secrets.SpotifyClientId, DataManager.Secrets.SpotifyClientSecret))
                .BuildServiceProvider();

            // TODO: inject this the proper way. need to get DownloadService non-static first.
            Modules.Voice.DownloadService.YouTubeService = Services.GetService<IYoutubeModuleService>();
            Modules.Voice.DownloadService.SpotifyService = Services.GetService<ISpotifyModuleService>();

            var config = new CommandServiceConfig()
            {
                IgnoreExtraArgs = true,
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
            };

            CommandService = new CommandService(config);
            CommandService.Log += DiscordLogRecievedAsync;
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

            Logger.Log("Installed commands.", LogSeverity.Debug);
        }

        public static string MakeCommandsMarkdown()
        {
            var commandsGroupedByModule = CommandService.Commands
                .GroupBy(c => c.Module);

            // TODO: This is hacky, will break if the module name "Main" changes.
            var mainGroup = commandsGroupedByModule.First(g => g.Key.Name == "Main");
            commandsGroupedByModule = commandsGroupedByModule
                .Where(g => g.Key.Name != "Main")
                .Prepend(mainGroup);

            var stringBuilder = new StringBuilder($"# List of commands ({CommandService.Commands.Count()} total)\n\n");
            foreach (var module in commandsGroupedByModule)
            {
                stringBuilder
                    .Append("## ")
                    .Append(module.Key.Name)
                    .Append(" (")
                    .Append(module.Count())
                    .Append(')')
                    .Append('\n')
                    .Append(module.First().Module.Summary)
                    .Append("\n\n")
                    .Append("|Command|Summary|\n|---|---|\n");

                foreach (var command in module)
                {
                    stringBuilder
                        .Append('|')
                        .Append(command.MakeFullCommandString("!wow"))
                        .Append('|')
                        .Append(command.Summary ?? "No description provided.")
                        .Append('|')
                        .Append('\n');
                }

                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        public static async Task ReadyAsync()
        {
            if (DataManager.AllGuildData.Count == 0)
            {
                await DataManager.InitializeAsync();

                // TODO: This timer stuff needs to be moved out of here.
                ServerIconModule.InitializeAllTimers();
                TimersModule.InitializeAllTimers();

                Client.ReactionAdded += ReactionAddedAsync;
                Client.ReactionRemoved += ReactionRemovedAsync;
                Client.MessageReceived += MessageRecievedAsync;
                Client.MessageDeleted += MessageDeletedAsync;
                Client.JoinedGuild += JoinedGuildAsync;
                Client.LeftGuild += LeftGuildAsync;
                Client.ButtonExecuted += ButtonExecutedAsync;
            }
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

            // This could take a while.
            _ = new WelcomeMessage(guild)
                .SendToBestChannelAsync();
        }

        public static async Task LeftGuildAsync(SocketGuild guild)
        {
            await DataManager.UnloadGuildDataAsync(guild.Id);
        }

        public static async Task DiscordLogRecievedAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is not null)
            {
                // Return if command intentionally threw an exception.
                if (logMessage.Exception.InnerException is CommandReturnException)
                    return;

                if (logMessage.Exception is CommandException commandException)
                {
                    string errorMessageText = $"An unhandled error occured when executing the command.```{commandException.InnerException.Message}\n```";
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

                if ((logMessage.Exception is GatewayReconnectException
                    || logMessage.Exception.InnerException is WebSocketClosedException or WebSocketException)
                    && !Program.IsDebug)
                {
                    // Client reconnects after these exceptions, so no need to dm bot owner.
                    Logger.LogException(logMessage.Exception, notifyOwner: false);
                }
                else
                {
                    Logger.LogException(logMessage.Exception, notifyOwner: true);
                }
            }
            else
            {
                // These "Unknown Dispatch" messages are useless to me and take up 90% of the log file.
                if (!logMessage.Message.Contains("Unknown Dispatch"))
                    Logger.Log(logMessage);
            }
        }

        public static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            if (reaction.UserId == Client.CurrentUser.Id)
                return;

            _ = !await ResponseMessage.ActOnReactionAddedAsync(reaction)
                && !AttachmentVotingModule.ActOnReactionAdded(reaction);
        }

        public static Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            if (reaction.UserId == Client.CurrentUser.Id)
                return Task.CompletedTask;

            _ = !ResponseMessage.ActOnReactionRemoved(reaction)
                && !AttachmentVotingModule.ActOnReactionRemoved(reaction);

            return Task.CompletedTask;
        }

        public static async Task MessageDeletedAsync(Cacheable<IMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel)
        {
            ISocketMessageChannel channel = await cachedChannel.GetOrDownloadAsync() as ISocketMessageChannel;
            InteractiveMessage.FromMessageId(DataManager.AllGuildData[channel.GetGuild().Id], cachedMessage.Id)?.Dispose();
        }

        public static Task ButtonExecutedAsync(SocketMessageComponent component)
        {
            _ = InteractiveMessage.ActOnButtonAsync(component);
            return Task.CompletedTask;
        }

        public static async Task MessageRecievedAsync(SocketMessage socketMessage)
        {
            if (socketMessage.Author.Id == Client.CurrentUser.Id)
                return;
            if (socketMessage is not SocketUserMessage socketUserMessage)
                return;

            if (socketMessage.Channel is SocketDMChannel)
            {
                // Route the messages the bot recieves to the owner.
                if (socketMessage.Author != ApplicationInfo.Owner)
                {
                    await ApplicationInfo.Owner.SendMessageAsync(
                        text: $"Message from **{socketMessage.Author.Mention}** ({socketMessage.Author})",
                        embed: new GenericMessage(socketMessage.Content).Embed);
                }

                return;
            }

            var context = new SocketCommandContext(Client, socketUserMessage);
            await DataManager.EnsureGuildDataExistsAsync(context.Guild.Id);
            await ActOnMessageAsync(context);
        }

        public static async Task<IResult> ExecuteCommandAsync(SocketCommandContext context, string input)
        {
            if (IsDisabled && context.User.Id != ApplicationInfo.Owner.Id)
            {
                await new WarningMessage("You can't use any commands right now. Check my Discord status to see when I go back online, or shoot me a DM.", "The bot is under maintenance.")
                    .SendAsync(context.Channel);
                return null;
            }

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
                services: Services);

            if (result.Error.HasValue)
                await SendErrorMessageToChannel(result.Error, context);
            return result;
        }

        public static async Task SendErrorMessageToChannel(CommandError? commandError, SocketCommandContext context)
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

        private static async Task<IEnumerable<CommandInfo>> SearchCommandsAsync(SocketCommandContext context, string term)
        {
            if (term.Length < 2)
                return Array.Empty<CommandInfo>();

            SearchResult result = CommandService.Search(
                context: context,
                input: term);

            if (result.Commands == null)
            {
                // This provides more results but is not as accurate.
                return (await CommandService.GetExecutableCommandsAsync(context, null))
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

            await AttachmentVotingModule.CheckMessageAsync(context);

            // Only auto mod message if not related to a game.
            //if (!await CountingGame.CheckMessageAsync(context))
            //{
                await ModeratorModule.CheckMessageWithAutoMod(context);
            //}

            if (message.Content.StartsWithWord(
                context.Guild.GetCommandPrefix(), true))
            {
                // The message starts with the command prefix and the prefix is not part of another word.
                await ActOnMessageAsCommandAsync(context);
                return;
            }
            else if (!await MainModule.TryExecuteAliasAsync(context))
            {
                // Only check for keyword when the message is not an alias/command.
                KeywordsModule.CheckMessageForKeyword(context);
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
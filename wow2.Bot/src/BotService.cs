using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Modules;
using wow2.Bot.Modules.Games.Counting;
using wow2.Bot.Modules.Games.VerbalMemory;
using wow2.Bot.Modules.Keywords;
using wow2.Bot.Modules.Main;
using wow2.Bot.Modules.Moderator;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot
{
    public class BotService
    {
        public BotService(Secrets secrets, string guildDataDirPath)
        {
            Data = new BotDataManager(this, guildDataDirPath);
            Secrets = secrets;
        }

        public event EventHandler<LogEventArgs> LogRequested;

        public DiscordSocketClient Client { get; set; }

        public IServiceProvider Services { get; set; }

        public LogSeverity LogSeverity { get; set; } = LogSeverity.Verbose;

        public CommandService CommandService { get; set; }

        public RestApplication ApplicationInfo { get; set; }

        public BotDataManager Data { get; set; }

        public Secrets Secrets { get; set; } = new Secrets();

        public async Task<SocketGuildUser> GetClientGuildUserAsync(ISocketMessageChannel channel)
            => (SocketGuildUser)await channel.GetUserAsync(Client.CurrentUser.Id);

        public async Task InitializeAndStartClientAsync()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                ExclusiveBulkDelete = false,
                AlwaysDownloadUsers = true,
            });

            MethodInfo[] pollTasks = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(PollTaskAttribute), false).Length > 0)
                .ToArray();

            // Start polling tasks.
            foreach (MethodInfo method in pollTasks)
            {
                StartPollTask(
                    func: (Func<BotService, Task>)Delegate
                        .CreateDelegate(typeof(Func<BotService, Task>), null, method),
                    intervalMinutes: ((PollTaskAttribute)method
                        .GetCustomAttribute(typeof(PollTaskAttribute))).IntervalMinutes);
            }

            // Add event handlers.
            Client.Ready += ReadyAsync;
            Client.Log += DiscordLogRecievedAsync;
            Client.ReactionAdded += ReactionAddedAsync;
            Client.ReactionRemoved += ReactionRemovedAsync;
            Client.MessageReceived += MessageRecievedAsync;
            Client.MessageDeleted += MessageDeletedAsync;
            Client.JoinedGuild += JoinedGuildAsync;
            Client.LeftGuild += LeftGuildAsync;

            // Start the Discord client.
            await Client.LoginAsync(TokenType.Bot, Secrets.DiscordBotToken);
            await Client.StartAsync();
            await InstallCommandsAsync();

            ApplicationInfo = await Client.GetApplicationInfoAsync();
        }

        public async Task InstallCommandsAsync()
        {
            Services = new ServiceCollection()
                .AddSingleton(this)
                .BuildServiceProvider();

            var config = new CommandServiceConfig()
            {
                IgnoreExtraArgs = true,
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
            };

            CommandService = new CommandService(config);
            CommandService.Log += DiscordLogRecievedAsync;
            await CommandService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
        }

        public string MakeCommandsMarkdown()
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
                stringBuilder.AppendLine(
                    $"## {module.Key.Name}\n{module.First().Module.Summary}\n");

                foreach (var command in module)
                {
                    string summary = command.Summary == null ? null : $"\n     - {command.Summary}";
                    stringBuilder.AppendLine(
                        $" - {command.MakeFullCommandString("!wow")}{summary}\n");
                }
            }

            return stringBuilder.ToString();
        }

        public void Log(object message, LogSeverity severity = LogSeverity.Debug)
        {
            if (severity == LogSeverity.Debug
                && LogSeverity != LogSeverity.Debug)
            {
                return;
            }

            LogRequested(this, new LogEventArgs(
                $"{DateTime.Now} [{severity}] {message}"));
        }

        public void Log(LogMessage logMessage)
            => Log($"{logMessage.Source}: {logMessage.Message}", logMessage.Severity);

        public void LogException(Exception exception, string message = "Exception was thrown:", bool notifyOwner = true)
        {
            LogRequested(this, new LogEventArgs(
                $"{DateTime.Now} [Exception] {message}\n------ START OF EXCEPTION ------\n\n{exception}\n\n------ END OF EXCEPTION ------"));

            if (notifyOwner)
                _ = ApplicationInfo.Owner.SendMessageAsync($"{message}\n```\n{exception}\n```");
        }

        public async Task ReadyAsync()
        {
            await Data.InitializeAsync();
            await Client.SetStatusAsync(UserStatus.Online);
            await Client.SetGameAsync("!wow help");
        }

        public async Task JoinedGuildAsync(SocketGuild guild)
        {
            var guildData = await Data.EnsureGuildDataExistsAsync(guild.Id);

            // Only set if it's the first time the bot has joined this guild.
            if (guildData.DateTimeJoinedBinary == 0)
            {
                Data.AllGuildData[guild.Id]
                    .DateTimeJoinedBinary = DateTime.Now.ToBinary();
            }

            await new WelcomeMessage(guild.GetCommandPrefix(this))
                .SendToBestChannelAsync(guild);
        }

        public async Task LeftGuildAsync(SocketGuild guild)
        {
            await Data.UnloadGuildDataAsync(guild.Id);
        }

        public async Task DiscordLogRecievedAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is Exception)
            {
                // Return if command intentionally threw an exception.
                if (logMessage.Exception.InnerException is CommandReturnException)
                    return;

                if (logMessage.Exception is CommandException commandException)
                {
                    string errorMessageText = $"An unhandled exception was thrown.\nWant to let the developer know? [Create an issue on Github.](https://github.com/rednir/wow2/issues/new)\n```{commandException.InnerException.Message}\n```";
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
                    && LogSeverity != LogSeverity.Debug)
                {
                    // Client reconnects after these exceptions, so no need to dm bot owner.
                    LogException(logMessage.Exception, notifyOwner: false);
                }
                else
                {
                    LogException(logMessage.Exception, notifyOwner: true);
                }
            }
            else
            {
                Log(logMessage);
            }
        }

        public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == Client.CurrentUser.Id)
                return;

            IUserMessage message = await cachedMessage.GetOrDownloadAsync();
            if (message == null)
                return;

            if (!await PagedMessage.ActOnReactionAsync(reaction))
                await ResponseMessage.ActOnReactionAddedAsync(this, reaction, message);
        }

        public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == Client.CurrentUser.Id)
                return;

            IUserMessage message = await cachedMessage.GetOrDownloadAsync();
            if (message == null)
                return;

            ResponseMessage.ActOnReactionRemoved(this, reaction, message);
        }

        public Task MessageDeletedAsync(Cacheable<IMessage, ulong> cachedMessage, ISocketMessageChannel channel)
        {
            PagedMessage.FromMessageId(cachedMessage.Id)?.Dispose();
            return Task.CompletedTask;
        }

        public async Task MessageRecievedAsync(SocketMessage socketMessage)
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
            await Data.EnsureGuildDataExistsAsync(context.Guild.Id);
            await ActOnMessageAsync(context);
        }

        public async Task<IResult> ExecuteCommandAsync(SocketCommandContext context, string input)
        {
            using var _ = context.Channel.EnterTypingState();

            if (ModeratorModule.CheckForCommandAbuse(context, this))
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

        public async Task SendErrorMessageToChannel(CommandError? commandError, SocketCommandContext context)
        {
            string commandPrefix = context.Guild.GetCommandPrefix(this);

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

        private async Task<IEnumerable<CommandInfo>> SearchCommandsAsync(SocketCommandContext context, string term)
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

        private async Task ActOnMessageAsync(SocketCommandContext context)
        {
            SocketUserMessage message = context.Message;

            // Only auto mod message if not related to a game.
            if (!await CountingGame.CheckMessageAsync(context, this)
                && !await VerbalMemoryGame.CheckMessageAsync(context, this))
            {
                await ModeratorModule.CheckMessageWithAutoMod(context, this);
            }

            if (message.Content.StartsWithWord(
                context.Guild.GetCommandPrefix(this), true))
            {
                // The message starts with the command prefix and the prefix is not part of another word.
                await ActOnMessageAsCommandAsync(context);
                return;
            }
            else if (!await MainModule.TryExecuteAliasAsync(context, this))
            {
                // Only check for keyword when the message is not an alias/command.
                KeywordsModule.CheckMessageForKeyword(context, this);
                return;
            }
        }

        private async Task ActOnMessageAsCommandAsync(SocketCommandContext context)
        {
            string commandPrefix = context.Guild.GetCommandPrefix(this);

            if (context.Message.Content == commandPrefix)
            {
                await new AboutMessage(commandPrefix, ApplicationInfo, Client.Guilds.Count)
                    .SendAsync(context.Channel);
                return;
            }

            await ExecuteCommandAsync(
                context,
                context.Message.Content.MakeCommandInput(commandPrefix));
        }

        private void StartPollTask(Func<BotService, Task> func, int intervalMinutes)
        {
            var timer = new Timer(intervalMinutes * 60000) { AutoReset = true };
            timer.Elapsed += async (source, e) =>
            {
                try
                {
                    await func.Invoke(this);
                    Log($"Finished running polling service '{func.Method.Name}'.", LogSeverity.Debug);
                }
                catch (Exception ex)
                {
                    LogException(ex, $"Exception thrown when running polling service '{func.Method.Name}'");
                }
            };

            timer.Start();
            Log($"Started polling service '{func.Method.Name}', set to run every {intervalMinutes} minutes.", LogSeverity.Debug);
        }
    }
}
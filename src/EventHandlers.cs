using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Modules;
using ExtentionMethods;

namespace wow2
{
    public static class EventHandlers
    {
        public static string CommandPrefix { get; } = "!wow";
        public static CommandService BotCommandService;

        public static async Task InstallCommandsAsync()
        {
            BotCommandService = new CommandService();
            BotCommandService.Log += LogAsync;
            await BotCommandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
        }

        public static async Task ReadyAsync()
        {
            await Program.Client.SetGameAsync("!wow help");
        }

        public static async Task LogAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is CommandException commandException)
            {
                Console.WriteLine($"Log: [{logMessage.Source}: {logMessage.Severity}] Command '{commandException.Command.Name}' threw an exception in guild '{commandException.Context.Guild.Name}' due to message '{commandException.Context.Message.Content}'");
                Console.WriteLine($" ------ START OF EXCEPTION ------\n{commandException}\n------ END OF EXCEPTION ------");

                // Also notify the guild that this error happened in.
                await commandException.Context.Channel.SendMessageAsync(
                    embed: MessageEmbedPresets.Verbose($"An unhandled exception was thrown when executing command `{commandException.Command.Name}` and was automatically reported.", VerboseMessageSeverity.Error)
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
        }

        public static async Task CommandRecievedAsync(SocketMessage socketMessage)
        {
            var socketUserMessage = (SocketUserMessage)socketMessage;

            // The message is not a user message.
            if (socketMessage == null) return;

            if (socketMessage.Content == CommandPrefix)
            {
                await socketMessage.Channel.SendMessageAsync(
                    embed: MessageEmbedPresets.Verbose($"To view a list of commands, type `{CommandPrefix} help`")
                );
                return;
            }

            IResult result = await BotCommandService.ExecuteAsync
            (
                context: new SocketCommandContext(Program.Client, (SocketUserMessage)socketMessage),
                argPos: CommandPrefix.Length + 1,
                services: null
            );

            switch (result.Error)
            {
                case CommandError.BadArgCount:
                    await socketMessage.Channel.SendMessageAsync(
                        embed: MessageEmbedPresets.Verbose("Invalid usage of command.\nYou typed either too little or too many parameters.", VerboseMessageSeverity.Warning)
                    );
                    break;

                case CommandError.ParseFailed:
                    await socketMessage.Channel.SendMessageAsync(
                        embed: MessageEmbedPresets.Verbose("Invalid usage of command.\nOne or more parameters were incorrect.", VerboseMessageSeverity.Warning)
                    );
                    break;

                case CommandError.UnknownCommand:
                    await socketMessage.Channel.SendMessageAsync(
                        embed: MessageEmbedPresets.Verbose("No such command.\nDid you make a typo?", VerboseMessageSeverity.Warning)
                    );
                    break;
            }
        }
    }
}
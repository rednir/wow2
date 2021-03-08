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
        public static readonly string CommandPrefix = "!wow2";
        private static CommandService Commands;

        public static async Task InstallCommandsAsync()
        {
            Commands = new CommandService();
            Commands.Log += LogAsync;
            await Commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
        }

        public static async Task LogAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is CommandException commandException)
            {
                Console.WriteLine($"Log: [{logMessage.Source}: {logMessage.Severity}] Command '{commandException.Command.Name}' failed in guild '{commandException.Context.Guild.Name}' due to message '{commandException.Context.Message.Content}'");
                Console.WriteLine($" ------ START OF EXCEPTION ------\n{commandException}\n------ END OF EXCEPTION ------");

                // Also notify the guild that this error happened in.
                await commandException.Context.Channel.SendMessageAsync(embed: MessageEmbedPresets.Verbose($"An unhandled exception was thrown and was automatically reported.", VerboseMessageSeverity.Error));
            }
            else
            {
                Console.WriteLine($"Log: [{logMessage.Source}: {logMessage.Severity}] {logMessage.Message}");
            }
        }

        public static async Task MessageRecievedAsync(SocketMessage recievedMessage)
        {
            // TODO: check self id instead
            if (recievedMessage.Author.IsBot) return;

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

            IResult result = await Commands.ExecuteAsync
            (
                context: new SocketCommandContext(Program.Client, (SocketUserMessage)socketMessage),
                argPos: CommandPrefix.Length,
                services: null
            );
            
            switch (result.Error)
            {
                case CommandError.BadArgCount:
                    await socketMessage.Channel.SendMessageAsync(embed: MessageEmbedPresets.Verbose("Invalid usage of command.\nYou typed either too little or too many parameters.", VerboseMessageSeverity.Warning));
                    break;

                case CommandError.UnknownCommand:
                    await socketMessage.Channel.SendMessageAsync(embed: MessageEmbedPresets.Verbose("No such command.\nDid you make a typo?", VerboseMessageSeverity.Warning));
                    break;
            }
        }
    }
}
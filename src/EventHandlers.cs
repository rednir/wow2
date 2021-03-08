using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using wow2.CommandModules;

namespace wow2
{
    public static class EventHandlers
    {
        public static readonly string CommandPrefix = "!wow2";
        private static CommandService Commands;

        public static async Task InstallCommandsAsync()
        {
            Commands = new CommandService();
            await Commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
        }

        public static async Task LogAsync(LogMessage logMessage)
        {
            await Task.Run(() => Console.WriteLine($"Log: [{logMessage.Source}: {logMessage.Severity}] {logMessage.Message}"));
        }

        public static async Task MessageRecievedAsync(SocketMessage recievedMessage)
        {
            // TODO: check self id instead
            if (recievedMessage.Author.IsBot) return;

            //await recievedMessage.Channel.SendMessageAsync($"{DataManager.DictionaryOfGuildData[((SocketGuildChannel)recievedMessage.Channel).Id].Keywords.Count}");

            await KeywordsModule.CheckMessageForKeywordAsync(recievedMessage);

            if (recievedMessage.Content.StartsWith(CommandPrefix))
            {
                await CommandRecievedAsync(recievedMessage);
            }
        }

        public static async Task CommandRecievedAsync(SocketMessage socketMessage)
        {
            var socketUserMessage = (SocketUserMessage)socketMessage;

            // The message is not a user message.
            if (socketMessage == null) return;

            await Commands.ExecuteAsync
            (
                context: new SocketCommandContext(Program.Client, (SocketUserMessage)socketMessage),
                argPos: CommandPrefix.Length,
                services: null
            );
        }
    }
}
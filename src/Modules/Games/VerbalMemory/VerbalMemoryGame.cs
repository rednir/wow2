using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Data;
using wow2.Extentions;
using wow2.Verbose.Messages;

namespace wow2.Modules.Games.VerbalMemory
{
    public static class VerbalMemoryGame
    {
        public const string SeenKeyword = "s";
        public const string NewKeyword = "n";

        public static VerbalMemoryGameConfig GetConfigForGuild(IGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Games.VerbalMemory;

        /// <summary>Checks whether a user message is part of the verbal memory game, and acts on it if so.</summary>
        /// <returns>True if the message was related to the game.</returns>
        public static async Task<bool> CheckMessageAsync(SocketMessage receivedMessage)
        {
            var config = GetConfigForGuild(receivedMessage.GetGuild());

            if (!config.IsGameStarted)
                return false;

            if (receivedMessage.Channel != config.InitalContext.Channel
                || receivedMessage.Author != config.InitalContext.User)
            {
                return false;
            }

            string currentWord = config.CurrentWordMessage.Content;
            switch (receivedMessage.Content)
            {
                case NewKeyword:
                    if (config.SeenWords.Contains(currentWord))
                    {
                        await new InfoMessage(
                            description: "You've seen that word before.",
                            title: "Wrong!")
                                .SendAsync((ISocketMessageChannel)config.InitalContext.Channel);
                        await EndGameAsync(config);
                        return true;
                    }
                    else
                    {
                        config.SeenWords.Add(currentWord);
                        config.UnseenWords.Remove(currentWord);
                        break;
                    }

                case SeenKeyword:
                    if (config.SeenWords.Contains(currentWord))
                    {
                        break;
                    }
                    else
                    {
                        await new InfoMessage(
                            description: "You haven't seen that word before.",
                            title: "Wrong!")
                                .SendAsync((ISocketMessageChannel)config.InitalContext.Channel);
                        await EndGameAsync(config);
                        return true;
                    }

                default:
                    return false;
            }

            config.Turns++;
            await NextWordAsync(config);
            await receivedMessage.DeleteAsync();
            return true;
        }

        public static async Task StartGame(SocketCommandContext context)
        {
            var config = GetConfigForGuild(context.Guild);

            // TODO: need to find a better way of doing this
            var defaultConfig = new VerbalMemoryGameConfig();
            config.CurrentWordMessage = defaultConfig.CurrentWordMessage;
            config.SeenWords = defaultConfig.SeenWords;
            config.UnseenWords = defaultConfig.UnseenWords;
            config.Turns = defaultConfig.Turns;

            config.InitalContext = context;
            config.IsGameStarted = true;

            await new InfoMessage(
                description: $"After every word, respond with:\n • `{SeenKeyword}` if you have seen the word previously\n • `{NewKeyword}` if the word is new",
                title: $"Verbal memory has started for {context.User.Mention}")
                    .SendAsync(context.Channel);

            await NextWordAsync(config);
        }

        private static async Task NextWordAsync(VerbalMemoryGameConfig config)
        {
            var random = new Random();

            bool pickSeenWord = (random.NextDouble() >= 0.5) && (config.SeenWords.Count > 3);
            string currentWord = pickSeenWord ?
                config.SeenWords[random.Next(config.SeenWords.Count)] :
                config.UnseenWords[random.Next(config.UnseenWords.Count)];

            // Check if it's necessary to send a new message.
            if (config.CurrentWordMessage == null)
            {
                config.CurrentWordMessage = await config.InitalContext.Channel.SendMessageAsync(currentWord);
            }
            else
            {
                await config.CurrentWordMessage.ModifyAsync(message => message.Content = currentWord);
            }
        }

        private static async Task EndGameAsync(VerbalMemoryGameConfig config)
        {
            await new GenericMessage(
                description: $"You got `{config.Turns}` points, with `{config.SeenWords.Count}` different words.",
                title: "📈 Final Stats")
                    .SendAsync((ISocketMessageChannel)config.InitalContext.Channel);

            config.IsGameStarted = false;
        }
    }
}
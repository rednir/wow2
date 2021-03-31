using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using wow2.Verbose;
using wow2.Extentions;
using wow2.Data;

namespace wow2.Modules.Games
{
    public class VerbalMemoryGame
    {
        public const string SeenKeyword = "s";
        public const string NewKeyword = "n";

        public static async Task CheckMessageAsync(SocketMessage receivedMessage)
        {
            var config = DataManager.GetGamesConfigForGuild(receivedMessage.GetGuild()).VerbalMemory;
            
            if (config.CurrentWordMessage == null) return;

            if (receivedMessage.Channel != config.InitalContext.Channel
                || receivedMessage.Author != config.InitalContext.User) return;

            string currentWord = config.CurrentWordMessage.Content;
            switch (receivedMessage.Content)
            {
                case NewKeyword:
                    if (config.SeenWords.Contains(currentWord))
                    {
                        await GenericMessenger.SendInfoAsync(
                            channel: (ISocketMessageChannel)config.InitalContext.Channel, 
                            description: "You've seen that word before.",
                            title: "Wrong!");
                        await EndGameAsync(config);
                        return;
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
                        await GenericMessenger.SendInfoAsync(
                            channel: (ISocketMessageChannel)config.InitalContext.Channel, 
                            description: "You haven't seen that word before.",
                            title: "Wrong!");
                        await EndGameAsync(config);
                        return;
                    }

                default:
                    return;
            }

            config.Turns++;
            await config.CurrentWordMessage.DeleteAsync();
            await NextWordAsync(config);
        }

        public static async Task StartGame(SocketCommandContext context)
        {
            var config = DataManager.GetGamesConfigForGuild(context.Guild).VerbalMemory;

            config.InitalContext = context;
            await GenericMessenger.SendInfoAsync(context.Channel, $"Every time I send a word, you must respond with:\n • `{SeenKeyword}` if you have seen the word previously\n • `{NewKeyword}` if the word is new", $"Verbal memory has started for {context.User.Mention}");
            await NextWordAsync(config);
        }

        private static async Task NextWordAsync(VerbalMemoryGameConfig config)
        {
            var random = new Random();

            bool pickSeenWord = (random.NextDouble() >= 0.5) && (config.SeenWords.Count() > 3);
            string currentWord = pickSeenWord ? 
                config.SeenWords[random.Next(config.SeenWords.Count())] :
                config.UnseenWords[random.Next(config.UnseenWords.Count())];

            config.CurrentWordMessage = await config.InitalContext.Channel.SendMessageAsync(currentWord);
        }

        private static async Task EndGameAsync(VerbalMemoryGameConfig config)
        {
            await GenericMessenger.SendResponseAsync(
                channel: (ISocketMessageChannel)config.InitalContext.Channel,
                title: "Final Stats",
                description: $"You got `{config.Turns}` points, with `{config.SeenWords.Count}` different words.");

            // TODO: need to find a better way of doing this
            var defaultConfig = new VerbalMemoryGameConfig();
            config.CurrentWordMessage = defaultConfig.CurrentWordMessage;
            config.SeenWords = defaultConfig.SeenWords;
            config.UnseenWords = defaultConfig.UnseenWords;
            config.Turns = defaultConfig.Turns;

            Logger.Log(config.SeenWords.Count());
        }
    }
}
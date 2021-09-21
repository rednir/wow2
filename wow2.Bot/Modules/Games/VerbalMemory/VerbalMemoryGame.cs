using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.VerbalMemory
{
    public static class VerbalMemoryGame
    {
        public static VerbalMemoryGameConfig GetConfig(IGuild guild)
            => DataManager.AllGuildData[guild.Id].Games.VerbalMemory;

        public static async Task EvaluateChoiceAsync(Choice choice, VerbalMemoryGameConfig config)
        {
            switch (choice)
            {
                case Choice.New:
                    if (config.SeenWords.Contains(config.CurrentWord))
                    {
                        await new InfoMessage(
                            description: "You've seen that word before.",
                            title: "Wrong!")
                                .SendAsync((ISocketMessageChannel)config.InitalContext.Channel);
                        await EndGameAsync(config);
                        return;
                    }
                    else
                    {
                        config.SeenWords.Add(config.CurrentWord);
                        config.UnseenWords.Remove(config.CurrentWord);
                        break;
                    }

                case Choice.Seen:
                    if (config.SeenWords.Contains(config.CurrentWord))
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
                        return;
                    }
            }

            config.Turns++;
            await NextWordAsync(config);
        }

        public static async Task StartGame(SocketCommandContext context)
        {
            var config = GetConfig(context.Guild);

            // TODO: need to find a better way of doing this
            var defaultConfig = new VerbalMemoryGameConfig();
            config.CurrentWordMessage = defaultConfig.CurrentWordMessage;
            config.CurrentWord = defaultConfig.CurrentWord;
            config.SeenWords = defaultConfig.SeenWords;
            config.UnseenWords = defaultConfig.UnseenWords;
            config.Turns = defaultConfig.Turns;

            config.InitalContext = context;
            config.IsGameStarted = true;

            config.GameMessage = await new VerbalMemoryMessage(config)
                .SendAsync(context.Channel);

            await NextWordAsync(config);
        }

        private static async Task NextWordAsync(VerbalMemoryGameConfig config)
        {
            var random = new Random();

            bool pickSeenWord = (random.NextDouble() >= 0.5) && (config.SeenWords.Count > 3);
            config.CurrentWord = pickSeenWord ?
                config.SeenWords[random.Next(config.SeenWords.Count)] :
                config.UnseenWords[random.Next(config.UnseenWords.Count)];

            // Check if it's necessary to send a new message.
            if (config.CurrentWordMessage == null)
            {
                config.CurrentWordMessage = await config.InitalContext.Channel.SendMessageAsync($"**{config.Turns + 1}.** {config.CurrentWord}");
            }
            else
            {
                await config.CurrentWordMessage.ModifyAsync(message => message.Content = $"**{config.Turns + 1}.** {config.CurrentWord}");
            }
        }

        private static async Task EndGameAsync(VerbalMemoryGameConfig config)
        {
            await new GenericMessage(
                description: $"You got `{config.Turns}` points, with `{config.SeenWords.Count}` unique words.",
                title: "📈 Final Stats")
                    .SendAsync((ISocketMessageChannel)config.InitalContext.Channel);

            config.IsGameStarted = false;
            config.LeaderboardEntries.Add(new VerbalMemoryLeaderboardEntry(config));
            config.LeaderboardEntries = config.LeaderboardEntries.OrderByDescending(e => e.Points).ToList();

            await config.GameMessage?.ModifyAsync(m => m.Components = null);
        }
    }
}
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
        public const string SeenWordText = "Seen it.";
        public const string NewWordText = "That's new!";

        public static VerbalMemoryGameConfig GetConfig(IGuild guild)
            => DataManager.AllGuildData[guild.Id].Games.VerbalMemory;

        public static async Task<bool> ActOnButtonAsync(SocketMessageComponent component)
        {
            var config = GetConfig(component.Channel.GetGuild());

            if (!config.IsGameStarted || config.GameMessage.Id != component.Message.Id)
                return false;

            if (component.Channel.Id != config.InitalContext.Channel.Id
                || component.User.Id != config.InitalContext.User.Id)
            {
                await component.FollowupAsync(
                    embed: new WarningMessage("You aren't playing this game, get out of here.").Embed,
                    ephemeral: true);
                return true;
            }

            switch (component.Data.CustomId)
            {
                case NewWordText:
                    if (config.SeenWords.Contains(config.CurrentWord))
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
                        config.SeenWords.Add(config.CurrentWord);
                        config.UnseenWords.Remove(config.CurrentWord);
                        break;
                    }

                case SeenWordText:
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
                        return true;
                    }

                default:
                    return false;
            }

            config.Turns++;
            await NextWordAsync(config);
            return true;
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

            config.GameMessage = await new InfoMessage(
                description: "See the word below? Tell me if you've seen it yet or not by pressing the buttons.",
                title: $"Verbal memory has started for {context.User.Mention}")
            {
                Components = new ComponentBuilder()
                    .WithButton(SeenWordText, SeenWordText)
                    .WithButton(NewWordText, NewWordText),
            }
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
            config.LeaderboardEntries.Truncate(100, true);

            await config.GameMessage?.ModifyAsync(m => m.Components = null);
        }
    }
}
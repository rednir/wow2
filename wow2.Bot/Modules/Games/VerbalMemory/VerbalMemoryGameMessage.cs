using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.VerbalMemory
{
    public class VerbalMemoryGameMessage : GameMessage
    {
        public VerbalMemoryGameMessage(SocketCommandContext context, List<VerbalMemoryLeaderboardEntry> leaderboard, GameResourceService resourceService)
            : base(context, leaderboard.Cast<LeaderboardEntry>().ToArray(), resourceService)
        {
        }

        public List<string> SeenWords { get; set; } = new List<string>();

        public string CurrentWord { get; set; }

        private Random Random { get; } = new Random();

        public override async Task StopAsync()
        {
            int index = SubmitGame();
            await new GenericMessage(
                description: $"You got `{Points}` points, with `{SeenWords.Count}` unique words.\nYou're number {index + 1} on the leaderboard!",
                title: "📈 Final Stats")
                    .SendAsync(InitialContext.Channel);

            await base.StopAsync();
            return;
        }

        public override async Task<IUserMessage> SendAsync(IMessageChannel channel)
        {
            await NextWordAsync();
            return await base.SendAsync(channel);
        }

        protected override ActionButton[] ActionButtons => new[]
        {
            new ActionButton()
            {
                Label = "Seen it!",
                Style = ButtonStyle.Primary,
                Action = async component =>
                {
                    if (component.User.Id != InitialContext.User.Id)
                    {
                        await component.FollowupAsync(
                            embed: new WarningMessage("You aren't playing this game, get out of here.").Embed,
                            ephemeral: true);
                        return;
                    }

                    await EvaluateChoiceAsync(Choice.Seen);
                },
            },
            new ActionButton()
            {
                Label = "That's new!",
                Style = ButtonStyle.Primary,
                Action = async component =>
                {
                    if (component.User.Id != InitialContext.User.Id)
                    {
                        await component.FollowupAsync(
                            embed: new WarningMessage("You aren't playing this game, get out of here.").Embed,
                            ephemeral: true);
                        return;
                    }

                    await EvaluateChoiceAsync(Choice.New);
                },
            },
        };

        private async Task EvaluateChoiceAsync(Choice choice)
        {
            switch (choice)
            {
                case Choice.New:
                    if (SeenWords.Contains(CurrentWord))
                    {
                        await new InfoMessage(
                            description: "You've seen that word before.",
                            title: "Wrong!")
                                .SendAsync(InitialContext.Channel);
                        await StopAsync();
                        return;
                    }
                    else
                    {
                        SeenWords.Add(CurrentWord);
                        break;
                    }

                case Choice.Seen:
                    if (SeenWords.Contains(CurrentWord))
                    {
                        break;
                    }
                    else
                    {
                        await new InfoMessage(
                            description: "You haven't seen that word before.",
                            title: "Wrong!")
                                .SendAsync(InitialContext.Channel);
                        await StopAsync();
                        return;
                    }
            }

            Points++;
            await NextWordAsync();
        }

        private async Task NextWordAsync()
        {
            EmbedBuilder = new EmbedBuilder()
            {
                Description = $"**You are currently number `{PlaceInLeaderboard}` on the leaderboard\n**See the word below? Tell me if you've seen it yet or not by pressing the buttons.",
                Title = $"Verbal memory has started for {InitialContext.User.Username}",
                Color = Color.LightGrey,
            };

            bool pickSeenWord = (Random.NextDouble() >= 0.5) && (SeenWords.Count > 3);
            CurrentWord = pickSeenWord ?
                SeenWords[Random.Next(SeenWords.Count)] :
                ResourceService.GetRandomWord();

            Text = $"**{Points + 1}.** {CurrentWord}";
            await UpdateMessageAsync();
        }
    }
}
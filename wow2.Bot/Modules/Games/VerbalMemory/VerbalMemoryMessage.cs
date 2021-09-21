using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Games.VerbalMemory
{
    public class VerbalMemoryMessage : SavedMessage
    {
        public VerbalMemoryMessage(VerbalMemoryGameConfig config)
        {
            Config = config;
            EmbedBuilder = new EmbedBuilder()
            {
                Description = "See the word below? Tell me if you've seen it yet or not by pressing the buttons.",
                Title = $"Verbal memory has started for {config.InitalContext.User}",
                Color = Color.LightGrey,
            };
        }

        public VerbalMemoryGameConfig Config { get; }

        protected override ActionButtons[] ActionButtons => new[]
        {
            new ActionButtons()
            {
                Label = "Seen it!",
                Style = ButtonStyle.Primary,
                Action = async component => await ActOnButtonAsync(Choice.Seen, component),
            },
            new ActionButtons()
            {
                Label = "That's new!",
                Style = ButtonStyle.Primary,
                Action = async component => await ActOnButtonAsync(Choice.New, component),
            },
        };

        private async Task ActOnButtonAsync(Choice choice, SocketMessageComponent component)
        {
            if (!Config.IsGameStarted)
                return;

            if (component.Channel.Id != Config.InitalContext.Channel.Id
                || component.User.Id != Config.InitalContext.User.Id)
            {
                await component.FollowupAsync(
                    embed: new WarningMessage("You aren't playing this game, get out of here.").Embed,
                    ephemeral: true);
                return;
            }

            await VerbalMemoryGame.EvaluateChoiceAsync(choice, Config);
        }
    }
}
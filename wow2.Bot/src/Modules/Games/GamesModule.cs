using System.Threading.Tasks;
using Discord.Commands;
using wow2.Bot.Modules.Games.Counting;
using wow2.Bot.Modules.Games.VerbalMemory;

namespace wow2.Bot.Modules.Games
{
    [Name("Games")]
    [Group("games")]
    [Alias("game")]
    [Summary("For having a bit of fun.")]
    public class GamesModule : Module
    {
        public GamesModule(BotService botService)
            : base(botService)
        {
        }

        [Command("counting")]
        [Alias("count")]
        [Summary("Start counting in a text channel. INCREMENT is the number that will be added each time.")]
        public async Task CountingAsync(float increment = 1)
        {
            await CountingGame.StartGame(Context, BotService, increment);
        }

        [Command("verbal-memory")]
        [Alias("verbal")]
        [Summary("Try remember as many words as you can.")]
        public async Task VerbalMemoryAsync()
        {
            await VerbalMemoryGame.StartGame(Context, BotService);
        }
    }
}
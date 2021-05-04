using System.Threading.Tasks;
using Discord.Commands;
using wow2.Modules.Games.VerbalMemory;
using wow2.Modules.Games.Counting;
using wow2.Modules.Games.NumberMemory;

namespace wow2.Modules.Games
{
    [Name("Games")]
    [Group("games")]
    [Alias("game")]
    [Summary("For having a bit of fun.")]
    public class GamesModule : ModuleBase<SocketCommandContext>
    {
        [Command("counting")]
        [Alias("count")]
        [Summary("Start counting in a text channel. INCREMENT is the number that will be added each time.")]
        public async Task CountingAsync(float increment = 1)
        {
            await CountingGame.StartGame(Context, increment);
        }

        [Command("verbal-memory")]
        [Alias("verbal")]
        [Summary("Try remember as many words as you can.")]
        public async Task VerbalMemoryAsync()
        {
            await VerbalMemoryGame.StartGame(Context);
        }

        [Command("number-memory")]
        [Alias("number")]
        [Summary("Try remember the largest number possible.")]
        public async Task NumberMemory()
        {
            await NumberMemoryGame.StartGame(Context);
        }
    }
}
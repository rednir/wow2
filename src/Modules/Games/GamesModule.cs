using System.Threading.Tasks;
using Discord.Commands;

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
            await Counting.StartGame(Context, increment);
        }

        [Command("verbal-memory")]
        [Alias("verbal")]
        [Summary("Try remember as many words as you can, discerning words you have seen before from new words")]
        public async Task VerbalMemoryAsync()
        {
            await VerbalMemory.StartGame(Context);
        }
    }
}
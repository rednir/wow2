using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using wow2.Bot.Data;
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
        private GamesModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Games;

        [Command("counting")]
        [Alias("count")]
        [Summary("Start counting in a text channel. INCREMENT is the number that will be added each time.")]
        public async Task CountingAsync(float increment = 1)
        {
            await CountingGame.StartGame(Context, increment);
        }

        [Command("counting-leaderboard")]
        [Alias("count-leaderboard", "countingleaderboard", "countleaderboard")]
        [Summary("Shows the leaderboard for the counting game.")]
        public async Task CountingLeaderboardAsync(int page = 1)
        {
            await new CountingLeaderboardMessage(Config.Counting.LeaderboardEntries, page)
                .SendAsync(Context.Channel);
        }

        [Command("verbal-memory")]
        [Alias("verbal")]
        [Summary("Try remember as many words as you can.")]
        public async Task VerbalMemoryAsync()
        {
            await VerbalMemoryGame.StartGame(Context);
        }
    }
}
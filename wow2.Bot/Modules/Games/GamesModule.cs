using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using wow2.Bot.Data;
using wow2.Bot.Modules.Games.Counting;
using wow2.Bot.Modules.Games.VerbalMemory;
using wow2.Bot.Modules.Games.Typing;

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
            var message = new CountingGameMessage(Context, increment);
            message.SubmitGame = () =>
            {
                var entry = new CountingLeaderboardEntry(message);
                Config.CountingLeaderboard.Add(entry);
                Config.CountingLeaderboard = Config.CountingLeaderboard.OrderByDescending(e => e.Points).ToList();
            };

            await message.SendAsync(Context.Channel);
        }

        [Command("counting-leaderboard")]
        [Alias("count-leaderboard", "countingleaderboard", "countleaderboard")]
        [Summary("Shows the leaderboard for the counting game.")]
        public async Task CountingLeaderboardAsync(int page = 1)
        {
            await new CountingLeaderboardMessage(Config.CountingLeaderboard, page)
                .SendAsync(Context.Channel);
        }

        [Command("verbal-memory")]
        [Alias("verbal")]
        [Summary("Try remember as many words as you can.")]
        public async Task VerbalMemoryAsync()
        {
            var message = new VerbalMemoryGameMessage(Context, Config.VerbalMemoryLeaderboard, Config.GameResourceService);
            message.SubmitGame = () =>
            {
                var entry = new VerbalMemoryLeaderboardEntry(message);
                Config.VerbalMemoryLeaderboard.Add(entry);
                Config.VerbalMemoryLeaderboard = Config.VerbalMemoryLeaderboard.OrderByDescending(e => e.Points).ToList();
            };

            await message.SendAsync(Context.Channel);
        }

        [Command("verbal-memory-leaderboard")]
        [Alias("verbal-leaderboard", "verballeaderboard", "verbalmemoryleaderboard")]
        [Summary("Shows the leaderboard for the counting game.")]
        public async Task VerbalMemoryLeaderboardAsync(int page = 1)
        {
            await new VerbalMemoryLeaderboardMessage(Config.VerbalMemoryLeaderboard, page)
                .SendAsync(Context.Channel);
        }

        [Command("typing")]
        [Alias("typingtest", "typing-test", "type")]
        [Summary("Try to type a set set of words as fast as you can.")]
        public async Task TypingAsync()
        {
            var message = new TypingGameMessage(Context, Config.GameResourceService);
            message.SubmitGame = () =>
            {
                var entry = new TypingLeaderboardEntry(message);
                Config.TypingLeaderboard.Add(entry);
                Config.TypingLeaderboard = Config.TypingLeaderboard.OrderByDescending(e => e.Points).ToList();
            };

            await message.SendAsync(Context.Channel);
        }

        [Command("typing-leaderboard")]
        [Alias("typingleaderboard")]
        [Summary("Shows the leaderboard for the typing game.")]
        public async Task TypingLeaderboardAsync(int page = 1)
        {
            await new TypingLeaderboardMessage(Config.TypingLeaderboard, page)
                .SendAsync(Context.Channel);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Osu
{
    [Name("osu!")]
    [Group("osu")]
    [Summary("Integrations with the osu!api")]
    public class OsuModule : Module
    {
        public IOsuModuleService Service { get; set; }

        public OsuModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Osu;

        [Command("user")]
        [Alias("player")]
        [Summary("Get some infomation about a user.")]
        public async Task UserAsync([Name("USER")] string userInput, string mode = null)
        {
            UserData userData;
            try
            {
                userData = await Service.GetUserAsync(userInput, ParseMode(mode));
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That user doesn't exist.");
            }

            await new UserInfoMessage(userData, await Service.GetUserScoresAsync(userData.id, "best", ParseMode(mode)))
                .SendAsync(Context.Channel);
        }

        [Command("score")]
        [Alias("play")]
        [Summary("Show some infomation about a score.")]
        public async Task ScoreAsync(ulong id, string mode = "osu")
        {
            Score score;
            try
            {
                score = await Service.GetScoreAsync(id, ParseMode(mode));
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That score doesn't exist.");
            }

            await new ScoreMessage(await Service.GetUserAsync(score.user_id.ToString(), score.mode), score)
                .SendAsync(Context.Channel);
        }

        [Command("last")]
        [Alias("recent")]
        [Summary("Shows the most recent score set by a player.")]
        public async Task LastAsync([Name("USER")] string userInput, string mode = null)
        {
            UserData userData;
            try
            {
                userData = await Service.GetUserAsync(userInput, ParseMode(mode));
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That user doesn't exist.");
            }

            Score[] recentScores = await Service.GetUserScoresAsync(userData.id, "recent", ParseMode(mode));

            if (recentScores.Length == 0)
                throw new CommandReturnException(Context, $"{userData.username} hasn't set any scores in the last 24 hours.");

            await new ScoreMessage(userData, recentScores[0])
                .SendAsync(Context.Channel);
        }

        [Command("subscribe")]
        [Alias("sub")]
        [Summary("Toggle whether your server will get notified about USER.")]
        public async Task SubscribeAsync([Name("USER")] string userInput, string mode = null)
        {
            mode = ParseMode(mode);

            UserData userData;
            try
            {
                userData = await Service.GetUserAsync(userInput, mode);
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That user doesn't exist.");
            }

            mode ??= userData.playmode;

            if (Config.SubscribedUsers.RemoveAll(u => u.Id == userData.id && u.Mode == mode) != 0)
            {
                await new SuccessMessage($"You'll no longer get notifications about `{userData.username}` ({mode})")
                    .SendAsync(Context.Channel);
            }
            else
            {
                if (Config.SubscribedUsers.Count > 20)
                    throw new CommandReturnException(Context, "Remove some users before adding more.", "Too many subscribers");

                Score bestScore = (await Service.GetUserScoresAsync(userData.id, "best", mode)).FirstOrDefault();
                Config.SubscribedUsers.Add(new SubscribedUserData(userData, bestScore, mode));

                await new SuccessMessage(Config.AnnouncementsChannelId == 0 ?
                    $"Once you use `set-announcements-channel`, you'll get notifications about `{userData.username}` ({mode})" :
                    $"You'll get notifications about `{userData.username}` ({mode})")
                        .SendAsync(Context.Channel);
            }

            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("list-subs")]
        [Alias("list")]
        [Summary("Lists the users your server will get notified about.")]
        public async Task ListSubsAsync(int page = 1)
        {
            if (Config.SubscribedUsers.Count == 0)
                throw new CommandReturnException(Context, "Add some users to the subscriber list first.", "Nothing to show");

            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (SubscribedUserData user in Config.SubscribedUsers)
            {
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{user.Username} ({user.Mode}) | #{user.GlobalRank}",
                    Value = $"[View profile](https://osu.ppy.sh/users/{user.Id}/{user.Mode})",
                    IsInline = true,
                });
            }

            await new PagedMessage(
                title: "Subscribed Users",
                fieldBuilders: fieldBuilders,
                page: page)
                    .SendAsync(Context.Channel);
        }

        [Command("set-announcements-channel")]
        [Alias("announcements-channel", "set-announce-channel", "set-channel")]
        [Summary("Sets the channel where notifications about users will be sent.")]
        public async Task SetAnnoucementsChannelAsync(SocketTextChannel channel)
        {
            Config.AnnouncementsChannelId = channel.Id;
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);

            await new SuccessMessage($"You'll get osu! announcements in {channel.Mention}")
                .SendAsync(Context.Channel);
        }

        [Command("test-poll")]
        [RequireOwner(Group = "Permission")]
        [Summary("Check for new user milestones.")]
        public async Task TestPollAsync()
        {
            await Service.CheckForUserMilestonesAsync();
            await new SuccessMessage("Done!")
                .SendAsync(Context.Channel);
        }

        /// <summary>Finds the best matching gamemode based on a string.</summary>
        /// <returns>Returns the gamemode identifier as a string, or null if there was no best match.</returns>
        private static string ParseMode(string modeUserInput)
        {
            return modeUserInput?.ToLower() switch
            {
                "osu" or "std" or "standard" or "osu!std" or "osu!standard" => "osu",
                "taiko" or "drums" or "osu!taiko" => "taiko",
                "fruits" or "ctb" or "catch" or "osu!ctb" or "osu!catch" => "fruits",
                "mania" or "osu!mania" => "mania",
                _ => null,
            };
        }
    }
}
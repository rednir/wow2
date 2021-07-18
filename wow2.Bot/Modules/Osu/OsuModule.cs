using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Verbose;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Osu
{
    [Name("osu!")]
    [Group("osu")]
    [Summary("Integrations with the osu!api")]
    public class OsuModule : Module
    {
        public static readonly Dictionary<string, string> ModeStandardNames = new()
        {
            { "osu", "osu!standard" },
            { "taiko", "osu!taiko" },
            { "fruits", "osu!catch" },
            { "mania", "osu!mania" },
        };

        public static readonly Dictionary<string, ulong> ModeEmoteIds = new()
        {
            { "osu", 860176670715936768 },
            { "taiko", 860176882317524992 },
            { "fruits", 860176939620499487 },
            { "mania", 860176789389049907 },
        };

        public static readonly Dictionary<string, IEmote> RankingEmotes = new()
        {
            { "F", Emote.Parse("<:wow2osuF:865666110895161384>") },
            { "D", Emote.Parse("<:wow2osuD:858722080702857236>") },
            { "C", Emote.Parse("<:wow2osuC:858722124613419059>") },
            { "B", Emote.Parse("<:wow2osuB:858722163174801428>") },
            { "A", Emote.Parse("<:wow2osuA:858722183819165716>") },
            { "S", Emote.Parse("<:wow2osuS:858722203423735838>") },
            { "SH", Emote.Parse("<:wow2osuSH:858722220544753685>") },
            { "X", Emote.Parse("<:wow2osuX:858722239620055050>") },
            { "XH", Emote.Parse("<:wow2osuXH:858722266807926794>") },
        };

        private static readonly System.Timers.Timer RefreshAccessTokenTimer = new(18 * 3600000);

        private static readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://osu.ppy.sh/"),
        };

        static OsuModule()
        {
            PollingService.CreateService(CheckForUserMilestonesAsync, 15);

            _ = AuthenticateHttpClient();
            RefreshAccessTokenTimer.Elapsed += (sender, e) => _ = AuthenticateHttpClient();
            RefreshAccessTokenTimer.Start();
        }

        public OsuModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Osu;

        public static string MakeScoreTitle(Score score) =>
            $"{RankingEmotes[score.rank]}  {score.beatmapSet.title} [{score.beatmap.version}] {MakeReadableModsList(score.mods)}";

        public static string MakeScoreDescription(Score score) =>
            $"[More details](https://osu.ppy.sh/scores/osu/{score.id}) | {(score.replay ? $"[Download replay](https://osu.ppy.sh/scores/osu/{score.id}/download) | " : null)}{Math.Round(score.pp ?? 0, 0)}pp • {Math.Round(score.accuracy * 100, 2)}% • {score.max_combo}x";

        public static string MakeReadableModsList(IEnumerable<string> mods) =>
            (mods.Any() ? "+" : null) + string.Join(' ', mods);

        [Command("user")]
        [Alias("player")]
        [Summary("Get some infomation about a user.")]
        public async Task UserAsync([Name("USER")] string userInput, string mode = null)
        {
            UserData userData;
            try
            {
                userData = await GetUserAsync(userInput, ParseMode(mode));
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That user doesn't exist.");
            }

            await new UserInfoMessage(userData, await GetUserScoresAsync(userData.id, "best", ParseMode(mode)))
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
                score = await GetScoreAsync(id, ParseMode(mode));
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That score doesn't exist.");
            }

            await new ScoreMessage(await GetUserAsync(score.user_id.ToString(), score.mode), score)
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
                userData = await GetUserAsync(userInput, ParseMode(mode));
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That user doesn't exist.");
            }

            Score[] recentScores = await GetUserScoresAsync(userData.id, "recent", ParseMode(mode));

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
                userData = await GetUserAsync(userInput, mode);
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

                Score bestScore = (await GetUserScoresAsync(userData.id, "best", mode)).FirstOrDefault();
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
            await CheckForUserMilestonesAsync();
            await new SuccessMessage("Done!")
                .SendAsync(Context.Channel);
        }

        private static async Task AuthenticateHttpClient()
        {
            var tokenRequestParams = new Dictionary<string, string>()
            {
                { "client_id", DataManager.Secrets.OsuClientId },
                { "client_secret", DataManager.Secrets.OsuClientSecret },
                { "grant_type", "client_credentials" },
                { "scope", "public" },
            };

            Dictionary<string, object> tokenRequestResponse;
            try
            {
                tokenRequestResponse = await HttpClient
                    .PostAsync("oauth/token", new FormUrlEncodedContent(tokenRequestParams))
                    .Result.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Exception thrown when attempting to get an osu!api access token.");
                return;
            }

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", tokenRequestResponse["access_token"].ToString());
        }

        private static async Task<UserData> GetUserAsync(string user, string mode = null)
        {
            var userGetResponse = await HttpClient.GetAsync($"api/v2/users/{user}/{mode}");

            // If `user` is a username, the client will be redirected, losing
            // its headers. So another request will need to be made.
            if (userGetResponse.StatusCode == HttpStatusCode.Unauthorized)
                userGetResponse = await HttpClient.GetAsync(userGetResponse.RequestMessage.RequestUri);

            if (!userGetResponse.IsSuccessStatusCode)
                throw new WebException(userGetResponse.StatusCode.ToString());

            return await userGetResponse.Content.ReadFromJsonAsync<UserData>();
        }

        private static async Task<Score[]> GetUserScoresAsync(ulong userId, string type, string mode = null)
        {
            var bestScoresGetResponse = await HttpClient.GetAsync($"api/v2/users/{userId}/scores/{type}?{(mode == null ? null : $"mode={mode}&")}include_fails=1");
            return await bestScoresGetResponse.Content.ReadFromJsonAsync<Score[]>();
        }

        private static async Task<Score> GetScoreAsync(ulong id, string mode)
        {
            var scoreGetResponse = await HttpClient.GetAsync($"api/v2/scores/{mode}/{id}");

            if (!scoreGetResponse.IsSuccessStatusCode)
                throw new WebException(scoreGetResponse.StatusCode.ToString());

            return await scoreGetResponse.Content.ReadFromJsonAsync<Score>();
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

        private static async Task CheckForUserMilestonesAsync()
        {
            Dictionary<UserData, Score> CachedUpdatedUserDataAndBestScore = new();

            foreach (var config in DataManager.AllGuildData.Select(g => g.Value.Osu).ToArray())
            {
                if (config.AnnouncementsChannelId == 0)
                    continue;

                for (int i = 0; i < config.SubscribedUsers.Count; i++)
                {
                    SubscribedUserData subscribedUserData = config.SubscribedUsers[i];

                    // Check if we have already requested data for this user before.
                    KeyValuePair<UserData, Score> cachedPair = CachedUpdatedUserDataAndBestScore
                        .FirstOrDefault(p => p.Key.id == subscribedUserData.Id && p.Value.mode == subscribedUserData.Mode);

                    UserData updatedUserData = cachedPair.Key ?? await GetUserAsync(subscribedUserData.Id.ToString(), subscribedUserData.Mode);
                    Score currentBestScore = cachedPair.Value ?? (await GetUserScoresAsync(subscribedUserData.Id, "best", subscribedUserData.Mode))?.FirstOrDefault();

                    await CheckForNewTopPlayAsync(subscribedUserData, updatedUserData, currentBestScore, config);
                    await CheckForRankMilestoneAsync(subscribedUserData, updatedUserData, config);

                    // Update subscribed user data.
                    config.SubscribedUsers[i] = new SubscribedUserData(updatedUserData, currentBestScore, subscribedUserData.Mode);
                    CachedUpdatedUserDataAndBestScore.TryAdd(updatedUserData, currentBestScore);

                    await Task.Delay(500);
                }
            }
        }

        private static async Task CheckForNewTopPlayAsync(SubscribedUserData subscribedUserData, UserData updatedUserData, Score currentBestScore, OsuModuleConfig config)
        {
            if (!subscribedUserData.BestScore?.Equals(currentBestScore) ?? true)
            {
                // Don't continue if the player has zero plays.
                if (currentBestScore == null)
                    return;

                var textChannel = (SocketTextChannel)BotService.Client.GetChannel(config.AnnouncementsChannelId);
                await textChannel.SendMessageAsync(
                    text: $"**{updatedUserData.username}** just set a new top play, {(int)currentBestScore.pp - (int)(subscribedUserData.BestScore?.pp ?? 0)}pp higher than before!",
                    embed: new ScoreMessage(updatedUserData, currentBestScore).Embed);
            }
        }

        private static async Task CheckForRankMilestoneAsync(SubscribedUserData subscribedUserData, UserData updatedUserData, OsuModuleConfig config)
        {
            if (!updatedUserData.statistics.global_rank.HasValue)
                return;

            string oldRank = subscribedUserData.GlobalRank.GetValueOrDefault().ToString();
            string newRank = updatedUserData.statistics.global_rank.GetValueOrDefault().ToString();

            if (newRank.Length < oldRank.Length)
            {
                var textChannel = (SocketTextChannel)BotService.Client.GetChannel(config.AnnouncementsChannelId);
                await textChannel.SendMessageAsync(
                    text: $"**{updatedUserData.username}** is no longer a {oldRank.Length} digit in {ModeStandardNames[subscribedUserData.Mode]}, but a {newRank.Length} digit! Crazy!\nThis means they are now **top {Math.Pow(10, newRank.Length)}** in the world.",
                    embed: new UserInfoMessage(updatedUserData, await GetUserScoresAsync(updatedUserData.id, "best", subscribedUserData.Mode)).Embed);
            }
        }
    }
}
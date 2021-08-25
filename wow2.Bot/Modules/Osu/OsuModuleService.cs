using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Verbose;

namespace wow2.Bot.Modules.Osu
{
    public class OsuModuleService : IOsuModuleService
    {
        public static Dictionary<string, string> ModeStandardNames { get; } = new()
        {
            { "osu", "osu!standard" },
            { "taiko", "osu!taiko" },
            { "fruits", "osu!catch" },
            { "mania", "osu!mania" },
        };

        public static Dictionary<string, ulong> ModeEmoteIds { get; } = new()
        {
            { "osu", 860176670715936768 },
            { "taiko", 860176882317524992 },
            { "fruits", 860176939620499487 },
            { "mania", 860176789389049907 },
        };

        public static Dictionary<string, IEmote> RankingEmotes { get; } = new()
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

        public static Dictionary<string, IEmote> ModEmotes { get; } = new()
        {
            { "TD", Emote.Parse("<:TD:867017049145475122>") },
            { "SD", Emote.Parse("<:SD:867017027612311573>") },
            { "SO", Emote.Parse("<:SO:867017018262945822>") },
            { "RD", Emote.Parse("<:RD:867016990899830825>") },
            { "NF", Emote.Parse("<:NF:867016970166599710>") },
            { "MR", Emote.Parse("<:MR:867016944527867904>") },
            { "TP", Emote.Parse("<:TP:867017038937063504>") },
            { "RX", Emote.Parse("<:RX:867017009518739487>") },
            { "PF", Emote.Parse("<:PF:867016981264465941>") },
            { "NC", Emote.Parse("<:NC:867016957601775626>") },
            { "HR", Emote.Parse("<:HR:867016899392176208>") },
            { "FL", Emote.Parse("<:FL:867016845541376050>") },
            { "DT", Emote.Parse("<:DT:867016797093363722>") },
            { "AP", Emote.Parse("<:AP:867016751489482804>") },
            { "HT", Emote.Parse("<:HT:867016872520187914>") },
            { "HD", Emote.Parse("<:HD:867016920813273119>") },
            { "FI", Emote.Parse("<:FI:867016853360738326>") },
            { "EZ", Emote.Parse("<:EZ:867016827446886400>") },
            { "DS", Emote.Parse("<:DS:867016774775996436>") },
            { "CN", Emote.Parse("<:CN:867016761701302302>") },
            { "AT", Emote.Parse("<:AT:867016739808346122>") },
            { "9K", Emote.Parse("<:9K:867016686892220426>") },
            { "8K", Emote.Parse("<:8K:867016671335284736>") },
            { "7K", Emote.Parse("<:7K:867016653783171072>") },
            { "6K", Emote.Parse("<:6K:867016634135871489>") },
            { "5K", Emote.Parse("<:5K:867016614612566067>") },
            { "4K", Emote.Parse("<:4K:867016600677515314>") },
            { "3K", Emote.Parse("<:3K:867016575483510784>") },
            { "2K", Emote.Parse("<:2K:867016556898811964>") },
            { "1K", Emote.Parse("<:1K:867016423146651668>") },
        };

        public static string MakeScoreTitle(Score score) =>
            $"{RankingEmotes[score.rank]}  {score.beatmapSet.artist} - {score.beatmapSet.title} [{score.beatmap.version}] {MakeReadableModsList(score.mods)}";

        public static string MakeScoreDescription(Score score) =>
            $"[More details](https://osu.ppy.sh/scores/{score.mode}/{score.id}) | {(score.replay ? $"[Download replay](https://osu.ppy.sh/scores/{score.mode}/{score.id}/download) | " : null)}{Math.Round(score.pp ?? 0, 0)}pp • {Math.Round(score.accuracy * 100, 2)}% • {score.max_combo}x | {score.beatmap.difficulty_rating}\\*";

        public static string MakeReadableModsList(IEnumerable<string> mods)
        {
            string result = string.Empty;
            foreach (string mod in mods)
                result += ModEmotes.ContainsKey(mod) ? ModEmotes[mod] : $"<{mod}>";

            return result;
        }

        private readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://osu.ppy.sh/"),
        };

        private readonly Timer RefreshAccessTokenTimer = new(18 * 3600000);

        private readonly string ClientId;

        private readonly string ClientSecret;

        public OsuModuleService(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;

            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                _ = AuthenticateHttpClient();
                RefreshAccessTokenTimer.Elapsed += (sender, e) => _ = AuthenticateHttpClient();
                RefreshAccessTokenTimer.Start();
                PollingService.CreateService(CheckForUserMilestonesAsync, 10);
            }
        }

        public async Task<Score> GetScoreAsync(ulong id, string mode)
        {
            var scoreGetResponse = await HttpClient.GetAsync($"api/v2/scores/{mode}/{id}");

            if (!scoreGetResponse.IsSuccessStatusCode)
                throw new WebException(scoreGetResponse.StatusCode.ToString());

            Logger.Log($"Got osu! score {id} ({mode})", LogSeverity.Debug);
            return await scoreGetResponse.Content.ReadFromJsonAsync<Score>();
        }

        public async Task<UserData> GetUserAsync(string user, string mode = null)
        {
            var userGetResponse = await HttpClient.GetAsync($"api/v2/users/{user}/{mode}");

            // If `user` is a username, the client will be redirected, losing
            // its headers. So another request will need to be made.
            if (userGetResponse.StatusCode == HttpStatusCode.Unauthorized)
                userGetResponse = await HttpClient.GetAsync(userGetResponse.RequestMessage.RequestUri);

            if (!userGetResponse.IsSuccessStatusCode)
                throw new WebException(userGetResponse.StatusCode.ToString());

            Logger.Log($"Got osu! user data for {user} ({mode})", LogSeverity.Debug);
            return await userGetResponse.Content.ReadFromJsonAsync<UserData>();
        }

        public async Task<Score[]> GetUserScoresAsync(ulong userId, string type, string mode = null)
        {
            var bestScoresGetResponse = await HttpClient.GetAsync($"api/v2/users/{userId}/scores/{type}?{(mode == null ? null : $"mode={mode}&")}include_fails=1");
            Logger.Log($"Got osu! user {type} scores for {userId} ({mode})", LogSeverity.Debug);
            return await bestScoresGetResponse.Content.ReadFromJsonAsync<Score[]>();
        }

        public async Task CheckForUserMilestonesAsync()
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

        private async Task CheckForNewTopPlayAsync(SubscribedUserData subscribedUserData, UserData updatedUserData, Score currentBestScore, OsuModuleConfig config)
        {
            if (!subscribedUserData.BestScore?.Equals(currentBestScore) ?? true)
            {
                // Don't continue if the player has zero plays.
                if (currentBestScore == null)
                    return;

                // Server hasn't finished calculating new top play.
                if (currentBestScore.pp < subscribedUserData.BestScore?.pp)
                    return;

                var textChannel = (SocketTextChannel)BotService.Client.GetChannel(config.AnnouncementsChannelId);
                await textChannel.SendMessageAsync(
                    text: $"**{updatedUserData.username}** just set a new top play, {(int)currentBestScore.pp - (int)(subscribedUserData.BestScore?.pp ?? 0)}pp higher than before!",
                    embed: new ScoreMessage(updatedUserData, currentBestScore).Embed);
            }
        }

        private async Task CheckForRankMilestoneAsync(SubscribedUserData subscribedUserData, UserData updatedUserData, OsuModuleConfig config)
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

        private async Task AuthenticateHttpClient()
        {
            var tokenRequestParams = new Dictionary<string, string>()
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
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
    }
}
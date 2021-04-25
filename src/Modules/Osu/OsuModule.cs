using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using wow2.Verbose;
using wow2.Data;

namespace wow2.Modules.Osu
{
    [Name("osu!")]
    [Group("osu")]
    [Summary("Integrations with the osu!api")]
    public class OsuModule : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://osu.ppy.sh/")
        };

        static OsuModule()
        {
            _ = InitializeHttpClient();
        }

        [Command("user")]
        [Alias("player")]
        [Summary("Get some infomation about a user.")]
        public async Task UserAsync(string user)
        {
            UserData userData;
            try
            {
                userData = await GetUserAsync(user);
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That user doesn't exist.");
            }

            var fieldBuildersForScores = new List<EmbedFieldBuilder>();
            foreach (Score score in userData.BestScores)
            {
                fieldBuildersForScores.Add(new EmbedFieldBuilder()
                {
                    Name = $"{score.beatmapSet.title} [{score.beatmap.version}] {MakeReadableModsList(score.mods)}",
                    Value = $"{Math.Round(score.accuracy * 100, 2)}% • {score.max_combo}x • {Math.Round(score.pp, 0)}pp",
                    IsInline = true
                });
            }

            var embedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{userData.username} | #{userData.statistics.global_rank}",
                    IconUrl = userData.avatar_url,
                    Url = $"https://osu.ppy.sh/users/{userData.id}"
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"Joined: {DateTime.Parse(userData.join_date)}"
                },
                Description = $"**Performance:** {userData.statistics.pp}pp\n**Accuracy:** {Math.Round(userData.statistics.hit_accuracy, 2)}%\n**Time Played:** {userData.statistics.play_time / 3600}h",
                ImageUrl = userData.cover_url,
                Fields = fieldBuildersForScores,
                Color = Color.LightGrey
            };
            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("subscribe")]
        [Alias("sub")]
        [Summary("Toggle whether your server will get notified about USER.")]
        public async Task SubscribeAsync(string user)
        {
            var config = GetConfigForGuild(Context.Guild);
            UserData userData = await GetUserAsync(user);

            config.SubscribedUsers.Add(userData);
        }

        private static async Task InitializeHttpClient()
        {
            var tokenRequestParams = new Dictionary<string, string>()
            {
                {"client_id", DataManager.Secrets.OsuClientId},
                {"client_secret", DataManager.Secrets.OsuClientSecret},
                {"grant_type", "client_credentials"},
                {"scope", "public"}
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
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static async Task<UserData> GetUserAsync(string user)
        {
            var userGetResponse = await HttpClient.GetAsync($"api/v2/users/{user}");

            // If `user` is a username, the client will be redirected, losing
            // its headers. So another request will need to be made. 
            if (userGetResponse.StatusCode == HttpStatusCode.Unauthorized)
                userGetResponse = await HttpClient.GetAsync(userGetResponse.RequestMessage.RequestUri);

            if (!userGetResponse.IsSuccessStatusCode)
                throw new WebException(userGetResponse.StatusCode.ToString());

            var userData = await userGetResponse.Content.ReadFromJsonAsync<UserData>();
            var bestScoresGetResponse = await HttpClient.GetAsync($"api/v2/users/{userData.id}/scores/best");
            userData.BestScores = await bestScoresGetResponse.Content.ReadFromJsonAsync<List<Score>>();

            return userData;
        }

        private static string MakeReadableModsList(IEnumerable<string> mods)
            => $"{(mods.Any() ? "+" : null)}{string.Join(' ', mods)}";

        public static OsuModuleConfig GetConfigForGuild(IGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Osu;
    }
}
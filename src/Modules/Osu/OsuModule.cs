using System;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;
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
            UserData userData = await GetUserAsync(user);
            var embedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{userData.username} | #1234",
                    IconUrl = userData.avatar_url,
                    Url = $"https://osu.ppy.sh/users/{userData.id}"
                },
                //Title = "User Overview",
                Description = $"**Performance:** {userData.statistics.pp}\n**Accuracy:** {Math.Round(userData.statistics.hit_accuracy)}%\n**Time Played:** {userData.statistics.play_time / 3600}h",
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"Joined: {DateTime.Parse(userData.join_date)}"
                },
                ImageUrl = userData.cover_url,
                Color = Color.LightGrey
            };
            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("subscribe")]
        [Alias("sub")]
        [Summary("Toggle whether your server will get notified about USER.")]
        public async Task SubscribeAsync(string user)
        {
            throw new NotImplementedException();
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
            var response = await HttpClient.GetAsync($"api/v2/users/{user}");

            // If `user` is a username, the client will be redirected, losing
            // its headers. So another request will need to be made. 
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                response = await HttpClient.GetAsync(response.RequestMessage.RequestUri);

            return await response.Content.ReadFromJsonAsync<UserData>();
        }
    }
}
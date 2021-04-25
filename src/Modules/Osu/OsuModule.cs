using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
    }
}
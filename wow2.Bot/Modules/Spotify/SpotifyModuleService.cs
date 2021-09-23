using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using wow2.Bot.Verbose;

namespace wow2.Bot.Modules.Spotify
{
    public class SpotifyModuleService : ISpotifyModuleService
    {
        private readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://accounts.spotify.com/"),
        };

        private readonly string ClientId;

        private readonly string ClientSecret;

        public SpotifyModuleService(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;

            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrEmpty(clientSecret))
                PollingService.CreateTask(AuthenticateHttpClient, 60, true);
        }

        public ISpotifyClient Client { get; set; }

        private async Task AuthenticateHttpClient()
        {
            var tokenRequestParams = new Dictionary<string, string>()
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "grant_type", "client_credentials" },
            };

            Dictionary<string, object> tokenRequestResponse;
            try
            {
                tokenRequestResponse = await HttpClient
                    .PostAsync("api/token", new FormUrlEncodedContent(tokenRequestParams))
                    .Result.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Exception thrown when attempting to get an Spotify access token.");
                return;
            }

            Client = new SpotifyClient(tokenRequestResponse["access_token"].ToString());
        }
    }
}
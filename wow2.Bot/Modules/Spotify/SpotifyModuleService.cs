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
using SpotifyAPI.Web;
using wow2.Bot.Data;
using wow2.Bot.Verbose;

namespace wow2.Bot.Modules.Spotify
{
    public class SpotifyModuleService : ISpotifyModuleService
    {
        private readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://accounts.spotify.com/"),
        };

        private readonly Timer RefreshAccessTokenTimer = new(3600 * 1000);

        private readonly string ClientId;

        private readonly string ClientSecret;

        public SpotifyModuleService(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;

            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                _ = AuthenticateHttpClient();
                RefreshAccessTokenTimer.Elapsed += (sender, e) => _ = AuthenticateHttpClient();
                RefreshAccessTokenTimer.Start();
            }
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
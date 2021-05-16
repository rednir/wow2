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
using wow2.Data;
using wow2.Verbose;
using wow2.Verbose.Messages;

namespace wow2.Modules.Osu
{
    [Name("osu!")]
    [Group("osu")]
    [Summary("Integrations with the osu!api")]
    public class OsuModule : Module
    {
        private static readonly Dictionary<string, IEmote> RankingEmotes = new()
        {
            { "D", Emote.Parse("<:osud:838780206747090964>") },
            { "C", Emote.Parse("<:osuc:838780141433257995>") },
            { "B", Emote.Parse("<:osub:838780016278896712>") },
            { "A", Emote.Parse("<:osua:807023193264881664>") },
            { "S", Emote.Parse("<:osus:807023232116981801>") },
            { "SH", Emote.Parse("<:osush:807023257357123595>") },
            { "SS", Emote.Parse("<:osuss:807023277180583958>") },
            { "SSH", Emote.Parse("<:osussh:807023289742262272>") },
        };

        private static readonly System.Timers.Timer RefreshAccessTokenTimer = new(18 * 3600000);

        private static readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://osu.ppy.sh/"),
        };

        private static readonly Thread PollingThread = new(async () =>
        {
            const int delayMins = 15;
            while (true)
            {
                try
                {
                    await Task.Delay(delayMins * 60000);
                    await CheckForUserMilestonesAsync();
                    Logger.Log("Successfully checked osu! user milestones", LogSeverity.Debug);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Failure to check for new osu! user milestones.");
                    await Task.Delay(delayMins * 60000);
                }
            }
        });

        static OsuModule()
        {
            PollingThread.Start();

            _ = AuthenticateHttpClient();
            RefreshAccessTokenTimer.Elapsed += OnRefreshAccessTokenTimerElapsed;
            RefreshAccessTokenTimer.Start();
        }

        [Command("user")]
        [Alias("player")]
        [Summary("Get some infomation about a user.")]
        public async Task UserAsync([Name("USER")] params string[] userSplit)
        {
            UserData userData;
            try
            {
                userData = await GetUserAsync(string.Join(' ', userSplit));
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
                    Name = MakeScoreTitle(score),
                    Value = MakeScoreDescription(score),
                });
            }

            var embedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{userData.username} | #{userData.statistics.global_rank}",
                    IconUrl = userData.avatar_url.StartsWith("http") ? userData.avatar_url : null,
                    Url = $"https://osu.ppy.sh/users/{userData.id}",
                },
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"Joined: {DateTime.Parse(userData.join_date)}",
                },
                Description = $"**Performance:** {userData.statistics.pp}pp\n**Accuracy:** {Math.Round(userData.statistics.hit_accuracy, 2)}%\n**Time Played:** {userData.statistics.play_time / 3600}h",
                ImageUrl = userData.cover_url,
                Fields = fieldBuildersForScores,
                Color = Color.LightGrey,
            };
            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("subscribe")]
        [Alias("sub")]
        [Summary("Toggle whether your server will get notified about USER.")]
        public async Task SubscribeAsync([Name("USER")] params string[] userSplit)
        {
            var config = GetConfigForGuild(Context.Guild);
            UserData userData;
            try
            {
                userData = await GetUserAsync(string.Join(' ', userSplit));
            }
            catch (WebException)
            {
                throw new CommandReturnException(Context, "That user doesn't exist.");
            }

            if (config.SubscribedUsers.RemoveAll(u => u.id == userData.id) != 0)
            {
                await new SuccessMessage($"You'll no longer get notifications about `{userData.username}`")
                    .SendAsync(Context.Channel);
            }
            else
            {
                if (config.SubscribedUsers.Count > 15)
                    throw new CommandReturnException(Context, "Remove some users before adding more.", "Too many subscribers");

                config.SubscribedUsers.Add(userData);

                await new SuccessMessage(config.AnnouncementsChannelId == 0 ?
                    $"Once you use `set-announcements-channel`, you'll get notifications about `{userData.username}`" :
                    $"You'll get notifications about `{userData.username}`.")
                        .SendAsync(Context.Channel);
            }
            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("list-subs")]
        [Alias("list")]
        [Summary("Lists the users your server will get notified about.")]
        public async Task ListSubsAsync(int page = 1)
        {
            var config = GetConfigForGuild(Context.Guild);

            if (config.SubscribedUsers.Count == 0)
                throw new CommandReturnException(Context, "Add some users to the subscriber list first.", "Nothing to show");

            var fieldBuilders = new List<EmbedFieldBuilder>();
            foreach (UserData user in config.SubscribedUsers)
            {
                fieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{user.username} | #{user.statistics.global_rank}",
                    Value = $"[View profile](https://osu.ppy.sh/users/{user.id})",
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
            var config = GetConfigForGuild(Context.Guild);

            config.AnnouncementsChannelId = channel.Id;
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

        private static void OnRefreshAccessTokenTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
            => _ = AuthenticateHttpClient();

        private static async Task AuthenticateHttpClient()
        {
            var tokenRequestParams = new Dictionary<string, string>()
            {
                {"client_id", DataManager.Secrets.OsuClientId},
                {"client_secret", DataManager.Secrets.OsuClientSecret},
                {"grant_type", "client_credentials"},
                {"scope", "public"},
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

        private static async Task CheckForUserMilestonesAsync()
        {
            foreach (var pair in DataManager.DictionaryOfGuildData)
            {
                var config = pair.Value.Osu;

                // Guild hasn't set a announcements channel, so ignore it.
                if (config.AnnouncementsChannelId == 0) continue;

                for (int i = 0; i < config.SubscribedUsers.Count; i++)
                {
                    UserData currentUserData = config.SubscribedUsers[i];
                    UserData updatedUserData = await GetUserAsync(config.SubscribedUsers[i].id.ToString());

                    // Check if top play has changed.
                    if (!currentUserData.BestScores.FirstOrDefault()?
                        .Equals(updatedUserData.BestScores.FirstOrDefault()) ?? false)
                    {
                        config.SubscribedUsers[i] = updatedUserData;
                        await NotifyGuildForNewTopPlayAsync(
                            userData: updatedUserData,
                            channel: (SocketTextChannel)Bot.Client.GetChannel(config.AnnouncementsChannelId));
                        await DataManager.SaveGuildDataToFileAsync(pair.Key);
                    }
                    await Task.Delay(2000);
                }
            }
        }

        private static async Task NotifyGuildForNewTopPlayAsync(UserData userData, SocketTextChannel channel)
        {
            Score score = userData.BestScores[0];
            var embedBuilder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{userData.username} set a new top play!",
                    IconUrl = userData.avatar_url,
                    Url = $"https://osu.ppy.sh/users/{userData.id}",
                },
                Title = MakeScoreTitle(score),
                Description = MakeScoreDescription(score),
                ImageUrl = score.beatmapSet.covers.cover,
                Color = Color.LightGrey,
            };
            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        private static string MakeScoreTitle(Score score)
            => $"{RankingEmotes[score.rank]} {score.beatmapSet.title} [{score.beatmap.version}] {MakeReadableModsList(score.mods)}";

        private static string MakeScoreDescription(Score score)
            => $"[More details](https://osu.ppy.sh/scores/osu/{score.id}) | {Math.Round(score.pp, 0)}pp • {Math.Round(score.accuracy * 100, 2)}% • {score.max_combo}x";

        private static string MakeReadableModsList(IEnumerable<string> mods)
            => $"{(mods.Any() ? "+" : null)}{string.Join(' ', mods)}";

        public static OsuModuleConfig GetConfigForGuild(IGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Osu;
    }
}
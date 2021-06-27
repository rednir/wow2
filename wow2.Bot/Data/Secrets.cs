namespace wow2.Bot.Data
{
    /// <summary>The contents of the secrets.json file.</summary>
    public class Secrets
    {
        public string DiscordBotToken { get; set; } = "Replace this with a bot token from https://discord.com/developers/applications";

        public string GoogleApiKey { get; set; } = "Replace this with a key from https://console.cloud.google.com/apis/credentials";

        public string OsuClientId { get; set; } = "Replace this with a client ID from https://osu.ppy.sh/home/account/edit#new-oauth-application";

        public string OsuClientSecret { get; set; } = "Replace this with a client secret from https://osu.ppy.sh/home/account/edit#new-oauth-application";

        public string RedditAppId { get; set; } = "Replace this with a Reddit App ID from https://www.reddit.com/prefs/apps";

        public string RedditRefreshToken { get; set; } = "Replace this with a Reddit user refresh token";
    }
}
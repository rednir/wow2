
namespace wow2.Data
{
    /// <summary>The contents of the secrets.json file.</summary>
    public class Secrets
    {
        public string DiscordBotToken { get; set; } = "Replace this with a bot token from https://discord.com/developers/applications";
        public string GoogleApiKey { get; set; } = "Replace this with a key from https://console.cloud.google.com/apis/credentials";
    }
}
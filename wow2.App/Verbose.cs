using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using wow2.Bot.Verbose;

using wow2.App;

namespace wow2.Bot.Verbose
{
    public static class Verbose
    {
        static Verbose()
        {
            Directory.CreateDirectory(LogsDirPath);
            Logger.LogRequested += (sender, e) => Console.WriteLine(e.Message);
        }

        public static string AppDataDirPath => Environment.GetEnvironmentVariable("WOW2_APPDATA_FOLDER") ?? $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/wow2";
        public static string LogsDirPath => $"{AppDataDirPath}/Logs";
        public static string LogFilePath => $"{LogsDirPath}/{Program.TimeStarted:yyyy-MM-dd_HH-mm-ss}.log";

        private static HttpClient GithubHttpClient { get; } = new()
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        public static async Task CheckForUpdates()
        {
            // TODO: parse the tag name in a smarter way.
            GithubRelease latestRelease = await GetLatestReleaseAsync();
            if (latestRelease.tag_name != Program.Version && Program.Version.StartsWith("v"))
            {
                Console.WriteLine($"You are not running the latest release! Click here to download: {latestRelease.html_url}");
            }
        }

        private static async Task<GithubRelease> GetLatestReleaseAsync()
        {
            GithubHttpClient.DefaultRequestHeaders.Add("User-Agent", "wow2");
            var response = await GithubHttpClient.GetAsync("repos/rednir/wow2/releases/latest");
            return await JsonSerializer.DeserializeAsync<GithubRelease>(await response.Content.ReadAsStreamAsync());
        }
    }
}
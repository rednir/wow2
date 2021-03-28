using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Discord;
using wow2.Data;

namespace wow2.Verbose
{
    public static class Logger
    {
        private static readonly HttpClient GithubHttpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        public static async Task LogInitialize()
        {
            Output($"wow2 {Program.Version}\nhttps://github.com/rednir/wow2\n-----------------\nRuntime version: {Environment.Version}\n{RuntimeInformation.OSDescription}\n-----------------\n");

            // TODO: parse the tag name in a smarter way.
            GithubRelease latestRelease = await GetLatestReleaseAsync();
            if ((await GetLatestReleaseAsync()).tag_name != Program.Version)
            {
                Log($"You are not running the latest release! Click here to download: {latestRelease.html_url}", LogSeverity.Warning);
            }
        }

        public static void Log(object message, LogSeverity severity = LogSeverity.Debug)
            => Output($"{DateTime.Now} [{severity.ToString()}] {message}");

        public static void Log(LogMessage logMessage)
            => Output($"{DateTime.Now} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Message}");

        public static void LogException(Exception exception, string message = "Exception was thrown:")
            => Output($"{DateTime.Now} [Exception] {message}\n------ START OF EXCEPTION ------\n\n{exception}\n\n------ END OF EXCEPTION ------");

        private static void Output(string message)
        {
            try
            {
                File.AppendAllText($"{DataManager.LogsDirPath}/{Program.TimeStarted.ToString("yyyy-MM-dd_HH-mm-ss")}.log", message + "\n");
            }
            catch { }
            finally
            {
                Console.WriteLine(message);
            }
        }

        private static async Task<GithubRelease> GetLatestReleaseAsync()
        {
            GithubHttpClient.DefaultRequestHeaders.Add("User-Agent", "wow2");
            var response = await GithubHttpClient.GetAsync("repos/rednir/wow2/releases/latest");
            var latestRelease = await JsonSerializer.DeserializeAsync<GithubRelease>(await response.Content.ReadAsStreamAsync());

            return latestRelease;
        }
    }

    /// <summary>What the Github GET request content for checking for a new release will be deserialized into.</summary>
    public class GithubRelease
    {
        public string tag_name { get; set; }
        public string html_url { get; set; }
    }
}
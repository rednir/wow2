using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using wow2.Data;

namespace wow2.Verbose
{
    /// <summary>Contains methods used for writing to a log file and the standard output stream.</summary>
    public static class Logger
    {
        public static readonly string LogFilePath = $"{DataManager.LogsDirPath}/{Program.TimeStarted:yyyy-MM-dd_HH-mm-ss}.log";

        private static readonly HttpClient GithubHttpClient = new()
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        public static async Task LogInitialize()
        {
            Output($"wow2 {Program.Version}\nhttps://github.com/rednir/wow2\n-----------------\nRuntime version: {Environment.Version}\n{RuntimeInformation.OSDescription}\n-----------------\n");

            // TODO: parse the tag name in a smarter way.
            GithubRelease latestRelease = await GetLatestReleaseAsync();
            if (latestRelease.tag_name != Program.Version && Program.Version.StartsWith("v"))
            {
                Log($"You are not running the latest release! Click here to download: {latestRelease.html_url}", LogSeverity.Warning);
            }
        }

        public static void Log(object message, LogSeverity severity = LogSeverity.Debug)
        {
            if (severity == LogSeverity.Debug && !Program.IsDebug) return;
            Output($"{DateTime.Now} [{severity}] {message}");
        }

        public static void Log(LogMessage logMessage)
        {
            if (logMessage.Severity == LogSeverity.Debug && !Program.IsDebug) return;
            Output($"{DateTime.Now} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Message}");
        }

        public static void LogException(Exception exception, string message = "Exception was thrown:", bool notifyOwner = true)
        {
            Output($"{DateTime.Now} [Exception] {message}\n------ START OF EXCEPTION ------\n\n{exception}\n\n------ END OF EXCEPTION ------");
            if (notifyOwner)
                _ = Bot.ApplicationInfo.Owner.SendMessageAsync($"```\n{exception}\n```");
        }

        public static async Task<string> GetLogsForSessionAsync()
            => await File.ReadAllTextAsync(LogFilePath);

        private static void Output(string message)
        {
            try
            {
                File.AppendAllText(LogFilePath, message + "\n");
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
            Log($"Got latest github release with tag {latestRelease.tag_name}", LogSeverity.Debug);
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
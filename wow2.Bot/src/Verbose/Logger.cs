using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using wow2.Bot.Data;

namespace wow2.Bot.Verbose
{
    /// <summary>Contains methods used for writing to a log file and the standard output stream.</summary>
    public static class Logger
    {
        public static event EventHandler<LogEventArgs> LogRequested;

        private static HttpClient GithubHttpClient { get; } = new()
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        public static void Log(object message, LogSeverity severity = LogSeverity.Debug)
        {
            if (severity == LogSeverity.Debug
                && BotService.LogSeverity != LogSeverity.Debug)
            {
                return;
            }

            Output($"{DateTime.Now} [{severity}] {message}");
        }

        public static void Log(LogMessage logMessage)
        {
            if (logMessage.Severity == LogSeverity.Debug
                && BotService.LogSeverity != LogSeverity.Debug)
            {
                return;
            }

            Output($"{DateTime.Now} [{logMessage.Severity}] {logMessage.Source}: {logMessage.Message}");
        }

        public static void LogException(Exception exception, string message = "Exception was thrown:", bool notifyOwner = true)
        {
            Output($"{DateTime.Now} [Exception] {message}\n------ START OF EXCEPTION ------\n\n{exception}\n\n------ END OF EXCEPTION ------");
            if (notifyOwner)
                _ = BotService.ApplicationInfo.Owner.SendMessageAsync($"{message}\n```\n{exception}\n```");
        }

        public static async Task<string> GetLogsForSessionAsync() =>
            throw new NotImplementedException();
            //await File.ReadAllTextAsync(LogFilePath);

        private static void Output(string message)
        {
            // Need to make this class instance in BotService.
            LogRequested(null, new LogEventArgs(message));
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
}
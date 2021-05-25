using System;
using System.Net.Http;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using wow2.Bot;
using wow2.Bot.Data;
using wow2.Bot.Verbose;

namespace wow2.App
{
    public class Program
    {
        public static string Version => IsDebug ? "DEBUG BUILD" : "v3.0";

        public static DateTime TimeStarted { get; } = DateTime.Now;

        public static string AppDataDirPath { get; } = Environment.GetEnvironmentVariable("WOW2_APPDATA_FOLDER")
            ?? $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/wow2";

        public static string LogsDirPath => $"{AppDataDirPath}/Logs";

        public static string LogFilePath => $"{LogsDirPath}/{TimeStarted:yyyy-MM-dd_HH-mm-ss}.log";

        public static bool IsDebug { get; private set; }

        [Conditional("DEBUG")]
        private static void SetIsDebugField()
            => IsDebug = true;

        private static async Task<Secrets> GetSecretsFromFileAsync(string path)
        {
            var options = new JsonSerializerOptions() { WriteIndented = true, };

            if (!File.Exists(path))
            {
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new Secrets(), options));
                throw new FileNotFoundException($"Couldn't find a secrets file at {path}, so one was created. You MUST have your Discord bot token at the very least in the secrets file in order to run this program.");
            }

            Secrets result = JsonSerializer.Deserialize<Secrets>(File.ReadAllText(path), options);

            // Always rewrite file, just in case there are new properties.
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(result, options));

            return result;
        }

        private static async Task<GithubRelease> GetLatestGithubReleaseAsync()
        {
            var httpClient = new HttpClient() { BaseAddress = new Uri("https://api.github.com/") };

            httpClient.DefaultRequestHeaders.Add("User-Agent", "wow2");
            var response = await httpClient.GetAsync("repos/rednir/wow2/releases/latest");

            // TODO: parse the tag name in a smarter way.
            return await JsonSerializer.DeserializeAsync<GithubRelease>(
                await response.Content.ReadAsStreamAsync());
        }

        private static void Main(string[] args)
            => new Program().MainAsync(args).GetAwaiter().GetResult();

        private async Task MainAsync(string[] args)
        {
            SetIsDebugField();

            Console.WriteLine($"wow2 {Version}\nhttps://github.com/rednir/wow2\n-----------------\nRuntime version: {Environment.Version}\n{RuntimeInformation.OSDescription}\n-----------------\n");

            // Check for updates from Github.
            GithubRelease latestRelease = await GetLatestGithubReleaseAsync();
            if (latestRelease.tag_name != Version && Version.StartsWith("v"))
                Console.WriteLine($"You are not running the latest release! Click here to download: {latestRelease.html_url}");

            // Initialize folders.
            Directory.CreateDirectory(AppDataDirPath);
            Directory.CreateDirectory(LogsDirPath);

            // Start the bot service.
            Secrets secrets = await GetSecretsFromFileAsync(AppDataDirPath + "/secrets.json");
            var botService = new BotService(secrets, AppDataDirPath + "/GuildData");
            await botService.InitializeAndStartClientAsync();

            // Add event handlers.
            botService.LogRequested += OnLogRecieved;

            await Task.Delay(-1);
        }

        private void OnLogRecieved(object sender, LogEventArgs e)
        {
            Console.WriteLine(e.Message);
            File.AppendAllText(LogFilePath, e.Message + "\n");
        }
    }
}

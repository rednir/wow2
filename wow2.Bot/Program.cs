using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using wow2.Bot.Data;
using wow2.Bot.Verbose;

namespace wow2.Bot
{
    public class Program
    {
        public static readonly DateTime TimeStarted = DateTime.Now;
        private const string ReleaseVersion = "v3.0";

        public static string Version => IsDebug ? "DEBUG BUILD" : ReleaseVersion;

        public static string RuntimeDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static bool IsDebug { get; private set; }

        public static CommandLineOptions Options { get; } = new();

        [Conditional("DEBUG")]
        private static void SetIsDebugField()
            => IsDebug = true;

        private static void Main(string[] args)
            => new Program().MainAsync(args).GetAwaiter().GetResult();

        private async Task MainAsync(string[] args)
        {
            SetIsDebugField();
            await Logger.LogInitialize();
            await DataManager.LoadSecretsFromFileAsync();

            Options.Parse(args);

            await BotService.InstallCommandsAsync();
            await BotService.InitializeAndStartClientAsync();

            await Task.Delay(-1);
        }
    }
}

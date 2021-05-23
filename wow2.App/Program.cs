using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using wow2.Bot;
using wow2.Bot.CommandLine;
using wow2.Bot.Data;
using wow2.Bot.Verbose;

namespace wow2.App
{
    public class Program
    {
        public static readonly DateTime TimeStarted = DateTime.Now;
        private const string ReleaseNumber = "v3.0";
        public static string Version => IsDebug ? "DEBUG BUILD" : ReleaseNumber;

        public static bool IsDebug { get; private set; }

        [Conditional("DEBUG")]
        private static void SetIsDebugField()
            => IsDebug = true;

        private static void Main(string[] args)
            => new Program().MainAsync(args).GetAwaiter().GetResult();

        private async Task MainAsync(string[] args)
        {
            SetIsDebugField();

            Console.WriteLine($"wow2 {Version}\nhttps://github.com/rednir/wow2\n-----------------\nRuntime version: {Environment.Version}\n{RuntimeInformation.OSDescription}\n-----------------\n");

            DataManager.AppDataDirPath = Verbose.AppDataDirPath;
            await DataManager.LoadSecretsFromFileAsync();
            await BotService.InstallCommandsAsync();

            if (CommandLineOptions.ParseArgs(args))
                return;

            await BotService.InitializeAndStartClientAsync();

            await Task.Delay(-1);
        }
    }
}

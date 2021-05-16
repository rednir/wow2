using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using wow2.Data;
using wow2.Verbose;

namespace wow2
{
    public class Program
    {
        public static readonly DateTime TimeStarted = DateTime.Now;
        public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
        private const string ReleaseVersion = "v3.0";

        public static string Version => IsDebug ? "DEBUG BUILD" : ReleaseVersion;
        public static string RuntimeDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static bool IsDebug { get; private set; }

        [Conditional("DEBUG")]
        private static void SetIsDebugField()
            => IsDebug = true;

        private static void Main()
            => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            SetIsDebugField();
            await Logger.LogInitialize();
            await DataManager.LoadSecretsFromFileAsync();
            await Bot.InitializeAndStartClientAsync();

            await Task.Delay(-1);
        }
    }
}

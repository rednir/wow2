using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using wow2.Verbose;
using wow2.Data;

namespace wow2
{
    public class Program
    {
        private const string ReleaseVersion = "v3.0";
        private static bool IsDebugField;

        [Conditional("DEBUG")]
        private static void SetIsDebugField()
            => IsDebugField = true;

        public static readonly DateTime TimeStarted = DateTime.Now;
        public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

        public static bool IsDebug
        {
            get { return IsDebugField; }
        }

        public static string Version
        {
            get { return IsDebug ? "DEBUG BUILD" : ReleaseVersion; }
        }

        public static string RuntimeDirectory
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

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

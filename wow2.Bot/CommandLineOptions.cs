using System;
using System.IO;
using CliOptions;
using Discord;
using wow2.Bot.Verbose;

namespace wow2.Bot
{
    public class CommandLineOptions : ArgumentsParser
    {
        [MethodOption("help", 'h', Description = "Displays a list of options and exits.")]
        public void Help()
        {
            Console.WriteLine("Application options:\n" + HelpText);
            Environment.Exit(0);
        }

        [MethodOption("commands", 'c', Description = "Writes a list of commands to a markdown file.")]
        public void Commands()
        {
            string md = BotService.MakeCommandsMarkdown();
            File.WriteAllText("COMMANDS.md", md);
            Logger.Log($"Wrote to {Path.GetFullPath("COMMANDS.md")}", LogSeverity.Info);
        }
    }
}
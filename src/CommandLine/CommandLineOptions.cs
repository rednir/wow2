using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Discord;
using wow2.Verbose;

namespace wow2.CommandLine
{
    public class CommandLineOptions
    {
        public static IEnumerable<MethodInfo> OptionMethods { get; } = typeof(CommandLineOptions).GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(OptionAttribute), false).Length > 0);

        /// <summary>Executes a method with a matching attribute.</summary>
        /// <returns>True if the user specified an option.</summary>
        public static bool ParseArgs(string[] args)
        {
            if (args.Length == 0)
                return false;

            foreach (MethodInfo method in OptionMethods)
            {
                var attribute = GetOptionAttribute(method);
                if (args[0] == "--" + attribute.LongName || args[0] == "-" + attribute.ShortName)
                {
                    Logger.Log($"Found option '{attribute.LongName}'", LogSeverity.Debug);
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), null, method);
                    action.Invoke();
                    return true;
                }
            }

            Logger.Log("Invalid option.", LogSeverity.Error);
            return true;
        }

        [Option("help", 'h')]
        public void Help()
        {
            var stringBuilder = new StringBuilder("\nApplication options:\n");
            foreach (MethodInfo method in OptionMethods)
            {
                var attribute = GetOptionAttribute(method);
                stringBuilder
                    .Append("  ")
                    .Append(attribute.ShortName == default ? string.Empty : $"-{attribute.ShortName}, ")
                    .Append("--")
                    .Append(attribute.LongName)
                    .Append("\t\t\t")
                    .AppendLine(attribute.Description);
            }

            Console.WriteLine(stringBuilder.ToString());
        }

        private static OptionAttribute GetOptionAttribute(MethodInfo method) =>
            (OptionAttribute)method.GetCustomAttribute(typeof(OptionAttribute));
    }
}
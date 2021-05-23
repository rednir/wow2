using System;
using System.Linq;
using System.Reflection;
using wow2.Verbose;

namespace wow2.CommandLine
{
    public class CommandLineOptions
    {
        /// <summary>Executes a method with a matching attribute.</summary>
        /// <returns>True if a matching method was found.</summary>
        public static bool ParseArgs(string[] args)
        {
            if (args.Length == 0)
                return false;

            var optionMethods = typeof(CommandLineOptions).GetMethods().Where(
                m => m.GetCustomAttributes(typeof(OptionAttribute), false).Length > 0);

            foreach (MethodInfo method in optionMethods)
            {
                var attribute = (OptionAttribute)method.GetCustomAttribute(typeof(OptionAttribute));
                if (args[0] == "--" + attribute.LongName || args[0] == "-" + attribute.ShortName)
                {
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), null, method);
                    action.Invoke();
                    return true;
                }
            }

            return false;
        }
    }
}
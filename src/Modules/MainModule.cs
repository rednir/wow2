using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ExtentionMethods;
using wow2;

namespace wow2.Modules
{
    [Name("Main")]
    public class MainModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync(embed: MessageEmbedPresets.GenericResponse(CommandInfoIntoMessage(), "Help"));
        }

        private string CommandInfoIntoMessage()
        {
            // Sort the commands into a dictionary
            var commandsSortedByModules = new Dictionary<ModuleInfo, List<CommandInfo>>();
            foreach (CommandInfo command in EventHandlers.BotCommandService.Commands)
            {
                commandsSortedByModules.TryAdd(command.Module, new List<CommandInfo>());
                commandsSortedByModules[command.Module].Add(command);
            }

            // Create a help text string for each module
            List<string> listOfModuleHelpTexts = new List<string>();
            foreach (ModuleInfo module in commandsSortedByModules.Keys)
            {
                var stringBuilderForModule = new StringBuilder();
                stringBuilderForModule.AppendLine(module.Name);

                stringBuilderForModule.AppendLine("```");
                foreach (CommandInfo command in commandsSortedByModules[module])
                {
                    // Append each command info
                    stringBuilderForModule.AppendLine($"{command.Name}");
                }
                stringBuilderForModule.AppendLine("```");

                listOfModuleHelpTexts.Add(stringBuilderForModule.ToString());
            }

            // TODO: ability to only return one module's help text
            return String.Join("\n", listOfModuleHelpTexts);
        }
    }
}
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
            await ReplyAsync(embed: MessageEmbedPresets.Help(CommandInfoIntoMessage()));
        }

        private List<EmbedFieldBuilder> CommandInfoIntoMessage()
        {
            // Sort the commands into a dictionary
            var commandsSortedByModules = new Dictionary<ModuleInfo, List<CommandInfo>>();
            foreach (CommandInfo command in EventHandlers.BotCommandService.Commands)
            {
                commandsSortedByModules.TryAdd(command.Module, new List<CommandInfo>());
                commandsSortedByModules[command.Module].Add(command);
            }

            // Create a help text string for each module
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (ModuleInfo module in commandsSortedByModules.Keys)
            {
                var fieldBuilderForModule = new EmbedFieldBuilder();
                fieldBuilderForModule.Name = module.Name;

                //fieldBuilderForModule.AppendLine("```");
                foreach (CommandInfo command in commandsSortedByModules[module])
                {
                    // Append each command info
                    fieldBuilderForModule.Value += $"`{EventHandlers.CommandPrefix} {module.Aliases.First()} {command.Aliases.First()}`\n";
                }
                //fieldBuilderForModule.AppendLine("```");

                listOfFieldBuilders.Add(fieldBuilderForModule);
            }

            // TODO: ability to only return one module's help text
            return listOfFieldBuilders;
        }
    }
}
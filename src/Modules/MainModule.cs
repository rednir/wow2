using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ExtentionMethods;

namespace wow2.Modules
{
    [Name("Main")]
    public class MainModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync(
                embed: MessageEmbedPresets.Fields(CommandInfoToEmbedFields(), "Command Help")
            );
        }

        [Command("savedata")]
        public async Task UploadRawGuildData()
        {
            await Context.Channel.SendFileAsync(
                filePath: $"{DataManager.AppDataDirPath}/GuildData/{Context.Message.GetGuild().Id}.json",
                embed: MessageEmbedPresets.Verbose("Successfully uploaded a `.json` file containing all the saved data for this server.", VerboseMessageSeverity.Info)
            );
        }

        private List<EmbedFieldBuilder> CommandInfoToEmbedFields()
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
                var fieldBuilderForModule = new EmbedFieldBuilder()
                {
                    Name = module.Name
                };
                
                foreach (CommandInfo command in commandsSortedByModules[module])
                {
                    // Append each command info
                    string parametersInfo = "";
                    foreach (ParameterInfo parameter in command.Parameters)
                    {
                        // Create a string of the command's parameters
                        string optionalText = !parameter.IsOptional ? "optional:" : "";
                        parametersInfo += $" [{optionalText}{parameter.Name.ToUpper()}]";
                    }
                    fieldBuilderForModule.Value += $"`{EventHandlers.CommandPrefix} {(module.Aliases.FirstOrDefault() == "" ? "" : $"{module.Aliases.First()} ")}{command.Name}{parametersInfo}`\n";
                }

                // TODO: find a way to get name attribute of this class instead of hardcoding module name.
                if (module.Name == "Main")
                {
                    listOfFieldBuilders.Insert(0, fieldBuilderForModule);
                }
                else
                {
                    listOfFieldBuilders.Add(fieldBuilderForModule);
                }
            }

            // TODO: ability to only return one module's help text
            return listOfFieldBuilders;
        }
    }
}
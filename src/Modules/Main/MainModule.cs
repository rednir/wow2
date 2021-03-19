using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using ExtentionMethods;

namespace wow2.Modules.Main
{
    [Name("Main")]
    public class MainModule : ModuleBase<SocketCommandContext>
    {
        [Command("about")]
        public async Task AboutAsync()
        {
            await ShowAboutAsync(Context);
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync(
                embed: MessageEmbedPresets.Fields(await CommandInfoToEmbedFields(), "Command Help")
            );
        }

        [Command("alias")]
        public async Task AliasAsync(string name, string command)
        {
            
        }

        [Command("savedata")]
        public async Task UploadRawGuildData()
        {
            await Context.Channel.SendFileAsync(
                filePath: $"{DataManager.AppDataDirPath}/GuildData/{Context.Guild.Id}.json",
                embed: MessageEmbedPresets.Verbose("Successfully uploaded a `.json` file containing all the saved data for this server.")
            );
        }

        public static async Task ShowAboutAsync(SocketCommandContext context)
        {
            const string githubLink = "https://github.com/rednir/wow2";
            var appInfo = await Program.Client.GetApplicationInfoAsync();

            await context.Channel.SendMessageAsync(
                embed: MessageEmbedPresets.About(
                    name: appInfo.Name,
                    author: appInfo.Owner,
                    description: $"{(string.IsNullOrWhiteSpace(appInfo.Description) ? "" : appInfo.Description)}\n{githubLink}",
                    footer: $" - To view a list of commands, type `{EventHandlers.CommandPrefix} help`"
                )
            );
        }

        private async Task<List<EmbedFieldBuilder>> CommandInfoToEmbedFields()
        {
            // Sort the commands into a dictionary
            var commands = await EventHandlers.BotCommandService.GetExecutableCommandsAsync(new CommandContext(Context.Client, Context.Message), null);
            var commandsSortedByModules = new Dictionary<ModuleInfo, List<CommandInfo>>();
            foreach (CommandInfo command in commands)
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

                if (!String.IsNullOrWhiteSpace(module.Summary))
                    fieldBuilderForModule.Value += $"*{module.Summary}*\n";

                foreach (CommandInfo command in commandsSortedByModules[module])
                {
                    // Append each command info
                    string parametersInfo = "";
                    foreach (ParameterInfo parameter in command.Parameters)
                    {
                        // Create a string of the command's parameters
                        string optionalText = parameter.IsOptional ? "optional:" : "";
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
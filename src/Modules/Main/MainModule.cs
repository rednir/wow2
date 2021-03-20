using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
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
        public async Task HelpAsync([Name("groupname")] string group = null)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                await ReplyAsync(
                    embed: MessageEmbedPresets.Fields(await CommandInfoToEmbedFields(), "All Commands")
                );
            }
            else
            {
                await ReplyAsync(
                    embed: MessageEmbedPresets.Fields(await CommandInfoToEmbedFields(group), $"Command Help")
                );
            }
        }

        [Command("alias")]
        [Alias("aliases")]
        public async Task AliasAsync(string name, string definition)
        {
            var config = DataManager.GetMainConfigForGuild(Context.Guild);

            if (!config.AliasesDictionary.TryAdd(name, definition))
            {
                if (definition != "")
                    throw new CommandReturnException($"The alias `{name}` already exists.\nTo remove the alias, type `{EventHandlers.CommandPrefix} alias \"{name}\" \"\"`", Context);
                
                config.AliasesDictionary.Remove(name);
                await ReplyAsync(
                    embed: MessageEmbedPresets.Verbose($"The alias `{name}` was removed.")
                );
                return;
            }

            if (definition == "")
                throw new CommandReturnException($"An alias should have a definition that isn't blank.", Context);

            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"Typing `{name}` will now execute `{EventHandlers.CommandPrefix} {definition}`")
            );

            await DataManager.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("alias-list")]
        [Alias("alias", "aliases", "list-alias", "list-aliases")]
        public async Task AliasAsync()
        {
            var config = DataManager.GetMainConfigForGuild(Context.Guild);

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var aliasPair in config.AliasesDictionary)
            {
                var fieldBuilderForAlias = new EmbedFieldBuilder()
                {
                    Name = aliasPair.Key,
                    Value = $"`{EventHandlers.CommandPrefix} {aliasPair.Value}`",
                    IsInline = true
                };
                listOfFieldBuilders.Add(fieldBuilderForAlias);
            }

            await ReplyAsync(
                embed: MessageEmbedPresets.Fields(listOfFieldBuilders, "List of Aliases", "To remove any of these aliases, just set the alias definition to \"\"")
            );
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

        public static async Task CheckForAliasAsync(SocketMessage message)
        {
            var config = DataManager.GetMainConfigForGuild(message.GetGuild());

            var aliasesFound = config.AliasesDictionary.Where(a => message.Content.StartsWith(a.Key, true, null));
            if (aliasesFound.Count() != 0)
            {
                var context = new SocketCommandContext(Program.Client, (SocketUserMessage)message);
                var aliasToExecute = aliasesFound.First();

                IResult result = await EventHandlers.BotCommandService.ExecuteAsync
                (
                    context: context,
                    input: aliasToExecute.Value + message.Content.Replace(aliasToExecute.Key, "", true, null),
                    services: null
                );

                if (result.Error.HasValue)
                    await EventHandlers.SendErrorMessageToChannel(result.Error, context.Message.Channel);
            }
        }

        /// <summary>Builds embed fields for all excecutable commands.</summary>
        private async Task<List<EmbedFieldBuilder>> CommandInfoToEmbedFields()
        {
            // Sort the commands into a dictionary
            var commands = await EventHandlers.BotCommandService.GetExecutableCommandsAsync(
                new CommandContext(Context.Client, Context.Message), null
            );
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
            return listOfFieldBuilders;
        }

        /// <summary>Builds embed fields for commands in a single module</summary>
        private async Task<List<EmbedFieldBuilder>> CommandInfoToEmbedFields(string specifiedModuleName)
        {
            // Find commands in module.
            var listOfCommandInfo = (await EventHandlers.BotCommandService.GetExecutableCommandsAsync(
                new CommandContext(Context.Client, Context.Message), null
            ))
            .Where(c => 
                c.Module.Name.Equals(specifiedModuleName, StringComparison.CurrentCultureIgnoreCase)
                || c.Module.Aliases.Contains(specifiedModuleName.ToLower())
            );

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (CommandInfo command in listOfCommandInfo)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"`{EventHandlers.CommandPrefix} {command.Module.Group} {command.Name}`",
                    Value = $"*{(string.IsNullOrWhiteSpace(command.Summary) ? "No description provided." : command.Summary)}*"
                });
            }
            return listOfFieldBuilders;
        }
    }
}
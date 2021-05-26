using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Main
{
    [Name("Main")]
    [Summary("Stuff to do with the bot an other random stuff.")]
    public class MainModule : Module
    {
        protected MainModule(BotService botService)
            : base(botService)
        {
        }

        public MainModuleConfig Config => BotService.Data.AllGuildData[Context.Guild.Id].Main;

        public static async Task<bool> TryExecuteAliasAsync(SocketCommandContext context, BotService botService)
        {
            var config = botService.Data.AllGuildData[context.Guild.Id].Main;
            string messageContent = context.Message.Content;

            var aliasesFound = config.AliasesDictionary.Where(a =>
                messageContent.StartsWithWord(a.Key));

            if (aliasesFound.Any())
            {
                var aliasToExecute = aliasesFound.First();

                await botService.ExecuteCommandAsync(
                    context,
                    aliasToExecute.Value + messageContent.Replace(aliasToExecute.Key, string.Empty, true, null));

                return true;
            }

            return false;
        }

        [Command("about")]
        [Summary("Shows some infomation about the bot.")]
        public async Task AboutAsync()
        {
            await new AboutMessage(
                commandPrefix: Config.CommandPrefix,
                appInfo: BotService.ApplicationInfo,
                guildCount: BotService.Client.Guilds.Count)
                    .SendAsync(Context.Channel);
        }

        [Command("help")]
        [Summary("Displays a list of modules or commands in a specific module.")]
        public async Task HelpAsync([Name("MODULE")] string group = null, int page = 1)
        {
            // Assume user meant page number instead of group if number.
            if (int.TryParse(group, out int groupParameterAsPage))
            {
                page = groupParameterAsPage;
                group = null;
            }

            var commandPrefix = Config.CommandPrefix;
            if (string.IsNullOrWhiteSpace(group))
            {
                await new PagedMessage(
                    fieldBuilders: await ModuleInfoToEmbedFieldsAsync(commandPrefix),
                    description: $"There's {BotService.CommandService.Commands.Count()} total commands for you to play around with.",
                    title: "📃 Help",
                    page: page)
                        .SendAsync(Context.Channel);
            }
            else
            {
                await new PagedMessage(
                    fieldBuilders: await CommandInfoToEmbedFieldsAsync(group, commandPrefix),
                    page: page,
                    title: "📃 Command Help")
                        .SendAsync(Context.Channel);
            }
        }

        [Command("alias")]
        [Alias("aliases")]
        [Summary("Sets an alias. Typing the NAME of an alias will execute '!wow DEFINITION' as a command. Set the DEFINITION of an alias to blank to remove it.")]
        public async Task AliasAsync(string name, [Name("DEFINITION")] params string[] definitionSplit)
        {
            string removeAliasText = $"To remove the alias, type `{Config.CommandPrefix} alias \"{name}\"`";
            string definition = string.Join(" ", definitionSplit);

            // Remove command prefix from user input as it might cause confusion.
            if (definition.StartsWithWord(Config.CommandPrefix, true))
                definition = definition.MakeCommandInput(Config.CommandPrefix);

            if (Config.AliasesDictionary.ContainsKey(name))
            {
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    // Assume the user wants to change the existing alias' definition.
                    Config.AliasesDictionary[name] = definition;
                }
                else
                {
                    // Assume the user wants to remove the existing alias.
                    Config.AliasesDictionary.Remove(name);
                    await new SuccessMessage($"The alias `{name}` was removed.")
                        .SendAsync(Context.Channel);
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(definition))
                    throw new CommandReturnException(Context, "An alias should have a definition that isn't blank.");

                Config.AliasesDictionary.Add(name, definition);
            }

            await new SuccessMessage($"Typing `{name}` will now execute `{Config.CommandPrefix} {definition}`\n{removeAliasText}")
                .SendAsync(Context.Channel);
            await BotService.Data.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        [Command("alias-list")]
        [Alias("alias", "aliases", "list-alias", "list-aliases")]
        [Summary("Displays a list of aliases.")]
        public async Task AliasListAsync()
        {
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (var aliasPair in Config.AliasesDictionary)
            {
                var fieldBuilderForAlias = new EmbedFieldBuilder()
                {
                    Name = aliasPair.Key,
                    Value = $"`{Config.CommandPrefix} {aliasPair.Value}`",
                    IsInline = true,
                };
                listOfFieldBuilders.Add(fieldBuilderForAlias);
            }

            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                title: "📎 List of Aliases",
                description: "To remove any of these aliases, set the alias definition to a blank value.")
                    .SendAsync(Context.Channel);
        }

        [Command("ping")]
        [Summary("Checks the latency between the message that executes a command, and the response that the bot sends.")]
        public async Task PingAsync()
        {
            IUserMessage pongMessage = await new SuccessMessage("That was about `...`ms", "Pong!")
                .SendAsync(Context.Channel);
            TimeSpan pongTimeSpan = pongMessage.Timestamp.Subtract(Context.Message.Timestamp);

            await pongMessage.ModifyAsync(message
                => message.Embed = new SuccessMessage($"That was about `{pongTimeSpan.Milliseconds}ms`", "Pong!").Embed);
        }

        [Command("upload-raw-data")]
        [Alias("raw-data", "upload-raw")]
        [Summary("Uploads a file containing all the data the bot stores about this server.")]
        public async Task UploadRawGuildDataAsync()
        {
            await Context.Channel.SendFileAsync(
                filePath: $"{BotService.Data.GuildDataDirPath}/{Context.Guild.Id}.json");
        }

        [Command("set-command-prefix")]
        [Alias("set-prefix", "setprefix")]
        [Summary("Change the prefix used to identify commands. '!wow' is the default.")]
        public async Task SetCommandPrefixAsync(string prefix)
        {
            if (prefix.Contains(' ') || string.IsNullOrWhiteSpace(prefix))
                throw new CommandReturnException(Context, "The command prefix must not contain spaces.");

            Config.CommandPrefix = prefix;
            await new SuccessMessage($"Changed command prefix to `{prefix}`")
                .SendAsync(Context.Channel);
            await BotService.Data.SaveGuildDataToFileAsync(Context.Guild.Id);
        }

        /// <summary>Builds embed fields for all command modules.</summary>
        private async Task<List<EmbedFieldBuilder>> ModuleInfoToEmbedFieldsAsync(string commandPrefix)
        {
            var listOfModules = (await BotService.CommandService.GetExecutableCommandsAsync(Context, null))
                .Select(command => command.Module)

                // Remove duplicate modules.
                .GroupBy(module => module)
                .Select(group => group.First());

            // Create a help text string for each module
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            await Task.Run(() =>
            {
                foreach (ModuleInfo module in listOfModules)
                {
                    var fieldBuilderForModule = new EmbedFieldBuilder()
                    {
                        Name = $"• {module.Name} Module",
                    };

                    if (!string.IsNullOrWhiteSpace(module.Summary))
                        fieldBuilderForModule.Value += $"*{module.Summary}*\n";

                    fieldBuilderForModule.Value += $"`{commandPrefix} help {module.Name.ToLower()}`";

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
            });
            return listOfFieldBuilders;
        }

        /// <summary>Builds embed fields for commands in a single module.</summary>
        private async Task<List<EmbedFieldBuilder>> CommandInfoToEmbedFieldsAsync(string specifiedModuleName, string commandPrefix)
        {
            // Find commands in module.
            var listOfCommandInfo = (await BotService.CommandService.GetExecutableCommandsAsync(
                new CommandContext(Context.Client, Context.Message), null))
            .Where(c =>
                c.Module.Name.Equals(specifiedModuleName, StringComparison.CurrentCultureIgnoreCase)
                || c.Module.Aliases.Contains(specifiedModuleName.ToLower()));

            if (!listOfCommandInfo.Any())
                throw new CommandReturnException(Context, "Did you make a typo?", "No such module");

            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            foreach (CommandInfo command in listOfCommandInfo)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = command.MakeFullCommandString(commandPrefix),
                    Value = $"*{(string.IsNullOrWhiteSpace(command.Summary) ? "No description provided." : command.Summary)}*",
                });
            }

            return listOfFieldBuilders;
        }
    }
}
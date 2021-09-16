using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using wow2.Bot.Data;
using wow2.Bot.Extensions;
using wow2.Bot.Verbose.Messages;

namespace wow2.Bot.Modules.Main
{
    [Name("Main")]
    [Summary("Stuff to do with the bot and other random stuff.")]
    public class MainModule : Module
    {
        public static readonly IEmote LikeReactionEmote = new Emoji("👍");
        public static readonly IEmote DislikeReactionEmote = new Emoji("👎");

        public MainModuleConfig Config => DataManager.AllGuildData[Context.Guild.Id].Main;

        public static async Task<bool> TryExecuteAliasAsync(SocketCommandContext context)
        {
            var config = DataManager.AllGuildData[context.Guild.Id].Main;
            string messageContent = context.Message.Content;

            var aliasesFound = config.AliasesDictionary.Where(a =>
                messageContent.StartsWithWord(a.Key));

            if (aliasesFound.Any())
            {
                var aliasToExecute = aliasesFound.First();

                await BotService.ExecuteCommandAsync(
                    context,
                    aliasToExecute.Value + messageContent.Replace(aliasToExecute.Key, string.Empty, true, null));

                return true;
            }

            return false;
        }

        public static async Task CheckMessageAsync(SocketCommandContext context)
        {
            var config = DataManager.AllGuildData[context.Guild.Id].Main;

            if (!DataManager.AllGuildData[context.Guild.Id].Main.VotingEnabledChannelIds.Contains(context.Channel.Id)
                || context.Message.Attachments.Count == 0)
            {
                return;
            }

            _ = context.Message.AddReactionsAsync(new[] { LikeReactionEmote, DislikeReactionEmote });
            config.VotingEnabledAttachments.Add(new VotingEnabledAttachment(context));
        }

        public static bool ActOnReactionAdded(SocketReaction reaction)
        {
            var config = DataManager.AllGuildData[reaction.Channel.GetGuild().Id].Main;

            VotingEnabledAttachment attachment = config.VotingEnabledAttachments.Find(a => a.MessageId == reaction.MessageId);
            if (attachment == null)
                return false;

            if (reaction.Emote.Name == LikeReactionEmote.Name && !attachment.UsersLikedIds.Contains(reaction.UserId))
                attachment.UsersLikedIds.Add(reaction.UserId);
            else if (reaction.Emote.Name == DislikeReactionEmote.Name && !attachment.UsersDislikedIds.Contains(reaction.UserId))
                attachment.UsersDislikedIds.Add(reaction.UserId);
            else
                return false;

            return true;
        }

        public static bool ActOnReactionRemoved(SocketReaction reaction)
        {
            var config = DataManager.AllGuildData[reaction.Channel.GetGuild().Id].Main;

            VotingEnabledAttachment attachment = config.VotingEnabledAttachments.Find(a => a.MessageId == reaction.MessageId);
            if (attachment == null)
                return false;

            if (reaction.Emote.Name == LikeReactionEmote.Name && attachment.UsersLikedIds.Contains(reaction.UserId))
                attachment.UsersLikedIds.Remove(reaction.UserId);
            else if (reaction.Emote.Name == DislikeReactionEmote.Name && attachment.UsersDislikedIds.Contains(reaction.UserId))
                attachment.UsersDislikedIds.Remove(reaction.UserId);
            else
                return false;

            return true;
        }

        [Command("about")]
        [Summary("Shows some infomation about the bot.")]
        public async Task AboutAsync()
        {
            await new AboutMessage(Config.CommandPrefix)
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

        [Command("say")]
        [Summary("Sends a message. That's it.")]
        public async Task SayAsync([Remainder] string message)
            => await ReplyAsync(message);

        [Command("toggle-attachment-voting")]
        [Alias("toggle-voting", "toggle-image-voting", "toggle-video-voting")]
        [Summary("Toggles whether the specified text channel will have thumbs up/down reactions for each new message with attachment posted there.")]
        public async Task ToggleVotingInChannelAsync(SocketTextChannel channel)
        {
            bool currentlyOn = Config.VotingEnabledChannelIds.Contains(channel.Id);
            await SendToggleQuestionAsync(
                currentState: currentlyOn,
                setter: x =>
                {
                    if (x && !currentlyOn)
                        Config.VotingEnabledChannelIds.Add(channel.Id);
                    else if (!x && currentlyOn)
                        Config.VotingEnabledChannelIds.Remove(channel.Id);
                },
                toggledOnMessage: $"Every new message with an attachment in {channel.Mention} will have thumbs up/down reactions added.",
                toggledOffMessage: $"Messages in {channel.Mention} will no longer have thumbs up/down reactions added.");
        }

        [Command("attachment-list")]
        [Alias("list-attachment", "list-attachments", "attachments-list", "attachments", "image-list", "list-images")]
        [Summary("Lists all attachments with voting enabled. SORT can be points/users/date/likes/deletions/values, default is likes.")]
        public async Task AttachmentListAsync(AttachmentSorts sort = AttachmentSorts.Points, int page = 1)
        {
            var listOfFieldBuilders = new List<EmbedFieldBuilder>();
            var attachmentsCollection = getAttachments();

            int num = 1;
            foreach (var attachment in attachmentsCollection)
            {
                listOfFieldBuilders.Add(new EmbedFieldBuilder()
                {
                    Name = $"{num}) {attachment.Points} points",
                    Value = $"[{attachment.FileName}]({attachment.MessageUrl}) by {attachment.AuthorMention} at {attachment.DateTime.ToShortDateString()}\n{attachment.UsersLikedIds.Count} {LikeReactionEmote}   |   {attachment.UsersDislikedIds.Count} {DislikeReactionEmote}",
                });
                num++;
            }

            await new PagedMessage(
                fieldBuilders: listOfFieldBuilders,
                description: $"*There are {attachmentsCollection.Length} attachments with voting enabled, as listed below.*",
                title: "🖼 Voting-enabled Attachments",
                page: page)
                    .SendAsync(Context.Channel);

            VotingEnabledAttachment[] getAttachments()
            {
                return sort switch
                {
                    AttachmentSorts.Users => Config.VotingEnabledAttachments.OrderByDescending(p => p.UsersLikedIds.Concat(p.UsersDislikedIds).Distinct()).ToArray(),
                    AttachmentSorts.Likes => Config.VotingEnabledAttachments.OrderByDescending(p => p.UsersLikedIds.Count).ToArray(),
                    AttachmentSorts.Dislikes => Config.VotingEnabledAttachments.OrderByDescending(p => p.UsersDislikedIds.Count).ToArray(),
                    AttachmentSorts.Points => Config.VotingEnabledAttachments.OrderByDescending(p => p.Points).ToArray(),
                    _ => Config.VotingEnabledAttachments.ToArray(),
                };
            }
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

                    fieldBuilderForModule.Value += $"`{commandPrefix} help {module.Group ?? module.Name.ToLower()}`";

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

        [Command("upload-raw-data")]
        [Alias("raw-data", "upload-raw")]
        [Summary("Uploads a file containing all the data the bot stores about this server.")]
        public async Task UploadRawGuildDataAsync()
        {
            await Context.Channel.SendFileAsync(
                filePath: $"{DataManager.AppDataDirPath}/GuildData/{Context.Guild.Id}.json");
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
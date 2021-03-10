using System;
using System.Threading.Tasks;
using Discord.Commands;
using ExtentionMethods;

namespace wow2.Modules
{
    [Name("Config")]
    [Group("Config")]
    [Alias("config")]
    public class ConfigModule : ModuleBase<SocketCommandContext>
    {
        [Command("toggle-keywords-react-to-delete")]
        [Summary("Toggles whether bot responses to keywords should have a reaction, allowing a user to delete the message.")]
        public async Task ToggleKeywordsReactToDeleteAsync()
        {
            DataManager.GetConfigForGuild(Context.Message.GetGuild()).KeywordsReactToDelete = !DataManager.GetConfigForGuild(Context.Message.GetGuild()).KeywordsReactToDelete;
            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"React to delete is now `{(DataManager.GetConfigForGuild(Context.Message.GetGuild()).KeywordsReactToDelete ? "on" : "off")}` for keyword responses.", VerboseMessageSeverity.Info)
            );
        }
    }
}
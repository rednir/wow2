using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace wow2.Modules.Dev
{
    [Name("Developer")]
    [Group("dev")]
    [RequireOwner(Group = "Permission")]
    public class DevModule : ModuleBase<SocketCommandContext>
    {
        [Command("load-guild-data")]
        public async Task LoadGuildDataAsync()
        {
            await DataManager.LoadGuildDataFromFileAsync();
            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data loaded.")
            );
        }

        [Command("save-guild-data")]
        public async Task SaveGuildDataAsync(bool alsoExit = false)
        {
            await DataManager.SaveGuildDataToFileAsync();
            await ReplyAsync(
                embed: MessageEmbedPresets.Verbose($"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data saved.")
            );
            if (alsoExit) Environment.Exit(0);
        }

        [Command("throw")]
        public Task ThrowAsync()
            => throw new ArgumentException("This is a test error message.");
    }
}
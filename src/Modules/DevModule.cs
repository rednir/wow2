using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace wow2.Modules
{
    [Name("Dev (todo: shouldnt show in help text)")]
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
    }
}
using System;
using System.Threading.Tasks;
using Discord.Commands;
using wow2.Verbose;
using wow2.Data;

namespace wow2.Modules.Dev
{
    [Name("Developer")]
    [Group("dev")]
    [RequireOwner(Group = "Permission")]
    [Summary("For developer things.")]
    public class DevModule : ModuleBase<SocketCommandContext>
    {
        [Command("load-guild-data")]
        public async Task LoadGuildDataAsync()
        {
            await DataManager.LoadGuildDataFromFileAsync();
            await Messenger.SendSuccessAsync(Context.Channel, $"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data loaded.");
        }

        [Command("save-guild-data")]
        public async Task SaveGuildDataAsync(bool alsoExit = false)
        {
            await DataManager.SaveGuildDataToFileAsync();
            await Messenger.SendSuccessAsync(Context.Channel, $"`{DataManager.DictionaryOfGuildData.Count}` guilds has their data saved.");
            if (alsoExit) Environment.Exit(0);
        }
    }
}
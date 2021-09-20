using System.Collections.Generic;
using System.Threading.Tasks;

namespace wow2.Bot.Data
{
    public interface IDataManager
    {
        string GuildDataDirPath { get; }

        string AppDataDirPath { get; }

        string LogsDirPath { get; }

        Dictionary<ulong, GuildData> AllGuildData { get; }

        Secrets Secrets { get; set; }

        Task InitializeAsync();

        Task LoadSecretsFromFileAsync();

        Task LoadGuildDataFromFileAsync();

        Task LoadGuildDataFromFileAsync(ulong guildId);

        Task SaveGuildDataToFileAsync();

        Task SaveGuildDataToFileAsync(ulong guildId);

        Task UnloadGuildDataAsync(ulong guildId);

        Task<GuildData> EnsureGuildDataExistsAsync(ulong guildId);
    }
}
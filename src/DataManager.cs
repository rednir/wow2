using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace wow2
{
    public static class DataManager
    {
        public static string AppDataDirPath { get; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/wow2";
        public static DirectoryInfo AppDataDirInfo { get; set; }

        public static Dictionary<ulong, GuildData> DictionaryOfGuildData { get; set; } = new Dictionary<ulong, GuildData>();

        /// <summary>Creates required directories if necessary and loads all guild data.</summary>
        public static async Task InitializeAsync()
        {
            AppDataDirInfo = Directory.CreateDirectory($"{AppDataDirPath}/GuildData");
            await LoadGuildDataFromFileAsync();
        }

        public static async Task LoadGuildDataFromFileAsync(ulong specifiedGuildId = 0)
        {
            if (specifiedGuildId == 0)
            {
                // Load data for all guilds
                foreach (FileInfo fileInfo in AppDataDirInfo.EnumerateFiles())
                {
                    try
                    {
                        // Remove the ".json" at the end of the filename and convert to ulong.
                        ulong guildId = Convert.ToUInt64(fileInfo.Name.Substring(0, fileInfo.Name.Length - 5));

                        string guildDataJson = await File.ReadAllTextAsync(fileInfo.FullName);
                        DictionaryOfGuildData[guildId] = JsonSerializer.Deserialize<GuildData>(guildDataJson);

                        Console.WriteLine($"Loaded guild data for {guildId} (mass load)");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Failed to load from file {fileInfo.Name} (mass load)");
                    }
                }
            }
            else
            {
                try
                {
                    // Load data for one guild
                    string guildDataJson = await File.ReadAllTextAsync($"{AppDataDirPath}/GuildData/{specifiedGuildId}.json");
                    DictionaryOfGuildData[specifiedGuildId] = JsonSerializer.Deserialize<GuildData>(guildDataJson);

                    Console.WriteLine($"Loaded guild data for {specifiedGuildId} (specific load)");
                }
                catch
                {
                    Console.WriteLine($"Failed to load for guild {specifiedGuildId} (specific load)");
                }
            }
        }

        public static async Task SaveGuildDataToFileAsync(ulong guildId)
        {
            GuildData guildData = new GuildData();
            foreach (ulong id in DictionaryOfGuildData.Keys)
            {
                if (id == guildId)
                {
                    guildData = DictionaryOfGuildData[id];
                    break;
                }
            }

            await File.WriteAllTextAsync($"{AppDataDirPath}/GuildData/{guildId}.json", JsonSerializer.Serialize(guildData));
        }

        /// <summary>If the data file for the specified guild does not exist, one will be created and loaded. Otherwise this does nothing.</summary>
        public static async Task EnsureGuildDataFileExistsAsync(ulong guildId)
        {
            if (!File.Exists($"{AppDataDirPath}/GuildData/{guildId}.json"))
            {
                await SaveGuildDataToFileAsync(guildId);
                await LoadGuildDataFromFileAsync(guildId);
            }
        }

        public static Dictionary<string, List<string>> GetKeywordsDictionaryForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Keywords;

        public static Config GetConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Config;
    }
}
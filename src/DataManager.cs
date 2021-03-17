using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Discord.WebSocket;
using wow2.Modules.Keywords;
using wow2.Modules.Games;
using wow2.Modules.Voice;

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

        /// <summary>Load all guild data from all files.</summary>
        public static async Task LoadGuildDataFromFileAsync()
        {
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load from file {fileInfo.Name} due to: {ex.Message} (mass load)");
                }
            }
        }

        /// <summary>Load guild data from the corresponding file.</summary>
        public static async Task LoadGuildDataFromFileAsync(ulong specifiedGuildId)
        {
            try
            {
                string guildDataJson = await File.ReadAllTextAsync($"{AppDataDirPath}/GuildData/{specifiedGuildId}.json");
                DictionaryOfGuildData[specifiedGuildId] = JsonSerializer.Deserialize<GuildData>(guildDataJson);

                Console.WriteLine($"Loaded guild data for {specifiedGuildId} (specific load)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load for guild {specifiedGuildId} due to: {ex.Message} (specific load)");
            }
        }

        /// <summary>Write all guild data to corresponding files.</summary>
        public static async Task SaveGuildDataToFileAsync()
        {
            foreach (ulong id in DictionaryOfGuildData.Keys)
            {
                await File.WriteAllTextAsync($"{AppDataDirPath}/GuildData/{id}.json", JsonSerializer.Serialize(DictionaryOfGuildData[id]));
                Console.WriteLine($"Saved guild data for {id} (mass save)");
            }
        }

        /// <summary>Write guild data to file for a specific guild.</summary>
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
            Console.WriteLine($"Saved guild data for {guildId} (specific save)");
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

        public static KeywordsModuleConfig GetKeywordsConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Keywords;

        public static GamesModuleConfig GetGamesConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Games;

        public static VoiceModuleConfig GetVoiceConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Voice;
    }
}
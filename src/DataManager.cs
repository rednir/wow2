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

        /// <summary>TODO</summary>
        public static async Task Initialize()
        {
            AppDataDirInfo = Directory.CreateDirectory($"{AppDataDirPath}/GuildData");
            await LoadGuildDataToFile();
        }

        public static async Task LoadGuildDataToFile(ulong specifiedGuildId = 0)
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

                        Console.WriteLine($"Loaded {guildId}");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Failed to load from file {fileInfo.Name}");
                    }
                }
            }
            else
            {
                // Load data for one guild
                // TODO
            }
        }

        public static async Task SaveGuildDataToFile(ulong guildId, GuildData guildData)
        {
            DictionaryOfGuildData[guildId] = guildData;
            await File.WriteAllTextAsync($"{AppDataDirPath}/GuildData/{guildId}.json", JsonSerializer.Serialize(guildData));
        }
    }
}